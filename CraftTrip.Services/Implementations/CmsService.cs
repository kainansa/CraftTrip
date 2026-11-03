using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Services.Implementations;

public class CmsService : ICmsService
{
    private readonly CraftTripDbContext _context;

    public CmsService(CraftTripDbContext context)
    {
        _context = context;
    }

    public async Task<ContentPage?> GetPageContentAsync(string pageKey)
    {
        return await _context.ContentPages
            .AsNoTracking()
            .Include(p => p.Sections.Where(s => s.IsActive).OrderBy(s => s.DisplayOrder))
            .FirstOrDefaultAsync(p => p.PageKey == pageKey && p.IsPublished && !p.IsDeleted);
    }

    public async Task<List<ContentSection>> GetSectionsAsync(string pageKey)
    {
        var page = await GetPageContentAsync(pageKey);
        return page?.Sections.ToList() ?? new List<ContentSection>();
    }

    public async Task<bool> UpdateSectionAsync(
        Guid sectionId,
        string title,
        string? subtitle,
        string? contentHtml,
        string? ctaText,
        string? ctaUrl,
        string? bgUrl)
    {
        var section = await _context.ContentSections.FirstOrDefaultAsync(s => s.Id == sectionId);
        if (section == null) return false;

        section.Title = title.Trim();
        section.Subtitle = subtitle?.Trim();
        section.ContentHtml = contentHtml;
        section.PrimaryCtaText = ctaText?.Trim();
        section.PrimaryCtaUrl = ctaUrl?.Trim();
        if (!string.IsNullOrEmpty(bgUrl))
        {
            section.BackgroundImageUrl = bgUrl;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<MediaAsset>> GetAllMediaAssetsAsync()
    {
        return await _context.MediaAssets
            .AsNoTracking()
            .Where(m => !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<MediaAsset> SaveMediaAssetAsync(
        string fileName,
        string originalName,
        string url,
        long fileSize,
        string contentType,
        string? caption = null)
    {
        var asset = new MediaAsset
        {
            FileName = fileName,
            OriginalName = originalName,
            Url = url,
            FileSize = fileSize,
            ContentType = contentType,
            Caption = caption
        };

        _context.MediaAssets.Add(asset);
        await _context.SaveChangesAsync();
        return asset;
    }
}
