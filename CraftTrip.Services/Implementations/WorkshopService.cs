using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.DTOs;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Services.Implementations;

public class WorkshopService : IWorkshopService
{
    private readonly CraftTripDbContext _context;

    public WorkshopService(CraftTripDbContext context)
    {
        _context = context;
    }

    public async Task<List<Workshop>> GetFeaturedWorkshopsAsync(int count = 6)
    {
        return await _context.Workshops
            .AsNoTracking()
            .Include(w => w.Category)
            .Include(w => w.Village)
            .Include(w => w.Host)
            .Where(w => w.Status == WorkshopStatus.Published && !w.IsDeleted)
            .OrderByDescending(w => w.IsFeatured)
            .ThenByDescending(w => w.AverageRating)
            .Take(count)
            .ToListAsync();
    }

    public async Task<(List<Workshop> Workshops, int TotalCount)> SearchWorkshopsAsync(
        string? keyword,
        Guid? categoryId,
        Guid? villageId,
        decimal? minPrice,
        decimal? maxPrice,
        int? maxDuration,
        DateTime? availableDate,
        string? sortBy,
        int page = 1,
        int pageSize = 9)
    {
        var query = _context.Workshops
            .AsNoTracking()
            .Include(w => w.Category)
            .Include(w => w.Village)
            .Include(w => w.Host)
            .Where(w => w.Status == WorkshopStatus.Published && !w.IsDeleted);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim().ToLower();
            query = query.Where(w => w.Title.ToLower().Contains(kw) ||
                                     w.ShortDescription.ToLower().Contains(kw) ||
                                     (w.Village != null && w.Village.Name.ToLower().Contains(kw)));
        }

        if (categoryId.HasValue && categoryId != Guid.Empty)
        {
            query = query.Where(w => w.CategoryId == categoryId.Value);
        }

        if (villageId.HasValue && villageId != Guid.Empty)
        {
            query = query.Where(w => w.VillageId == villageId.Value);
        }

        if (minPrice.HasValue && minPrice.Value > 0)
        {
            query = query.Where(w => w.BasePrice >= minPrice.Value);
        }

        if (maxPrice.HasValue && maxPrice.Value > 0)
        {
            query = query.Where(w => w.BasePrice <= maxPrice.Value);
        }

        if (maxDuration.HasValue && maxDuration.Value > 0)
        {
            query = query.Where(w => w.DurationMinutes <= maxDuration.Value);
        }

        if (availableDate.HasValue)
        {
            var date = availableDate.Value.Date;
            query = query.Where(w => w.Schedules.Any(s => s.Date.Date == date && s.Slots.Any(sl => sl.AvailableCapacity > 0)));
        }

        query = sortBy switch
        {
            "price-asc" => query.OrderBy(w => w.BasePrice),
            "price-desc" => query.OrderByDescending(w => w.BasePrice),
            "rating" => query.OrderByDescending(w => w.AverageRating),
            "newest" => query.OrderByDescending(w => w.CreatedAt),
            _ => query.OrderByDescending(w => w.IsFeatured).ThenByDescending(w => w.CreatedAt)
        };

        var totalCount = await query.CountAsync();
        var workshops = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (workshops, totalCount);
    }

    public async Task<Workshop?> GetWorkshopDetailsByIdAsync(Guid id)
    {
        var workshop = await _context.Workshops
            .AsNoTracking()
            .Include(w => w.Category)
            .Include(w => w.Village)
            .Include(w => w.Host)
                .ThenInclude(h => h!.HostProfile)
            .Include(w => w.Images.OrderBy(i => i.DisplayOrder))
            .Include(w => w.Schedules.Where(s => s.Date >= DateTime.UtcNow.Date).OrderBy(s => s.Date))
                .ThenInclude(s => s.Slots)
            .Include(w => w.Reviews.OrderByDescending(r => r.CreatedAt))
                .ThenInclude(r => r.Guest)
            .FirstOrDefaultAsync(w => w.Id == id && !w.IsDeleted);

        if (workshop != null)
        {
            // Bounded client-side ordering: ensure strict chronological ordering of schedules and slots
            workshop.Schedules = workshop.Schedules
                .OrderBy(s => s.Date)
                .Select(s =>
                {
                    s.Slots = s.Slots.OrderBy(sl => sl.StartTime).ThenBy(sl => sl.EndTime).ToList();
                    return s;
                })
                .ToList();
        }

        return workshop;
    }

    public async Task<Workshop?> GetWorkshopDetailsBySlugAsync(string slug)
    {
        var workshop = await _context.Workshops
            .AsNoTracking()
            .Include(w => w.Category)
            .Include(w => w.Village)
            .Include(w => w.Host)
                .ThenInclude(h => h!.HostProfile)
            .Include(w => w.Images.OrderBy(i => i.DisplayOrder))
            .Include(w => w.Schedules.Where(s => s.Date >= DateTime.UtcNow.Date).OrderBy(s => s.Date))
                .ThenInclude(s => s.Slots)
            .Include(w => w.Reviews.OrderByDescending(r => r.CreatedAt))
                .ThenInclude(r => r.Guest)
            .FirstOrDefaultAsync(w => w.Slug == slug && !w.IsDeleted);

        if (workshop != null)
        {
            // Bounded client-side ordering: ensure strict chronological ordering of schedules and slots
            workshop.Schedules = workshop.Schedules
                .OrderBy(s => s.Date)
                .Select(s =>
                {
                    s.Slots = s.Slots.OrderBy(sl => sl.StartTime).ThenBy(sl => sl.EndTime).ToList();
                    return s;
                })
                .ToList();
        }

        return workshop;
    }

    public async Task<List<Workshop>> GetHostWorkshopsAsync(Guid hostId)
    {
        return await _context.Workshops
            .AsNoTracking()
            .Include(w => w.Category)
            .Include(w => w.Village)
            .Include(w => w.Revisions.OrderByDescending(r => r.SubmittedAt))
            .Where(w => w.HostId == hostId && !w.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<Workshop?> GetWorkshopForEditAsync(Guid workshopId, Guid hostId)
    {
        var workshop = await _context.Workshops
            .Include(w => w.Images.OrderBy(i => i.DisplayOrder))
            .Include(w => w.Schedules.OrderBy(s => s.Date))
                .ThenInclude(s => s.Slots)
            .Include(w => w.Revisions.OrderByDescending(r => r.SubmittedAt))
            .FirstOrDefaultAsync(w => w.Id == workshopId && !w.IsDeleted);

        if (workshop == null || workshop.HostId != hostId)
        {
            return null; // IDOR protection: Host can only access their own workshop
        }

        workshop.Schedules = workshop.Schedules
            .OrderBy(s => s.Date)
            .Select(s =>
            {
                s.Slots = s.Slots.OrderBy(sl => sl.StartTime).ThenBy(sl => sl.EndTime).ToList();
                return s;
            })
            .ToList();

        return workshop;
    }

    public async Task<Guid> CreateWorkshopAsync(WorkshopCreateDto dto, Guid hostId)
    {
        var slug = GenerateSlug(dto.Title);
        var baseSlug = slug;
        int counter = 1;
        while (await _context.Workshops.AnyAsync(w => w.Slug == slug))
        {
            slug = $"{baseSlug}-{counter++}";
        }

        var workshop = new Workshop
        {
            HostId = hostId,
            CategoryId = dto.CategoryId,
            VillageId = dto.VillageId,
            Title = dto.Title.Trim(),
            Slug = slug,
            ShortDescription = dto.ShortDescription.Trim(),
            FullDescription = dto.FullDescription.Trim(),
            ExperienceSteps = dto.ExperienceSteps,
            TakeHomeProduct = dto.TakeHomeProduct,
            TargetAudience = dto.TargetAudience,
            PreparationGuide = dto.PreparationGuide,
            IncludedServices = dto.IncludedServices,
            BasePrice = dto.BasePrice,
            DurationMinutes = dto.DurationMinutes,
            MinParticipants = dto.MinParticipants,
            MaxParticipants = dto.MaxParticipants,
            LocationAddress = dto.LocationAddress.Trim(),
            CoverImageUrl = dto.CoverImageUrl,
            Status = WorkshopStatus.Draft // Default is Draft (BR_HS_05)
        };

        if (!string.IsNullOrEmpty(dto.CoverImageUrl))
        {
            workshop.Images.Add(new WorkshopImage
            {
                ImageUrl = dto.CoverImageUrl,
                IsCover = true,
                DisplayOrder = 1,
                Caption = "Ảnh bìa trải nghiệm"
            });
        }

        int order = 2;
        foreach (var imgUrl in dto.GalleryImageUrls.Where(u => !string.IsNullOrEmpty(u) && u != dto.CoverImageUrl))
        {
            workshop.Images.Add(new WorkshopImage
            {
                ImageUrl = imgUrl,
                IsCover = false,
                DisplayOrder = order++,
                Caption = "Ảnh chi tiết xưởng"
            });
        }

        _context.Workshops.Add(workshop);
        await _context.SaveChangesAsync();
        return workshop.Id;
    }

    public async Task<bool> SubmitForModerationAsync(Guid workshopId, Guid hostId)
    {
        var workshop = await _context.Workshops.FirstOrDefaultAsync(w => w.Id == workshopId && !w.IsDeleted);
        if (workshop == null || workshop.HostId != hostId) return false;

        workshop.Status = WorkshopStatus.Pending;
        workshop.RejectionReason = null;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ProposeRevisionAsync(WorkshopRevisionDto dto, Guid hostId)
    {
        var workshop = await _context.Workshops.FirstOrDefaultAsync(w => w.Id == dto.WorkshopId && !w.IsDeleted);
        if (workshop == null || workshop.HostId != hostId) return false;

        var revision = new WorkshopRevision
        {
            WorkshopId = dto.WorkshopId,
            ProposedTitle = dto.ProposedTitle.Trim(),
            ProposedShortDescription = dto.ProposedShortDescription.Trim(),
            ProposedFullDescription = dto.ProposedFullDescription.Trim(),
            ProposedBasePrice = dto.ProposedBasePrice,
            ProposedDurationMinutes = dto.ProposedDurationMinutes,
            ProposedExperienceSteps = dto.ProposedExperienceSteps,
            ProposedIncludedServices = dto.ProposedIncludedServices,
            ProposedPreparationGuide = dto.ProposedPreparationGuide,
            ProposedCoverImageUrl = dto.ProposedCoverImageUrl,
            Status = RevisionStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };

        _context.WorkshopRevisions.Add(revision);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteWorkshopAsync(Guid workshopId, Guid hostId)
    {
        var workshop = await _context.Workshops.FirstOrDefaultAsync(w => w.Id == workshopId && !w.IsDeleted);
        if (workshop == null || workshop.HostId != hostId) return false;

        // Soft delete
        workshop.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    private static string GenerateSlug(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Guid.NewGuid().ToString("N")[..8];
        var normalized = text.ToLowerInvariant().Trim();
        var chars = new Dictionary<char, string>
        {
            {'á',"a"},{'à',"a"},{'ả',"a"},{'ã',"a"},{'ạ',"a"},{'ă',"a"},{'ắ',"a"},{'ằ',"a"},{'ẳ',"a"},{'ẵ',"a"},{'ặ',"a"},{'â',"a"},{'ấ',"a"},{'ầ',"a"},{'ẩ',"a"},{'ẫ',"a"},{'ậ',"a"},
            {'é',"e"},{'è',"e"},{'ẻ',"e"},{'ẽ',"e"},{'ẹ',"e"},{'ê',"e"},{'ế',"e"},{'ề',"e"},{'ể',"e"},{'ễ',"e"},{'ệ',"e"},
            {'í',"i"},{'ì',"i"},{'ỉ',"i"},{'ĩ',"i"},{'ị',"i"},
            {'ó',"o"},{'ò',"o"},{'ỏ',"o"},{'õ',"o"},{'ọ',"o"},{'ô',"o"},{'ố',"o"},{'ồ',"o"},{'ổ',"o"},{'ỗ',"o"},{'ộ',"o"},{'ơ',"o"},{'ớ',"o"},{'ờ',"o"},{'ở',"o"},{'ỡ',"o"},{'ợ',"o"},
            {'ú',"u"},{'ù',"u"},{'ủ',"u"},{'ũ',"u"},{'ụ',"u"},{'ư',"u"},{'ứ',"u"},{'ừ',"u"},{'ử',"u"},{'ữ',"u"},{'ự',"u"},
            {'ý',"y"},{'ỳ',"y"},{'ỷ',"y"},{'ỹ',"y"},{'ỵ',"y"},
            {'đ',"d"}
        };
        var sb = new System.Text.StringBuilder();
        foreach (var c in normalized)
        {
            if (chars.TryGetValue(c, out var repl))
                sb.Append(repl);
            else if (char.IsLetterOrDigit(c))
                sb.Append(c);
            else if (char.IsWhiteSpace(c) || c == '-')
                sb.Append('-');
        }
        var result = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-+", "-").Trim('-');
        return string.IsNullOrEmpty(result) ? Guid.NewGuid().ToString("N")[..8] : result;
    }
}
