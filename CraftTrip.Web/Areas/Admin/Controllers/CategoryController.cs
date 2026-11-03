using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Infrastructure.Data;

namespace CraftTrip.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class CategoryController : Controller
{
    private readonly CraftTripDbContext _context;

    public CategoryController(CraftTripDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var categories = await _context.CraftCategories
            .AsNoTracking()
            .Include(c => c.Workshops)
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        return View(categories);
    }

    [HttpGet]
    public IActionResult Create() => View(new CraftCategory());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CraftCategory category)
    {
        if (!ModelState.IsValid) return View(category);

        category.Slug = System.Text.RegularExpressions.Regex.Replace(category.Name.ToLower().Trim(), @"[^a-z0-9\s-]", "").Replace(" ", "-");
        _context.CraftCategories.Add(category);
        await _context.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đã thêm danh mục mới thành công!";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var cat = await _context.CraftCategories.FindAsync(id);
        if (cat == null || cat.IsDeleted) return NotFound();
        return View(cat);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, CraftCategory model)
    {
        var cat = await _context.CraftCategories.FindAsync(id);
        if (cat == null || cat.IsDeleted) return NotFound();

        cat.Name = model.Name.Trim();
        cat.Description = model.Description.Trim();
        cat.IconClass = model.IconClass;
        cat.ImageUrl = model.ImageUrl;
        cat.DisplayOrder = model.DisplayOrder;
        cat.IsFeatured = model.IsFeatured;

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã cập nhật danh mục thủ công!";
        return RedirectToAction("Index");
    }
}
