using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;
using CraftTrip.Core.Interfaces;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.DTOs;
using CraftTrip.Services.Interfaces;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Areas.Host.Controllers;

[Area("Host")]
[Authorize(Roles = "Host,Admin")]
public class WorkshopController : Controller
{
    private readonly IWorkshopService _workshopService;
    private readonly IMediaStorageService _mediaStorageService;
    private readonly CraftTripDbContext _context;

    public WorkshopController(
        IWorkshopService workshopService,
        IMediaStorageService mediaStorageService,
        CraftTripDbContext context)
    {
        _workshopService = workshopService;
        _mediaStorageService = mediaStorageService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var workshops = await _workshopService.GetHostWorkshopsAsync(hostId);
        return View(workshops);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadDropdownsAsync();
        return View(new HostWorkshopWizardViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HostWorkshopWizardViewModel model, string? actionType)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(model);
        }

        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var dto = new WorkshopCreateDto
        {
            Title = model.Title,
            CategoryId = model.CategoryId,
            VillageId = model.VillageId,
            ShortDescription = model.ShortDescription,
            FullDescription = model.FullDescription,
            ExperienceSteps = model.ExperienceSteps,
            TakeHomeProduct = model.TakeHomeProduct,
            TargetAudience = model.TargetAudience,
            PreparationGuide = model.PreparationGuide ?? "Trang phục thoải mái, dễ giặt.",
            IncludedServices = model.IncludedServices ?? "Nguyên vật liệu, trà nước đón tiếp, đóng gói mang về.",
            BasePrice = model.BasePrice,
            DurationMinutes = model.DurationMinutes,
            MinParticipants = model.MinParticipants,
            MaxParticipants = model.MaxParticipants,
            LocationAddress = model.LocationAddress,
            CoverImageUrl = string.IsNullOrEmpty(model.CoverImageUrl)
                ? "https://images.unsplash.com/photo-1578749556568-bc2c40e68b61?auto=format&fit=crop&w=1000&q=80"
                : model.CoverImageUrl
        };

        var workshopId = await _workshopService.CreateWorkshopAsync(dto, hostId);

        if (actionType == "submit")
        {
            await _workshopService.SubmitForModerationAsync(workshopId, hostId);
            TempData["SuccessMessage"] = "Đã gửi hồ sơ Workshop đến ban quản trị để duyệt!";
        }
        else
        {
            TempData["SuccessMessage"] = "Đã lưu bản nháp Workshop thành công!";
        }

        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var workshop = await _workshopService.GetWorkshopForEditAsync(id, hostId);

        if (workshop == null)
        {
            return NotFound("Không tìm thấy Workshop hoặc bạn không có quyền chỉnh sửa.");
        }

        var model = new HostWorkshopWizardViewModel
        {
            Id = workshop.Id,
            Title = workshop.Title,
            CategoryId = workshop.CategoryId,
            VillageId = workshop.VillageId,
            ShortDescription = workshop.ShortDescription,
            FullDescription = workshop.FullDescription,
            ExperienceSteps = workshop.ExperienceSteps,
            TakeHomeProduct = workshop.TakeHomeProduct,
            TargetAudience = workshop.TargetAudience,
            PreparationGuide = workshop.PreparationGuide,
            IncludedServices = workshop.IncludedServices,
            BasePrice = workshop.BasePrice,
            DurationMinutes = workshop.DurationMinutes,
            MinParticipants = workshop.MinParticipants,
            MaxParticipants = workshop.MaxParticipants,
            LocationAddress = workshop.LocationAddress,
            CoverImageUrl = workshop.CoverImageUrl
        };

        ViewBag.WorkshopStatus = workshop.Status;
        ViewBag.HasPendingRevision = workshop.Revisions.Any(r => r.Status == RevisionStatus.Pending);

        await LoadDropdownsAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, HostWorkshopWizardViewModel model)
    {
        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var workshop = await _workshopService.GetWorkshopForEditAsync(id, hostId);

        if (workshop == null)
        {
            return NotFound("Không tìm thấy Workshop.");
        }

        if (!ModelState.IsValid)
        {
            await LoadDropdownsAsync();
            return View(model);
        }

        if (workshop.Status == WorkshopStatus.Published)
        {
            // For Published workshop: submit a revision for moderation (Part 4.2 & BR_HS_05)
            var revisionDto = new WorkshopRevisionDto
            {
                WorkshopId = workshop.Id,
                ProposedTitle = model.Title,
                ProposedShortDescription = model.ShortDescription,
                ProposedFullDescription = model.FullDescription,
                ProposedBasePrice = model.BasePrice,
                ProposedDurationMinutes = model.DurationMinutes,
                ProposedExperienceSteps = model.ExperienceSteps,
                ProposedIncludedServices = model.IncludedServices ?? string.Empty,
                ProposedPreparationGuide = model.PreparationGuide ?? string.Empty,
                ProposedCoverImageUrl = model.CoverImageUrl
            };

            await _workshopService.ProposeRevisionAsync(revisionDto, hostId);
            TempData["SuccessMessage"] = "Các thay đổi đã được gửi đến ban quản trị để duyệt. Bản công khai trước đó vẫn được giữ nguyên cho tới khi duyệt xong.";
        }
        else
        {
            // For Draft or Rejected: update directly
            workshop.Title = model.Title.Trim();
            workshop.CategoryId = model.CategoryId;
            workshop.VillageId = model.VillageId;
            workshop.ShortDescription = model.ShortDescription.Trim();
            workshop.FullDescription = model.FullDescription.Trim();
            workshop.ExperienceSteps = model.ExperienceSteps;
            workshop.TakeHomeProduct = model.TakeHomeProduct;
            workshop.TargetAudience = model.TargetAudience;
            workshop.PreparationGuide = model.PreparationGuide ?? string.Empty;
            workshop.IncludedServices = model.IncludedServices ?? string.Empty;
            workshop.BasePrice = model.BasePrice;
            workshop.DurationMinutes = model.DurationMinutes;
            workshop.MinParticipants = model.MinParticipants;
            workshop.MaxParticipants = model.MaxParticipants;
            workshop.LocationAddress = model.LocationAddress.Trim();
            if (!string.IsNullOrEmpty(model.CoverImageUrl)) workshop.CoverImageUrl = model.CoverImageUrl;

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã cập nhật thông tin Workshop!";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitForReview(Guid id)
    {
        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var success = await _workshopService.SubmitForModerationAsync(id, hostId);

        if (success)
        {
            TempData["SuccessMessage"] = "Đã gửi duyệt thành công! Ban quản trị sẽ phản hồi trong vòng 24 giờ.";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể gửi duyệt workshop này.";
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> UploadImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "Không có tệp tải lên." });
        }

        try
        {
            using var stream = file.OpenReadStream();
            var url = await _mediaStorageService.UploadFileAsync(stream, file.FileName, file.ContentType, "workshops");
            return Json(new { url });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private async Task LoadDropdownsAsync()
    {
        ViewBag.Categories = new SelectList(
            await _context.CraftCategories.AsNoTracking().Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToListAsync(),
            "Id", "Name");

        ViewBag.Villages = new SelectList(
            await _context.Villages.AsNoTracking().Where(v => !v.IsDeleted).OrderBy(v => v.Name).ToListAsync(),
            "Id", "Name");
    }
}
