using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Web.Areas.Host.Controllers;

[Area("Host")]
[Authorize(Roles = "Host,Admin")]
public class FinanceController : Controller
{
    private readonly IFinancialService _financialService;
    private readonly CraftTripDbContext _context;

    public FinanceController(IFinancialService financialService, CraftTripDbContext context)
    {
        _financialService = financialService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var summary = await _financialService.GetHostFinancialSummaryAsync(hostId);

        var transactions = await _context.HostWalletTransactions
            .AsNoTracking()
            .Where(t => t.HostId == hostId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(20)
            .ToListAsync();

        var payouts = await _context.HostPayouts
            .AsNoTracking()
            .Include(p => p.Booking)
            .Where(p => p.HostId == hostId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(20)
            .ToListAsync();

        var viewModel = new CraftTrip.Web.ViewModels.HostFinanceViewModel
        {
            Summary = summary,
            Transactions = transactions,
            Payouts = payouts
        };

        ViewBag.Summary = summary;
        ViewBag.Transactions = transactions;
        ViewBag.Payouts = payouts;

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Withdraw(decimal amount, string? note)
    {
        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var success = await _financialService.RequestPayoutWithdrawalAsync(hostId, amount, note);
        if (!success)
        {
            TempData["ErrorMessage"] = "Số dư ví không đủ hoặc số tiền rút không hợp lệ.";
            return RedirectToAction("Index");
        }

        TempData["SuccessMessage"] = $"Đã tạo lệnh chuyển khoản {amount:N0} đ về tài khoản ngân hàng của bạn.";
        return RedirectToAction("Index");
    }
}
