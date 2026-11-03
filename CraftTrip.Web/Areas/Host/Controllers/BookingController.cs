using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CraftTrip.Services.Interfaces;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Areas.Host.Controllers;

[Area("Host")]
[Authorize(Roles = "Host,Admin")]
public class BookingController : Controller
{
    private readonly IBookingService _bookingService;

    public BookingController(IBookingService bookingService)
    {
        _bookingService = bookingService;
    }

    public async Task<IActionResult> Index()
    {
        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var bookings = await _bookingService.GetHostBookingsAsync(hostId);
        return View(bookings);
    }

    public async Task<IActionResult> Details(Guid id)
    {
        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var booking = await _bookingService.GetBookingDetailsAsync(id, hostId);

        if (booking == null)
        {
            return NotFound("Không tìm thấy đơn đặt chỗ hoặc bạn không có quyền xem.");
        }

        return View(booking);
    }

    [HttpGet]
    public IActionResult CheckIn()
    {
        return View(new HostCheckInViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CheckIn(HostCheckInViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.TicketCode))
        {
            ModelState.AddModelError("TicketCode", "Vui lòng nhập hoặc quét mã vé.");
            return View(model);
        }

        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Extract code if scanned payload is in format CRAFTTRIP-TICKET:code:bookingCode
        var cleanCode = model.TicketCode.Trim();
        if (cleanCode.StartsWith("CRAFTTRIP-TICKET:"))
        {
            var parts = cleanCode.Split(':');
            if (parts.Length >= 2)
            {
                cleanCode = parts[1];
            }
        }

        var (success, message, booking) = await _bookingService.CheckInAttendanceAsync(cleanCode, hostId);

        model.IsSuccess = success;
        model.StatusMessage = message;
        model.CheckedInBooking = booking;

        return View(model);
    }
}
