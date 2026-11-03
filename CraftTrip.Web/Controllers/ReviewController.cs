using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Interfaces;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Controllers;

[Authorize]
public class ReviewController : Controller
{
    private readonly IReviewService _reviewService;
    private readonly CraftTripDbContext _context;

    public ReviewController(IReviewService reviewService, CraftTripDbContext context)
    {
        _reviewService = reviewService;
        _context = context;
    }

    public async Task<IActionResult> Create(Guid bookingId)
    {
        var guestId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var booking = await _context.Bookings
            .Include(b => b.Workshop)
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == guestId);

        if (booking == null) return NotFound("Không tìm thấy thông tin đơn đặt chỗ.");

        // BR_RV_04: Check booking is Completed
        if (booking.Status != Core.Enums.BookingStatus.Completed)
        {
            TempData["ErrorMessage"] = "Bạn chỉ có thể viết đánh giá sau khi đã hoàn thành buổi trải nghiệm tại xưởng.";
            return RedirectToAction("History", "Booking");
        }

        if (await _context.Reviews.AnyAsync(r => r.BookingId == bookingId))
        {
            TempData["ErrorMessage"] = "Bạn đã gửi đánh giá cho đơn đặt chỗ này rồi.";
            return RedirectToAction("Details", "Workshop", new { id = booking.WorkshopId });
        }

        var viewModel = new ReviewSubmitViewModel
        {
            WorkshopId = booking.WorkshopId,
            BookingId = booking.Id,
            WorkshopTitle = booking.Workshop?.Title ?? string.Empty,
            Rating = 5
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewSubmitViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var guestId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var (success, message) = await _reviewService.CreateReviewAsync(
            model.WorkshopId, guestId, model.BookingId, model.Rating, model.Comment);

        if (!success)
        {
            ModelState.AddModelError("", message);
            return View(model);
        }

        TempData["SuccessMessage"] = "Cảm ơn bạn đã gửi đánh giá! Ý kiến của bạn sẽ giúp nghệ nhân hoàn thiện trải nghiệm tốt hơn.";
        return RedirectToAction("Details", "Workshop", new { id = model.WorkshopId });
    }
}
