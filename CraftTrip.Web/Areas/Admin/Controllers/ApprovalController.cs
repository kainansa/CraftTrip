using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ApprovalController : Controller
{
    private readonly IAdminService _adminService;

    public ApprovalController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    public async Task<IActionResult> Index()
    {
        var pendingWorkshops = await _adminService.GetPendingWorkshopsAsync();
        var pendingRevisions = await _adminService.GetPendingRevisionsAsync();

        ViewBag.PendingRevisions = pendingRevisions;
        return View(pendingWorkshops);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveWorkshop(Guid workshopId)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _adminService.ApproveWorkshopAsync(workshopId, adminId);

        if (success)
        {
            TempData["SuccessMessage"] = "Đã duyệt và công khai Workshop lên sàn!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể duyệt workshop này.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectWorkshop(Guid workshopId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối để Host biết cần sửa gì.";
            return RedirectToAction("Index");
        }

        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _adminService.RejectWorkshopAsync(workshopId, adminId, reason);

        if (success)
        {
            TempData["SuccessMessage"] = "Đã gửi thông báo từ chối kèm lý do cho Host.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể từ chối workshop này.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRevision(Guid revisionId)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _adminService.ApproveRevisionAsync(revisionId, adminId);

        if (success)
        {
            TempData["SuccessMessage"] = "Đã duyệt và cập nhật nội dung chỉnh sửa mới vào Workshop!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể duyệt phiên bản chỉnh sửa này.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RejectRevision(Guid revisionId, string reason)
    {
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _adminService.RejectRevisionAsync(revisionId, adminId, reason ?? "Nội dung chỉnh sửa không phù hợp quy chuẩn.");

        if (success)
        {
            TempData["SuccessMessage"] = "Đã từ chối phiên bản chỉnh sửa.";
        }

        return RedirectToAction("Index");
    }
}
