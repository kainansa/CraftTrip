using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Services.Implementations;

public class ReviewService : IReviewService
{
    private readonly CraftTripDbContext _context;

    public ReviewService(CraftTripDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CanUserReviewWorkshopAsync(Guid workshopId, Guid guestId)
    {
        // BR_RV_04: Only customers with a Completed booking may submit a review.
        return await _context.Bookings.AnyAsync(b =>
            b.WorkshopId == workshopId &&
            b.GuestId == guestId &&
            b.Status == BookingStatus.Completed &&
            !_context.Reviews.Any(r => r.BookingId == b.Id));
    }

    public async Task<(bool Success, string Message)> CreateReviewAsync(Guid workshopId, Guid guestId, Guid bookingId, int rating, string comment)
    {
        if (rating < 1 || rating > 5)
        {
            return (false, "Đánh giá phải từ 1 đến 5 sao.");
        }

        if (string.IsNullOrWhiteSpace(comment))
        {
            return (false, "Vui lòng nhập nội dung đánh giá.");
        }

        // Verify booking eligibility
        var booking = await _context.Bookings
            .FirstOrDefaultAsync(b => b.Id == bookingId && b.GuestId == guestId && b.WorkshopId == workshopId);

        if (booking == null)
        {
            return (false, "Không tìm thấy thông tin đơn đặt chỗ hợp lệ.");
        }

        // BR_RV_04: Check booking is Completed
        if (booking.Status != BookingStatus.Completed)
        {
            return (false, "Bạn chỉ có thể đánh giá sau khi đã hoàn thành buổi trải nghiệm.");
        }

        // Prevent duplicate reviews for the same booking
        if (await _context.Reviews.AnyAsync(r => r.BookingId == bookingId))
        {
            return (false, "Bạn đã gửi đánh giá cho đơn đặt chỗ này rồi.");
        }

        var review = new Review
        {
            WorkshopId = workshopId,
            GuestId = guestId,
            BookingId = bookingId,
            Rating = rating,
            Comment = comment.Trim()
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        // Update Workshop AverageRating and ReviewCount
        var workshop = await _context.Workshops
            .Include(w => w.Reviews)
            .FirstOrDefaultAsync(w => w.Id == workshopId);

        if (workshop != null)
        {
            var validReviews = workshop.Reviews.Where(r => !r.IsDeleted).ToList();
            workshop.ReviewCount = validReviews.Count;
            workshop.AverageRating = validReviews.Any() ? Math.Round(validReviews.Average(r => r.Rating), 1) : 0;
            await _context.SaveChangesAsync();
        }

        return (true, "Đánh giá của bạn đã được đăng thành công!");
    }

    public async Task<bool> HostReplyReviewAsync(Guid reviewId, Guid hostId, string reply)
    {
        var review = await _context.Reviews
            .Include(r => r.Workshop)
            .FirstOrDefaultAsync(r => r.Id == reviewId);

        if (review == null || review.Workshop?.HostId != hostId)
        {
            return false; // IDOR check: only workshop host can reply
        }

        review.HostReply = reply.Trim();
        review.RepliedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        return true;
    }
}
