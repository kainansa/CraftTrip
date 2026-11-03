using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Interfaces;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Controllers;

public class WorkshopController : Controller
{
    private readonly IWorkshopService _workshopService;
    private readonly IReviewService _reviewService;
    private readonly CraftTripDbContext _context;

    public WorkshopController(
        IWorkshopService workshopService,
        IReviewService reviewService,
        CraftTripDbContext context)
    {
        _workshopService = workshopService;
        _reviewService = reviewService;
        _context = context;
    }

    public async Task<IActionResult> Index(
        string? keyword,
        Guid? categoryId,
        Guid? villageId,
        decimal? minPrice,
        decimal? maxPrice,
        int? maxDuration,
        DateTime? availableDate,
        string? sortBy,
        int page = 1)
    {
        int pageSize = 9;
        var (workshops, totalCount) = await _workshopService.SearchWorkshopsAsync(
            keyword, categoryId, villageId, minPrice, maxPrice, maxDuration, availableDate, sortBy, page, pageSize);

        var categories = await _context.CraftCategories.AsNoTracking().Where(c => !c.IsDeleted).OrderBy(c => c.DisplayOrder).ToListAsync();
        var villages = await _context.Villages.AsNoTracking().Where(v => !v.IsDeleted).OrderBy(v => v.DisplayOrder).ToListAsync();

        var viewModel = new WorkshopListViewModel
        {
            Workshops = workshops,
            Categories = categories,
            Villages = villages,
            Keyword = keyword,
            CategoryId = categoryId,
            VillageId = villageId,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            MaxDuration = maxDuration,
            AvailableDate = availableDate,
            SortBy = sortBy,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };

        return View(viewModel);
    }

    public async Task<IActionResult> Details(Guid? id, string? slug)
    {
        var workshop = id.HasValue
            ? await _workshopService.GetWorkshopDetailsByIdAsync(id.Value)
            : (!string.IsNullOrEmpty(slug) ? await _workshopService.GetWorkshopDetailsBySlugAsync(slug) : null);

        if (workshop == null)
        {
            return NotFound("Không tìm thấy Workshop yêu cầu.");
        }

        bool canReview = false;
        Guid? eligibleBookingId = null;

        if (User.Identity != null && User.Identity.IsAuthenticated)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (Guid.TryParse(userIdStr, out var guestId))
            {
                canReview = await _reviewService.CanUserReviewWorkshopAsync(workshop.Id, guestId);
                if (canReview)
                {
                    var booking = await _context.Bookings
                        .Where(b => b.WorkshopId == workshop.Id && b.GuestId == guestId && b.Status == Core.Enums.BookingStatus.Completed && !_context.Reviews.Any(r => r.BookingId == b.Id))
                        .FirstOrDefaultAsync();
                    eligibleBookingId = booking?.Id;
                }
            }
        }

        var relatedWorkshops = await _context.Workshops
            .AsNoTracking()
            .Include(w => w.Village)
            .Where(w => w.CategoryId == workshop.CategoryId && w.Id != workshop.Id && w.Status == Core.Enums.WorkshopStatus.Published && !w.IsDeleted)
            .Take(3)
            .ToListAsync();

        var viewModel = new WorkshopDetailViewModel
        {
            Workshop = workshop,
            CanReview = canReview,
            EligibleBookingId = eligibleBookingId,
            RelatedWorkshops = relatedWorkshops
        };

        return View(viewModel);
    }
}
