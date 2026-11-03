using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class UserController : Controller
{
    private readonly IAdminService _adminService;
    private readonly CraftTripDbContext _context;

    public UserController(IAdminService adminService, CraftTripDbContext context)
    {
        _adminService = adminService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var users = await _adminService.GetAllUsersAsync();
        return View(users);
    }

    public async Task<IActionResult> Hosts()
    {
        var hosts = await _context.HostProfiles
            .AsNoTracking()
            .Include(h => h.User)
            .Include(h => h.Village)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();

        return View(hosts);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var success = await _adminService.ToggleUserStatusAsync(id);
        if (success)
        {
            TempData["SuccessMessage"] = "Đã cập nhật trạng thái hoạt động của tài khoản.";
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> VerifyHost(Guid hostProfileId, bool isApproved, string? note)
    {
        var success = await _adminService.VerifyHostAsync(hostProfileId, isApproved, note);
        if (success)
        {
            TempData["SuccessMessage"] = isApproved ? "Đã xác thực tài khoản Host thành công!" : "Đã từ chối xác thực Host.";
        }
        return RedirectToAction("Hosts");
    }
}
