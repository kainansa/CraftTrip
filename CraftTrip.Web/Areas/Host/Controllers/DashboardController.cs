using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CraftTrip.Core.Enums;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Interfaces;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Areas.Host.Controllers;

[Area("Host")]
[Authorize(Roles = "Host,Admin")]
public class DashboardController : Controller
{
    private readonly CraftTripDbContext _context;
    private readonly IFinancialService _financialService;

    public DashboardController(CraftTripDbContext context, IFinancialService financialService)
    {
        _context = context;
        _financialService = financialService;
    }

    public async Task<IActionResult> Index()
    {
        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var hostProfile = await _context.HostProfiles
            .Include(h => h.Village)
            .FirstOrDefaultAsync(h => h.UserId == hostId);

        if (hostProfile == null)
        {
            return RedirectToAction("Index", "Onboarding", new { area = "Host" });
        }

        var workshops = await _context.Workshops
            .AsNoTracking()
            .Where(w => w.HostId == hostId && !w.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

        var recentBookings = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Workshop)
            .Include(b => b.Slot)
                .ThenInclude(s => s!.Schedule)
            .Include(b => b.Ticket)
            .Where(b => b.Workshop != null && b.Workshop.HostId == hostId && !b.IsDeleted)
            .OrderByDescending(b => b.CreatedAt)
            .Take(5)
            .ToListAsync();

        var finSummary = await _financialService.GetHostFinancialSummaryAsync(hostId);

        var viewModel = new HostDashboardViewModel
        {
            Profile = hostProfile,
            ActiveWorkshopsCount = workshops.Count(w => w.Status == WorkshopStatus.Published),
            PendingWorkshopsCount = workshops.Count(w => w.Status == WorkshopStatus.Pending),
            UpcomingBookingsCount = await _context.Bookings.CountAsync(b => b.Workshop != null && b.Workshop.HostId == hostId && b.Status == BookingStatus.Confirmed),
            TotalServedGuestsCount = finSummary.CompletedBookingsCount,
            TotalNetEarnings = finSummary.NetHostRevenue,
            AvailableWalletBalance = finSummary.WalletBalance,
            PendingEligibilityBalance = finSummary.PendingBalance,
            RecentBookings = recentBookings,
            MyWorkshops = workshops
        };

        return View(viewModel);
    }
}
