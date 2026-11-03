using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Interfaces;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class FinancialController : Controller
{
    private readonly IFinancialService _financialService;
    private readonly IAdminService _adminService;
    private readonly CraftTripDbContext _context;

    public FinancialController(
        IFinancialService financialService,
        IAdminService adminService,
        CraftTripDbContext context)
    {
        _financialService = financialService;
        _adminService = adminService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var stats = await _financialService.GetAdminFinancialSummaryAsync();

        var bookings = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Workshop)
            .Include(b => b.Guest)
            .OrderByDescending(b => b.CreatedAt)
            .Take(25)
            .ToListAsync();

        var payouts = await _context.HostPayouts
            .AsNoTracking()
            .Include(p => p.Host)
            .Include(p => p.Booking)
            .OrderByDescending(p => p.CreatedAt)
            .Take(25)
            .ToListAsync();

        var refunds = await _context.Refunds
            .AsNoTracking()
            .Include(r => r.Booking)
            .OrderByDescending(r => r.CreatedAt)
            .Take(25)
            .ToListAsync();

        var rule = await _context.CommissionRules.FirstOrDefaultAsync(r => r.IsActive)
                   ?? new CommissionRule();

        var viewModel = new AdminFinancialDashboardViewModel
        {
            Stats = stats,
            Bookings = bookings,
            Payouts = payouts,
            Refunds = refunds,
            CommissionRule = rule
        };

        ViewBag.Stats = stats;
        ViewBag.Bookings = bookings;
        ViewBag.Payouts = payouts;
        ViewBag.Refunds = refunds;
        ViewBag.CommissionRule = rule;

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateCommission(AdminCommissionSettingViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Thông số tỷ lệ hoa hồng không hợp lệ.";
            return RedirectToAction("Index");
        }

        await _adminService.UpdateSystemSettingAsync("CommissionSmallGroup", model.SmallGroupRate.ToString());
        await _adminService.UpdateSystemSettingAsync("CommissionLargeGroup", model.LargeGroupRate.ToString());
        await _adminService.UpdateSystemSettingAsync("CommissionGroupThreshold", model.GroupSizeThreshold.ToString());

        TempData["SuccessMessage"] = $"Đã cập nhật tỷ lệ hoa hồng: Nhóm nhỏ {(model.SmallGroupRate * 100):N0}%, Nhóm lớn (từ {model.GroupSizeThreshold} người) {(model.LargeGroupRate * 100):N0}%!";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProcessEligiblePayouts()
    {
        var count = await _financialService.ProcessEligiblePayoutsAsync();
        TempData["SuccessMessage"] = $"Đã đối soát và chuyển {count} khoản thanh toán đủ điều kiện (> 24h kết thúc ca) vào ví của các Host!";
        return RedirectToAction("Index");
    }
}
