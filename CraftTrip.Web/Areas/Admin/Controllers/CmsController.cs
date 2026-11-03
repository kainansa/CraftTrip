using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Interfaces;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Interfaces;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CmsController : Controller
{
    private readonly ICmsService _cmsService;
    private readonly IMediaStorageService _mediaStorageService;
    private readonly CraftTripDbContext _context;

    public CmsController(
        ICmsService cmsService,
        IMediaStorageService mediaStorageService,
        CraftTripDbContext context)
    {
        _cmsService = cmsService;
        _mediaStorageService = mediaStorageService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var pages = await _context.ContentPages
            .AsNoTracking()
            .Include(p => p.Sections.OrderBy(s => s.DisplayOrder))
            .Where(p => !p.IsDeleted)
            .ToListAsync();

        return View(pages);
    }

    [HttpGet]
    public async Task<IActionResult> EditSection(Guid id)
    {
        var section = await _context.ContentSections
            .Include(s => s.Page)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (section == null) return NotFound("Không tìm thấy mục nội dung.");

        var model = new AdminSectionEditViewModel
        {
            SectionId = section.Id,
            SectionKey = section.SectionKey,
            PageKey = section.Page?.PageKey ?? "home",
            Title = section.Title,
            Subtitle = section.Subtitle,
            ContentHtml = section.ContentHtml,
            PrimaryCtaText = section.PrimaryCtaText,
            PrimaryCtaUrl = section.PrimaryCtaUrl,
            SecondaryCtaText = section.SecondaryCtaText,
            SecondaryCtaUrl = section.SecondaryCtaUrl,
            BackgroundImageUrl = section.BackgroundImageUrl
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditSection(AdminSectionEditViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var success = await _cmsService.UpdateSectionAsync(
            model.SectionId,
            model.Title,
            model.Subtitle,
            model.ContentHtml,
            model.PrimaryCtaText,
            model.PrimaryCtaUrl,
            model.BackgroundImageUrl);

        if (success)
        {
            TempData["SuccessMessage"] = "Đã cập nhật nội dung mục website thành công!";
        }
        else
        {
            TempData["ErrorMessage"] = "Không thể cập nhật nội dung.";
        }

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Media()
    {
        var assets = await _cmsService.GetAllMediaAssetsAsync();
        return View(assets);
    }

    [HttpPost]
    public async Task<IActionResult> UploadMedia(IFormFile file, string? caption)
    {
        if (file == null || file.Length == 0)
        {
            TempData["ErrorMessage"] = "Vui lòng chọn tệp hình ảnh để tải lên.";
            return RedirectToAction("Media");
        }

        using var stream = file.OpenReadStream();
        var url = await _mediaStorageService.UploadFileAsync(stream, file.FileName, file.ContentType, "cms");
        await _cmsService.SaveMediaAssetAsync(file.FileName, file.FileName, url, file.Length, file.ContentType, caption);

        TempData["SuccessMessage"] = "Tải hình ảnh lên thư viện thành công!";
        return RedirectToAction("Media");
    }
}
