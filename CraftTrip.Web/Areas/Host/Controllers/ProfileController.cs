using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Areas.Host.Controllers;

[Area("Host")]
[Authorize(Roles = "Host,Admin")]
public class ProfileController : Controller
{
    private readonly CraftTripDbContext _context;

    public ProfileController(CraftTripDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var profile = await _context.HostProfiles
            .Include(h => h.User)
            .Include(h => h.Village)
            .FirstOrDefaultAsync(h => h.UserId == hostId);

        if (profile == null)
        {
            return RedirectToAction("Index", "Onboarding", new { area = "Host" });
        }

        ViewBag.Villages = new SelectList(
            await _context.Villages.AsNoTracking().Where(v => !v.IsDeleted).OrderBy(v => v.Name).ToListAsync(),
            "Id", "Name", profile.VillageId);

        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(HostApplyViewModel model)
    {
        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var profile = await _context.HostProfiles.FirstOrDefaultAsync(h => h.UserId == hostId);
        if (profile == null) return NotFound();

        profile.StudioName = model.StudioName.Trim();
        profile.CraftType = model.CraftType.Trim();
        profile.Bio = model.Bio.Trim();
        profile.VillageId = model.VillageId;
        profile.Address = model.Address.Trim();
        profile.BankAccountName = model.BankAccountName.Trim().ToUpper();
        profile.BankAccountNumber = model.BankAccountNumber.Trim();
        profile.BankName = model.BankName.Trim();

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã lưu thông tin hồ sơ cơ sở thành công!";

        return RedirectToAction("Index");
    }
}
