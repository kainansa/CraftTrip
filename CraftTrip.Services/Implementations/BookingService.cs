using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;
using CraftTrip.Core.Interfaces;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.DTOs;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Services.Implementations;

public class BookingService : IBookingService
{
    private readonly CraftTripDbContext _context;
    private readonly IQrCodeService _qrCodeService;
    private readonly INotificationService _notificationService;
    private readonly IEmailService? _emailService;

    public BookingService(
        CraftTripDbContext context,
        IQrCodeService qrCodeService,
        INotificationService notificationService,
        IEmailService? emailService = null)
    {
        _context = context;
        _qrCodeService = qrCodeService;
        _notificationService = notificationService;
        _emailService = emailService;
    }

    public async Task<BookingResult> CreatePendingBookingAsync(BookingCreateDto dto, Guid guestId)
    {
        if (dto.Quantity <= 0)
        {
            return BookingResult.Failed("Số lượng khách phải lớn hơn 0.");
        }

        var slot = await _context.Slots
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch!.Workshop)
            .FirstOrDefaultAsync(s => s.Id == dto.SlotId && !s.IsDeleted);

        if (slot == null || slot.Schedule == null || slot.Schedule.Workshop == null)
        {
            return BookingResult.Failed("Khung giờ không tồn tại hoặc đã bị hủy.");
        }

        var workshop = slot.Schedule.Workshop;
        if (workshop.Status != WorkshopStatus.Published)
        {
            return BookingResult.Failed("Workshop hiện chưa mở nhận khách.");
        }

        if (slot.AvailableCapacity < dto.Quantity)
        {
            return BookingResult.Failed($"Rất tiếc, khung giờ này chỉ còn trống {slot.AvailableCapacity} chỗ.");
        }

        // Commission calculation (BR_CM_01)
        var commissionRule = await _context.CommissionRules.FirstOrDefaultAsync(r => r.IsActive)
                             ?? new CommissionRule();

        var commissionRate = dto.Quantity >= commissionRule.GroupSizeThreshold
            ? commissionRule.LargeGroupRate // 15% (0.15)
            : commissionRule.SmallGroupRate; // 12% (0.12)

        var unitPrice = workshop.BasePrice;
        var totalAmount = unitPrice * dto.Quantity;
        var platformFee = totalAmount * commissionRate;
        var hostEarnings = totalAmount - platformFee;

        // Reserve capacity with Optimistic Concurrency check
        slot.AvailableCapacity -= dto.Quantity;

        var bookingCode = $"CT-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        var booking = new Booking
        {
            BookingCode = bookingCode,
            GuestId = guestId,
            WorkshopId = workshop.Id,
            SlotId = slot.Id,
            Quantity = dto.Quantity,
            UnitPrice = unitPrice,
            TotalAmount = totalAmount,
            CommissionRate = commissionRate,
            PlatformFee = platformFee,
            HostEarnings = hostEarnings,
            Status = BookingStatus.Pending,
            CustomerName = dto.CustomerName.Trim(),
            CustomerEmail = dto.CustomerEmail.Trim(),
            CustomerPhone = dto.CustomerPhone.Trim(),
            SpecialNotes = dto.SpecialNotes?.Trim(),
            ExpiresAt = DateTime.UtcNow.AddMinutes(15), // 15-minute pending rule
            IsCapacityRestored = false
        };

        _context.BookingStatusHistories.Add(new BookingStatusHistory
        {
            BookingId = booking.Id,
            PreviousStatus = null,
            NewStatus = BookingStatus.Pending,
            Note = "Khởi tạo đơn đặt chỗ. Giữ chỗ trong 15 phút để chờ thanh toán."
        });

        _context.Bookings.Add(booking);

        try
        {
            await _context.SaveChangesAsync();
            return BookingResult.Succeeded(booking.Id, booking.BookingCode, booking.TotalAmount);
        }
        catch (DbUpdateConcurrencyException)
        {
            return BookingResult.Failed("Rất tiếc, đã có người nhanh tay đặt trước. Vui lòng chọn ca khác.");
        }
    }

    public async Task<bool> ConfirmPaymentSuccessAsync(string bookingCode, string transactionNo, string bankCode, decimal amount)
    {
        var booking = await _context.Bookings
            .Include(b => b.Workshop)
                .ThenInclude(w => w!.Village)
            .Include(b => b.Slot)
                .ThenInclude(s => s!.Schedule)
                    .ThenInclude(sch => sch!.Workshop)
            .Include(b => b.Ticket)
            .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

        if (booking == null) return false;

        // Idempotency: prevent duplicate processing if already Confirmed
        if (booking.Status == BookingStatus.Confirmed || booking.Status == BookingStatus.Completed)
        {
            return true;
        }

        booking.Status = BookingStatus.Confirmed;
        booking.PaymentTxnId = transactionNo;

        _context.BookingStatusHistories.Add(new BookingStatusHistory
        {
            BookingId = booking.Id,
            PreviousStatus = BookingStatus.Pending,
            NewStatus = BookingStatus.Confirmed,
            Note = $"Thanh toán VNPay thành công. Mã GD: {transactionNo}, Ngân hàng: {bankCode}."
        });

        // Record Payment
        var payment = new Payment
        {
            BookingId = booking.Id,
            Amount = amount > 0 ? amount : booking.TotalAmount,
            PaymentMethod = "VNPay",
            Status = PaymentStatus.Success,
            TransactionNo = transactionNo,
            BankCode = bankCode,
            PayDate = DateTime.UtcNow,
            ResponseCode = "00"
        };
        payment.Transactions.Add(new PaymentTransaction
        {
            TransactionType = "Callback",
            Amount = payment.Amount,
            IsSuccess = true,
            RawDataJson = $"BankCode={bankCode};TxnNo={transactionNo}"
        });
        _context.Payments.Add(payment);

        // Generate Booking Ticket with QR Code (Part 12)
        if (booking.Ticket == null)
        {
            var ticketCode = $"TCK-{booking.BookingCode}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
            var qrPayload = $"CRAFTTRIP-TICKET:{ticketCode}:{booking.BookingCode}";
            var qrCodeBase64 = _qrCodeService.GenerateQrCodeBase64(qrPayload);

            var ticket = new BookingTicket
            {
                BookingId = booking.Id,
                TicketCode = ticketCode,
                QrCodeData = !string.IsNullOrWhiteSpace(qrCodeBase64) ? qrCodeBase64 : $"data:text/plain;base64,{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(qrPayload))}",
                IsRedeemed = false
            };
            _context.BookingTickets.Add(ticket);
            booking.Ticket = ticket;
        }

        // Schedule Host Payout (BR_PO_02: eligible 24 hours after workshop session ended)
        if (!await _context.HostPayouts.AnyAsync(hp => hp.BookingId == booking.Id))
        {
            var slot = booking.Slot;
            var sessionEndTime = slot != null && slot.Schedule != null
                ? slot.Schedule.Date.Date + slot.EndTime
                : DateTime.UtcNow.AddHours(3);

            var eligibleAt = sessionEndTime.AddHours(24); // 24 hours after session ends

            var hostPayout = new HostPayout
            {
                HostId = booking.Workshop?.HostId ?? booking.Slot?.Schedule?.Workshop?.HostId ?? Guid.Empty,
                BookingId = booking.Id,
                Amount = booking.HostEarnings,
                EligibleAt = eligibleAt,
                Status = PayoutStatus.PendingEligibility,
                PayoutReference = $"PO-{booking.BookingCode}"
            };
            _context.HostPayouts.Add(hostPayout);
        }

        await _context.SaveChangesAsync();

        // Queue notifications
        await _notificationService.SendNotificationAsync(
            booking.GuestId,
            "Đặt vé Workshop thành công!",
            $"Đơn đặt chỗ {booking.BookingCode} cho workshop '{booking.Workshop?.Title}' đã được xác nhận. Vui lòng kiểm tra mã QR trong chi tiết vé.",
            $"/Booking/Details/{booking.Id}",
            "Booking");

        if (booking.Workshop != null)
        {
            await _notificationService.SendNotificationAsync(
                booking.Workshop.HostId,
                "Có khách đặt chỗ mới!",
                $"Khách hàng {booking.CustomerName} vừa đặt {booking.Quantity} vé cho buổi trải nghiệm ngày {booking.Slot?.Schedule?.Date:dd/MM/yyyy}.",
                $"/Host/Booking/Details/{booking.Id}",
                "Booking");
        }

        // Deliver E-Ticket Confirmation Email (Part 7)
        if (_emailService != null)
        {
            try
            {
                await _emailService.SendBookingConfirmationEmailAsync(booking);
            }
            catch
            {
                // Transient SMTP errors must never invalidate successful payment or ticket
            }
        }

        return true;
    }

    public async Task<int> ExpirePendingBookingsAsync()
    {
        var now = DateTime.UtcNow;
        var expiredBookings = await _context.Bookings
            .Include(b => b.Slot)
            .Where(b => b.Status == BookingStatus.Pending && b.ExpiresAt <= now && !b.IsCapacityRestored)
            .ToListAsync();

        if (!expiredBookings.Any()) return 0;

        foreach (var booking in expiredBookings)
        {
            if (booking.Slot != null && !booking.IsCapacityRestored)
            {
                // Restore capacity exactly once (Part 2.1 & 9)
                booking.Slot.AvailableCapacity += booking.Quantity;
                booking.IsCapacityRestored = true;
            }

            booking.Status = BookingStatus.Canceled;
            booking.CancellationReason = "Hết hạn thanh toán sau 15 phút.";
            booking.CanceledAt = now;

            _context.BookingStatusHistories.Add(new BookingStatusHistory
            {
                BookingId = booking.Id,
                PreviousStatus = BookingStatus.Pending,
                NewStatus = BookingStatus.Canceled,
                Note = "Hết hạn thanh toán 15 phút. Hệ thống tự động hoàn lại số chỗ trống."
            });
        }

        await _context.SaveChangesAsync();
        return expiredBookings.Count;
    }

    public async Task<bool> CancelBookingAsync(Guid bookingId, Guid guestId, string reason)
    {
        var booking = await _context.Bookings
            .Include(b => b.Slot)
                .ThenInclude(s => s!.Schedule)
                    .ThenInclude(sch => sch!.Workshop)
            .Include(b => b.Ticket)
            .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted);

        if (booking == null || booking.GuestId != guestId)
        {
            return false; // IDOR check
        }

        if (booking.Status != BookingStatus.Confirmed && booking.Status != BookingStatus.Paid)
        {
            return false; // Can only cancel confirmed or paid bookings
        }

        var slot = booking.Slot;
        if (slot == null || slot.Schedule == null) return false;

        // BR_CN_03: Cancellation must be >= 48 hours before session start!
        var sessionStartTime = slot.Schedule.Date.Date + slot.StartTime;
        var timeDifference = sessionStartTime - DateTime.UtcNow;

        if (timeDifference.TotalHours < 48)
        {
            // Fewer than 48 hours remaining: cancellation blocked by policy
            return false;
        }

        // Apply 100% refund as per BR_CN_03
        booking.Status = BookingStatus.Canceled;
        booking.CancellationReason = reason.Trim();
        booking.CanceledAt = DateTime.UtcNow;

        // Restore capacity safely
        if (!booking.IsCapacityRestored)
        {
            slot.AvailableCapacity += booking.Quantity;
            booking.IsCapacityRestored = true;
        }

        _context.BookingStatusHistories.Add(new BookingStatusHistory
        {
            BookingId = booking.Id,
            PreviousStatus = BookingStatus.Confirmed,
            NewStatus = BookingStatus.Canceled,
            Note = $"Khách hàng yêu cầu hủy vé trước 48 giờ. Lý do: {reason}. Hoàn tiền 100% ({booking.TotalAmount:N0} đ)."
        });

        // Record 100% Refund
        _context.Refunds.Add(new Refund
        {
            BookingId = booking.Id,
            Amount = booking.TotalAmount,
            Reason = reason,
            Status = RefundStatus.Processing,
            ProcessedAt = DateTime.UtcNow,
            RefundTransactionRef = $"REF-{booking.BookingCode}"
        });

        // Cancel any pending Host Payout
        var payout = await _context.HostPayouts.FirstOrDefaultAsync(hp => hp.BookingId == booking.Id);
        if (payout != null)
        {
            _context.HostPayouts.Remove(payout);
        }

        await _context.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(
            booking.GuestId,
            "Hủy vé thành công",
            $"Yêu cầu hủy vé cho đơn {booking.BookingCode} đã được phê duyệt. Hệ thống đang xử lý hoàn tiền 100% ({booking.TotalAmount:N0} đ).",
            $"/Booking/Details/{booking.Id}",
            "Refund");

        if (booking.Workshop != null)
        {
            await _notificationService.SendNotificationAsync(
                booking.Workshop.HostId,
                "Khách hàng đã hủy vé",
                $"Khách hàng {booking.CustomerName} đã hủy {booking.Quantity} vé đơn {booking.BookingCode}. Ca ngày {slot.Schedule.Date:dd/MM/yyyy} đã được mở lại chỗ trống.",
                $"/Host/Booking/Details/{booking.Id}",
                "Booking");
        }

        return true;
    }

    public async Task<List<Booking>> GetGuestBookingsAsync(Guid guestId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Workshop)
                .ThenInclude(w => w!.Village)
            .Include(b => b.Slot)
                .ThenInclude(s => s!.Schedule)
            .Include(b => b.Ticket)
            .Include(b => b.Review)
            .Where(b => b.GuestId == guestId && !b.IsDeleted)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<Booking?> GetBookingDetailsAsync(Guid bookingId, Guid? userId = null, bool isAdmin = false)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Workshop)
                .ThenInclude(w => w!.Village)
            .Include(b => b.Workshop)
                .ThenInclude(w => w!.Host)
                    .ThenInclude(h => h!.HostProfile)
            .Include(b => b.Slot)
                .ThenInclude(s => s!.Schedule)
            .Include(b => b.Ticket)
            .Include(b => b.Payments)
            .Include(b => b.Refunds)
            .Include(b => b.Review)
            .Include(b => b.StatusHistories.OrderByDescending(h => h.CreatedAt))
            .FirstOrDefaultAsync(b => b.Id == bookingId && !b.IsDeleted);

        if (booking == null) return null;

        // IDOR protection: Guest can only view their own booking, Host can only view bookings for their workshops, Admin can view all
        if (!isAdmin && userId.HasValue)
        {
            var isGuestOwner = booking.GuestId == userId.Value;
            var isHostOwner = booking.Workshop?.HostId == userId.Value;
            if (!isGuestOwner && !isHostOwner)
            {
                return null;
            }
        }

        return booking;
    }

    public async Task<Booking?> GetBookingByCodeAsync(string bookingCode)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Workshop)
            .Include(b => b.Slot)
                .ThenInclude(s => s!.Schedule)
            .Include(b => b.Ticket)
            .FirstOrDefaultAsync(b => b.BookingCode == bookingCode && !b.IsDeleted);
    }

    public async Task<List<Booking>> GetHostBookingsAsync(Guid hostId)
    {
        return await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Workshop)
            .Include(b => b.Slot)
                .ThenInclude(s => s!.Schedule)
            .Include(b => b.Ticket)
            .Where(b => b.Workshop != null && b.Workshop.HostId == hostId && !b.IsDeleted)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<(bool Success, string Message, Booking? Booking)> CheckInAttendanceAsync(string ticketOrBookingCode, Guid hostId)
    {
        var cleanCode = ticketOrBookingCode.Trim();

        var booking = await _context.Bookings
            .Include(b => b.Workshop)
            .Include(b => b.Slot)
                .ThenInclude(s => s!.Schedule)
            .Include(b => b.Ticket)
            .FirstOrDefaultAsync(b => (b.BookingCode == cleanCode || (b.Ticket != null && b.Ticket.TicketCode == cleanCode)) && !b.IsDeleted);

        if (booking == null)
        {
            return (false, "Không tìm thấy vé hoặc mã đơn đặt chỗ trong hệ thống.", null);
        }

        // Host ownership check (IDOR protection)
        if (booking.Workshop?.HostId != hostId)
        {
            return (false, "Vé này thuộc về Workshop của cơ sở khác. Bạn không có quyền điểm danh.", null);
        }

        if (booking.Status != BookingStatus.Confirmed && booking.Status != BookingStatus.Completed)
        {
            return (false, $"Vé không hợp lệ vì đơn đang ở trạng thái '{booking.Status}'.", booking);
        }

        if (booking.AttendanceStatus == AttendanceStatus.CheckedIn)
        {
            return (false, $"Vé này đã được sử dụng / ĐÃ ĐƯỢC ĐIỂM DANH trước đó vào lúc {booking.CheckedInAt:HH:mm dd/MM/yyyy}.", booking);
        }

        booking.AttendanceStatus = AttendanceStatus.CheckedIn;
        booking.CheckedInAt = DateTime.UtcNow;
        booking.CheckedInByHostId = hostId;
        booking.Status = BookingStatus.Completed; // Once checked-in, experience is marked completed!

        if (booking.Ticket != null)
        {
            booking.Ticket.IsRedeemed = true;
            booking.Ticket.RedeemedAt = DateTime.UtcNow;
        }

        _context.BookingStatusHistories.Add(new BookingStatusHistory
        {
            BookingId = booking.Id,
            PreviousStatus = BookingStatus.Confirmed,
            NewStatus = BookingStatus.Completed,
            Note = $"Host điểm danh check-in thành công. Số lượng: {booking.Quantity} khách."
        });

        await _context.SaveChangesAsync();
        return (true, $"Điểm danh thành công! Khách: {booking.CustomerName} ({booking.Quantity} người).", booking);
    }
}
