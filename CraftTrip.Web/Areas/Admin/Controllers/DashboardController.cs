using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Interfaces;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IAdminService _adminService;
    private readonly CraftTripDbContext _context;

    public DashboardController(IAdminService adminService, CraftTripDbContext context)
    {
        _adminService = adminService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var stats = await _adminService.GetDashboardStatsAsync();
        var pendingWorkshops = await _adminService.GetPendingWorkshopsAsync();
        var pendingRevisions = await _adminService.GetPendingRevisionsAsync();
        var pendingHosts = await _adminService.GetPendingHostVerificationsAsync();

        var recentBookings = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Workshop)
            .Include(b => b.Guest)
            .OrderByDescending(b => b.CreatedAt)
            .Take(10)
            .ToListAsync();

        var viewModel = new AdminDashboardViewModel
        {
            Stats = stats,
            PendingWorkshops = pendingWorkshops,
            PendingRevisions = pendingRevisions,
            PendingHosts = pendingHosts,
            RecentBookings = recentBookings
        };

        return View(viewModel);
    }
}
