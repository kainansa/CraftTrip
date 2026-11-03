using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.DTOs;
using CraftTrip.Services.Interfaces;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Controllers;

public class BookingController : Controller
{
    private readonly IBookingService _bookingService;
    private readonly IPaymentService _paymentService;
    private readonly CraftTripDbContext _context;
    private readonly IConfiguration _configuration;

    public BookingController(
        IBookingService bookingService,
        IPaymentService paymentService,
        CraftTripDbContext context,
        IConfiguration configuration)
    {
        _bookingService = bookingService;
        _paymentService = paymentService;
        _context = context;
        _configuration = configuration;
    }

    [Authorize]
    public async Task<IActionResult> Checkout(Guid slotId)
    {
        var slot = await _context.Slots
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch!.Workshop)
                    .ThenInclude(w => w!.Village)
            .FirstOrDefaultAsync(s => s.Id == slotId && !s.IsDeleted);

        if (slot == null || slot.Schedule == null || slot.Schedule.Workshop == null)
        {
            return NotFound("Khung giờ trải nghiệm không tồn tại.");
        }

        if (slot.AvailableCapacity <= 0)
        {
            TempData["ErrorMessage"] = "Rất tiếc, khung giờ này đã hết chỗ.";
            return RedirectToAction("Details", "Workshop", new { id = slot.Schedule.Workshop.Id });
        }

        var user = await _context.Users.FindAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));

        var viewModel = new BookingCheckoutViewModel
        {
            SlotId = slot.Id,
            Slot = slot,
            Workshop = slot.Schedule.Workshop,
            Quantity = 1,
            CustomerName = user?.FullName ?? string.Empty,
            CustomerEmail = user?.Email ?? string.Empty,
            CustomerPhone = user?.PhoneNumber ?? string.Empty
        };

        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessPayment(BookingCheckoutViewModel model)
    {
        // 1. Server-side validation of Slot, Workshop & Capacity
        var slot = await _context.Slots
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch!.Workshop)
                    .ThenInclude(w => w!.Village)
            .FirstOrDefaultAsync(s => s.Id == model.SlotId && !s.IsDeleted);

        if (slot == null || slot.Schedule?.Workshop == null)
        {
            TempData["ErrorMessage"] = "Khung giờ trải nghiệm không tồn tại hoặc đã bị đóng.";
            return RedirectToAction("Index", "Workshop");
        }

        if (slot.Schedule.Date.Date < DateTime.UtcNow.Date)
        {
            TempData["ErrorMessage"] = "Khung giờ này đã diễn ra trong quá khứ, vui lòng chọn ngày khác.";
            return RedirectToAction("Details", "Workshop", new { id = slot.Schedule.Workshop.Id });
        }

        if (slot.AvailableCapacity < model.Quantity)
        {
            TempData["ErrorMessage"] = $"Rất tiếc, khung giờ này chỉ còn trống {slot.AvailableCapacity} chỗ.";
            model.Slot = slot;
            model.Workshop = slot.Schedule.Workshop;
            return View("Checkout", model);
        }

        if (!ModelState.IsValid)
        {
            model.Slot = slot;
            model.Workshop = slot.Schedule.Workshop;
            return View("Checkout", model);
        }

        var guestId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var bookingDto = new BookingCreateDto
        {
            SlotId = model.SlotId,
            Quantity = model.Quantity,
            CustomerName = model.CustomerName.Trim(),
            CustomerEmail = model.CustomerEmail.Trim(),
            CustomerPhone = model.CustomerPhone.Trim(),
            SpecialNotes = model.SpecialNotes?.Trim()
        };

        var result = await _bookingService.CreatePendingBookingAsync(bookingDto, guestId);
        if (!result.Success || !result.BookingId.HasValue)
        {
            TempData["ErrorMessage"] = result.ErrorMessage ?? "Không thể khởi tạo đơn đặt chỗ.";
            return RedirectToAction("Checkout", new { slotId = model.SlotId });
        }

        var isSimulation = _configuration.GetValue<bool>("VNPay:SimulationMode", true);
        var tmnCode = _configuration["VNPay:TmnCode"];

        if (isSimulation || string.IsNullOrWhiteSpace(tmnCode) || tmnCode == "CRAFTTRIP")
        {
            // Direct to internal sandbox simulator for seamless testing
            return RedirectToAction("SimulatePayment", new { bookingCode = result.BookingCode });
        }

        // Generate VNPay return URL for production gateway
        var returnUrl = Url.Action("VNPayReturn", "Booking", null, Request.Scheme);
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        try
        {
            var paymentUrl = await _paymentService.GenerateVNPayUrlAsync(result.BookingId.Value, returnUrl!, clientIp);
            return Redirect(paymentUrl);
        }
        catch (Exception)
        {
            return RedirectToAction("SimulatePayment", new { bookingCode = result.BookingCode });
        }
    }

    // Handles the VNPay callback
    public async Task<IActionResult> VNPayReturn()
    {
        var queryParams = Request.Query.ToDictionary(k => k.Key, v => v.Value.ToString());

        var (success, message, bookingCode) = await _paymentService.ProcessVNPayReturnAsync(queryParams);
        if (!success)
        {
            TempData["ErrorMessage"] = message;
            if (!string.IsNullOrEmpty(bookingCode))
            {
                return RedirectToAction("Confirmation", new { bookingCode, success = false });
            }
            return RedirectToAction("Tickets");
        }

        return RedirectToAction("Confirmation", new { bookingCode, success = true });
    }

    // Development sandbox simulation page for testing payments without real banking card
    public async Task<IActionResult> SimulatePayment(string bookingCode)
    {
        var booking = await _bookingService.GetBookingByCodeAsync(bookingCode);
        if (booking == null) return NotFound();

        return View(booking);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CompleteSimulatedPayment(string bookingCode)
    {
        var booking = await _bookingService.GetBookingByCodeAsync(bookingCode);
        if (booking == null) return NotFound();

        var txnNo = $"SIM-{DateTime.UtcNow:yyMMddHHmmss}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
        await _bookingService.ConfirmPaymentSuccessAsync(bookingCode, txnNo, "VNPAY-TEST", booking.TotalAmount);

        return RedirectToAction("Confirmation", new { bookingCode, success = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> FailSimulatedPayment(string bookingCode)
    {
        var booking = await _bookingService.GetBookingByCodeAsync(bookingCode);
        if (booking == null) return NotFound();

        _context.Payments.Add(new Payment
        {
            BookingId = booking.Id,
            Amount = booking.TotalAmount,
            PaymentMethod = "VNPay (Simulated)",
            Status = PaymentStatus.Failed,
            TransactionNo = $"SIM-FAIL-{DateTime.UtcNow:yyMMdd}-{Guid.NewGuid().ToString("N")[..6]}",
            BankCode = "TESTBANK",
            ResponseCode = "24",
            PayDate = DateTime.UtcNow
        });

        _context.BookingStatusHistories.Add(new BookingStatusHistory
        {
            BookingId = booking.Id,
            PreviousStatus = booking.Status,
            NewStatus = booking.Status,
            Note = "Thanh toán không thành công (Khách hàng mô phỏng giao dịch thất bại)."
        });

        await _context.SaveChangesAsync();
        return RedirectToAction("Confirmation", new { bookingCode, success = false });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CancelSimulatedPayment(string bookingCode)
    {
        var booking = await _context.Bookings
            .Include(b => b.Slot)
            .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

        if (booking == null) return NotFound();

        if (booking.Status == BookingStatus.Pending)
        {
            if (!booking.IsCapacityRestored && booking.Slot != null)
            {
                booking.Slot.AvailableCapacity += booking.Quantity;
                booking.IsCapacityRestored = true;
            }
            booking.Status = BookingStatus.Canceled;
            booking.CancellationReason = "Người dùng hủy bỏ giao dịch tại cổng thanh toán.";
            booking.CanceledAt = DateTime.UtcNow;

            _context.BookingStatusHistories.Add(new BookingStatusHistory
            {
                BookingId = booking.Id,
                PreviousStatus = BookingStatus.Pending,
                NewStatus = BookingStatus.Canceled,
                Note = "Khách hàng hủy bỏ giao dịch thanh toán."
            });

            await _context.SaveChangesAsync();
        }

        TempData["ErrorMessage"] = "Bạn đã hủy giao dịch thanh toán.";
        return RedirectToAction("Details", "Workshop", new { id = booking.WorkshopId });
    }

    public async Task<IActionResult> Confirmation(string bookingCode, bool success = true)
    {
        if (string.IsNullOrEmpty(bookingCode)) return RedirectToAction("Index", "Home");

        var booking = await _bookingService.GetBookingByCodeAsync(bookingCode);
        if (booking == null) return NotFound("Không tìm thấy đơn đặt chỗ.");

        var viewModel = new BookingConfirmationViewModel
        {
            Booking = booking,
            PaymentSuccess = success,
            Message = success ? "Đặt vé và thanh toán thành công! Vé điện tử và mã QR đã được phát hành." : "Thanh toán chưa hoàn tất hoặc bị từ chối."
        };

        return View(viewModel);
    }

    [Authorize]
    public async Task<IActionResult> Tickets()
    {
        var guestId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var bookings = await _bookingService.GetGuestBookingsAsync(guestId);

        var tickets = bookings
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed || b.Ticket != null)
            .Select(b =>
            {
                var slot = b.Slot;
                var sessionStart = slot?.Schedule != null ? slot.Schedule.Date.Date + slot.StartTime : DateTime.MinValue;
                var canCancel = (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Paid) &&
                                (sessionStart - DateTime.UtcNow).TotalHours >= 48;

                return new GuestTicketViewModel
                {
                    BookingId = b.Id,
                    BookingCode = b.BookingCode,
                    TicketCode = b.Ticket?.TicketCode ?? $"TCK-{b.BookingCode}",
                    WorkshopTitle = b.Workshop?.Title ?? "Workshop làng nghề",
                    WorkshopCoverImage = b.Workshop?.CoverImageUrl,
                    VillageName = b.Workshop?.Village?.Name ?? "Làng nghề truyền thống",
                    WorkshopAddress = b.Workshop?.LocationAddress ?? string.Empty,
                    SessionDate = slot?.Schedule?.Date ?? DateTime.MinValue,
                    StartTime = slot?.StartTime ?? TimeSpan.Zero,
                    EndTime = slot?.EndTime ?? TimeSpan.Zero,
                    Quantity = b.Quantity,
                    TotalAmount = b.TotalAmount,
                    BookingStatus = b.Status,
                    AttendanceStatus = b.AttendanceStatus,
                    QrCodeData = b.Ticket?.QrCodeData,
                    CanCancel = canCancel
                };
            })
            .ToList();

        var model = new GuestTicketListViewModel
        {
            Tickets = tickets
        };

        return View(model);
    }

    [Authorize]
    public async Task<IActionResult> History()
    {
        var guestId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var bookings = await _bookingService.GetGuestBookingsAsync(guestId);
        return View(bookings);
    }

    [Authorize]
    public async Task<IActionResult> Details(Guid id)
    {
        var guestId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdmin = User.IsInRole("Admin");

        var booking = await _bookingService.GetBookingDetailsAsync(id, guestId, isAdmin);
        if (booking == null)
        {
            return NotFound("Không tìm thấy thông tin vé hoặc bạn không có quyền xem.");
        }

        return View(booking);
    }

    [Authorize]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var guestId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var booking = await _bookingService.GetBookingDetailsAsync(id, guestId);
        if (booking == null) return NotFound();

        var slot = booking.Slot;
        if (slot == null || slot.Schedule == null) return BadRequest();

        var sessionStart = slot.Schedule.Date.Date + slot.StartTime;
        var remainingHours = (sessionStart - DateTime.UtcNow).TotalHours;

        var viewModel = new BookingCancelViewModel
        {
            BookingId = booking.Id,
            BookingCode = booking.BookingCode,
            WorkshopTitle = booking.Workshop?.Title ?? string.Empty,
            SessionStartTime = sessionStart,
            TotalAmount = booking.TotalAmount,
            IsEligibleForFullRefund = remainingHours >= 48
        };

        return View(viewModel);
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmCancel(BookingCancelViewModel model)
    {
        var guestId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var success = await _bookingService.CancelBookingAsync(model.BookingId, guestId, model.Reason);
        if (!success)
        {
            TempData["ErrorMessage"] = "Không thể hủy vé vì thời gian còn lại dưới 48 giờ hoặc đơn vé không hợp lệ.";
            return RedirectToAction("Cancel", new { id = model.BookingId });
        }

        TempData["SuccessMessage"] = "Hủy vé thành công! Bạn được hoàn lại 100% số tiền đã thanh toán.";
        return RedirectToAction("History");
    }
}
