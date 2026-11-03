using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Areas.Host.Controllers;

[Area("Host")]
[Authorize]
public class OnboardingController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CraftTripDbContext _context;

    public OnboardingController(
        UserManager<ApplicationUser> userManager,
        CraftTripDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var existingProfile = await _context.HostProfiles.FirstOrDefaultAsync(h => h.UserId == userId);

        if (existingProfile != null)
        {
            return RedirectToAction("Index", "Dashboard", new { area = "Host" });
        }

        ViewBag.Villages = new SelectList(
            await _context.Villages.AsNoTracking().Where(v => !v.IsDeleted).OrderBy(v => v.Name).ToListAsync(),
            "Id", "Name");

        return View(new HostApplyViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(HostApplyViewModel model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Villages = new SelectList(
                await _context.Villages.AsNoTracking().Where(v => !v.IsDeleted).OrderBy(v => v.Name).ToListAsync(),
                "Id", "Name");
            return View(model);
        }

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user == null) return NotFound();

        var hostProfile = new HostProfile
        {
            UserId = userId,
            StudioName = model.StudioName.Trim(),
            CraftType = model.CraftType.Trim(),
            Bio = model.Bio.Trim(),
            VillageId = model.VillageId,
            Address = model.Address.Trim(),
            IdCardNumber = model.IdCardNumber.Trim(),
            BankAccountName = model.BankAccountName.Trim().ToUpper(),
            BankAccountNumber = model.BankAccountNumber.Trim(),
            BankName = model.BankName.Trim(),
            VerificationStatus = HostVerificationStatus.Pending
        };

        _context.HostProfiles.Add(hostProfile);

        user.UserRole = UserRole.Host;
        await _userManager.AddToRoleAsync(user, "Host");
        await _userManager.UpdateAsync(user);

        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đăng ký mở xưởng thành công! Bạn có thể bắt đầu tạo Workshop đầu tiên.";
        return RedirectToAction("Index", "Dashboard", new { area = "Host" });
    }
}
