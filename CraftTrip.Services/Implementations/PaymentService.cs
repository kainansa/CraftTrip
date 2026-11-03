using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Interfaces;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Services.Implementations;

public class PaymentService : IPaymentService
{
    private readonly CraftTripDbContext _context;
    private readonly IVNPayService _vnPayService;
    private readonly IBookingService _bookingService;

    public PaymentService(
        CraftTripDbContext context,
        IVNPayService vnPayService,
        IBookingService bookingService)
    {
        _context = context;
        _vnPayService = vnPayService;
        _bookingService = bookingService;
    }

    public async Task<string> GenerateVNPayUrlAsync(Guid bookingId, string returnUrl, string clientIp)
    {
        var booking = await _context.Bookings
            .Include(b => b.Workshop)
            .FirstOrDefaultAsync(b => b.Id == bookingId);

        if (booking == null)
            throw new ArgumentException("Đơn đặt chỗ không tồn tại.", nameof(bookingId));

        var paymentInfo = new VNPayPaymentInformation
        {
            BookingId = booking.Id,
            BookingCode = booking.BookingCode,
            Amount = booking.TotalAmount,
            OrderDescription = $"Thanh toan ve workshop {booking.BookingCode}",
            ClientIpAddress = string.IsNullOrWhiteSpace(clientIp) ? "127.0.0.1" : clientIp
        };

        return _vnPayService.CreatePaymentUrl(paymentInfo, returnUrl);
    }

    public async Task<(bool Success, string Message, string? BookingCode)> ProcessVNPayReturnAsync(IDictionary<string, string> queryParams)
    {
        var callbackResult = _vnPayService.ProcessCallback(queryParams);
        if (string.IsNullOrEmpty(callbackResult.BookingCode))
        {
            return (false, "Dữ liệu trả về từ cổng thanh toán không hợp lệ.", null);
        }

        if (!callbackResult.IsSuccess)
        {
            return (false, callbackResult.Message ?? "Giao dịch không thành công.", callbackResult.BookingCode);
        }

        var confirmed = await _bookingService.ConfirmPaymentSuccessAsync(
            callbackResult.BookingCode,
            callbackResult.TransactionNo ?? Guid.NewGuid().ToString("N")[..10],
            callbackResult.BankCode ?? "VNPAY",
            callbackResult.Amount);

        if (!confirmed)
        {
            return (false, "Không thể cập nhật trạng thái đơn đặt chỗ.", callbackResult.BookingCode);
        }

        return (true, "Thanh toán thành công!", callbackResult.BookingCode);
    }
}
