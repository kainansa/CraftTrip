using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.DTOs;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Services.Implementations;

public class AdminService : IAdminService
{
    private readonly CraftTripDbContext _context;
    private readonly INotificationService _notificationService;

    public AdminService(CraftTripDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    public async Task<AdminDashboardStatsDto> GetDashboardStatsAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var totalHosts = await _context.HostProfiles.CountAsync();
        var totalWorkshops = await _context.Workshops.CountAsync(w => !w.IsDeleted);
        var pendingWorkshops = await _context.Workshops.CountAsync(w => w.Status == WorkshopStatus.Pending && !w.IsDeleted);

        var completedBookings = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed)
            .ToListAsync();

        return new AdminDashboardStatsDto
        {
            TotalUsersCount = totalUsers,
            TotalHostsCount = totalHosts,
            TotalWorkshopsCount = totalWorkshops,
            PendingWorkshopsCount = pendingWorkshops,
            TotalBookingsCount = completedBookings.Count,
            TotalMarketplaceVolume = completedBookings.Sum(b => b.TotalAmount),
            TotalPlatformEarnings = completedBookings.Sum(b => b.PlatformFee)
        };
    }

    public async Task<List<Workshop>> GetPendingWorkshopsAsync()
    {
        return await _context.Workshops
            .AsNoTracking()
            .Include(w => w.Host)
                .ThenInclude(h => h!.HostProfile)
            .Include(w => w.Category)
            .Include(w => w.Village)
            .Where(w => w.Status == WorkshopStatus.Pending && !w.IsDeleted)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ApproveWorkshopAsync(Guid workshopId, Guid adminId)
    {
        var workshop = await _context.Workshops.FirstOrDefaultAsync(w => w.Id == workshopId && !w.IsDeleted);
        if (workshop == null) return false;

        workshop.Status = WorkshopStatus.Published;
        workshop.RejectionReason = null;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = adminId,
            Action = "ApproveWorkshop",
            EntityName = "Workshop",
            EntityId = workshopId.ToString(),
            Details = $"Admin duyệt workshop '{workshop.Title}'"
        });

        await _context.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(
            workshop.HostId,
            "Workshop của bạn đã được phê duyệt!",
            $"Trải nghiệm '{workshop.Title}' đã được ban quản trị phê duyệt và chính thức hiển thị trên trang chủ CraftTrip.",
            $"/Workshop/Details/{workshop.Id}",
            "Moderation");

        return true;
    }

    public async Task<bool> RejectWorkshopAsync(Guid workshopId, Guid adminId, string reason)
    {
        var workshop = await _context.Workshops.FirstOrDefaultAsync(w => w.Id == workshopId && !w.IsDeleted);
        if (workshop == null) return false;

        workshop.Status = WorkshopStatus.Rejected;
        workshop.RejectionReason = reason.Trim();

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = adminId,
            Action = "RejectWorkshop",
            EntityName = "Workshop",
            EntityId = workshopId.ToString(),
            Details = $"Admin từ chối workshop '{workshop.Title}'. Lý do: {reason}"
        });

        await _context.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(
            workshop.HostId,
            "Cần chỉnh sửa thông tin Workshop",
            $"Trải nghiệm '{workshop.Title}' chưa được duyệt. Lý do: {reason}. Vui lòng cập nhật lại để gửi duyệt.",
            $"/Host/Workshop/Edit/{workshop.Id}",
            "Moderation");

        return true;
    }

    public async Task<List<WorkshopRevision>> GetPendingRevisionsAsync()
    {
        return await _context.WorkshopRevisions
            .AsNoTracking()
            .Include(r => r.Workshop)
                .ThenInclude(w => w!.Host)
            .Where(r => r.Status == RevisionStatus.Pending)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync();
    }

    public async Task<bool> ApproveRevisionAsync(Guid revisionId, Guid adminId)
    {
        var revision = await _context.WorkshopRevisions
            .Include(r => r.Workshop)
            .FirstOrDefaultAsync(r => r.Id == revisionId);

        if (revision == null || revision.Workshop == null) return false;

        var workshop = revision.Workshop;

        // Apply proposed changes to the published workshop
        workshop.Title = revision.ProposedTitle;
        workshop.ShortDescription = revision.ProposedShortDescription;
        workshop.FullDescription = revision.ProposedFullDescription;
        workshop.BasePrice = revision.ProposedBasePrice;
        workshop.DurationMinutes = revision.ProposedDurationMinutes;
        workshop.ExperienceSteps = revision.ProposedExperienceSteps;
        workshop.IncludedServices = revision.ProposedIncludedServices;
        workshop.PreparationGuide = revision.ProposedPreparationGuide;

        if (!string.IsNullOrEmpty(revision.ProposedCoverImageUrl))
        {
            workshop.CoverImageUrl = revision.ProposedCoverImageUrl;
        }

        revision.Status = RevisionStatus.Approved;
        revision.ReviewedAt = DateTime.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = adminId,
            Action = "ApproveWorkshopRevision",
            EntityName = "WorkshopRevision",
            EntityId = revisionId.ToString(),
            Details = $"Admin duyệt phiên bản chỉnh sửa của workshop '{workshop.Title}'"
        });

        await _context.SaveChangesAsync();

        await _notificationService.SendNotificationAsync(
            workshop.HostId,
            "Nội dung chỉnh sửa đã được duyệt",
            $"Các thay đổi đối với workshop '{workshop.Title}' đã được phê duyệt và cập nhật công khai.",
            $"/Workshop/Details/{workshop.Id}",
            "Moderation");

        return true;
    }

    public async Task<bool> RejectRevisionAsync(Guid revisionId, Guid adminId, string reason)
    {
        var revision = await _context.WorkshopRevisions
            .Include(r => r.Workshop)
            .FirstOrDefaultAsync(r => r.Id == revisionId);

        if (revision == null) return false;

        revision.Status = RevisionStatus.Rejected;
        revision.AdminFeedback = reason.Trim();
        revision.ReviewedAt = DateTime.UtcNow;

        _context.AuditLogs.Add(new AuditLog
        {
            UserId = adminId,
            Action = "RejectWorkshopRevision",
            EntityName = "WorkshopRevision",
            EntityId = revisionId.ToString(),
            Details = $"Admin từ chối phiên bản chỉnh sửa. Lý do: {reason}"
        });

        await _context.SaveChangesAsync();

        if (revision.Workshop != null)
        {
            await _notificationService.SendNotificationAsync(
                revision.Workshop.HostId,
                "Chỉnh sửa workshop bị từ chối",
                $"Nội dung chỉnh sửa workshop '{revision.Workshop.Title}' chưa được phê duyệt. Lý do: {reason}",
                $"/Host/Workshop/Edit/{revision.Workshop.Id}",
                "Moderation");
        }

        return true;
    }

    public async Task<List<ApplicationUser>> GetAllUsersAsync()
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.HostProfile)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> ToggleUserStatusAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null) return false;

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<HostProfile>> GetPendingHostVerificationsAsync()
    {
        return await _context.HostProfiles
            .AsNoTracking()
            .Include(h => h.User)
            .Include(h => h.Village)
            .Where(h => h.VerificationStatus == HostVerificationStatus.Pending)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<bool> VerifyHostAsync(Guid hostProfileId, bool isApproved, string? note)
    {
        var host = await _context.HostProfiles.Include(h => h.User).FirstOrDefaultAsync(h => h.Id == hostProfileId);
        if (host == null) return false;

        host.VerificationStatus = isApproved ? HostVerificationStatus.Verified : HostVerificationStatus.Rejected;
        host.VerificationNote = note;

        await _context.SaveChangesAsync();

        if (host.User != null)
        {
            await _notificationService.SendNotificationAsync(
                host.User.Id,
                isApproved ? "Hồ sơ xưởng đã được xác thực!" : "Hồ sơ xưởng cần bổ sung thông tin",
                isApproved ? "Chúc mừng! Cơ sở của bạn đã được chứng nhận là Đối tác Làng nghề CraftTrip." : $"Hồ sơ chưa được xác nhận: {note}",
                "/Host/Profile",
                "Verification");
        }

        return true;
    }

    public async Task<List<SystemSetting>> GetSystemSettingsAsync()
    {
        return await _context.SystemSettings.AsNoTracking().OrderBy(s => s.Category).ThenBy(s => s.Key).ToListAsync();
    }

    public async Task<bool> UpdateSystemSettingAsync(string key, string value)
    {
        var setting = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            setting = new SystemSetting { Key = key, Value = value, Description = key };
            _context.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
        }

        // If commission rule settings are updated, also sync with active CommissionRule entity
        if (key == "CommissionSmallGroup" || key == "CommissionLargeGroup" || key == "CommissionGroupThreshold")
        {
            var rule = await _context.CommissionRules.FirstOrDefaultAsync(r => r.IsActive);
            if (rule != null)
            {
                if (key == "CommissionSmallGroup" && decimal.TryParse(value, out var small)) rule.SmallGroupRate = small;
                if (key == "CommissionLargeGroup" && decimal.TryParse(value, out var large)) rule.LargeGroupRate = large;
                if (key == "CommissionGroupThreshold" && int.TryParse(value, out var thresh)) rule.GroupSizeThreshold = thresh;
            }
        }

        await _context.SaveChangesAsync();
        return true;
    }
}
