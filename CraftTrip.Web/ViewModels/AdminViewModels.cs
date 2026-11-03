using System.ComponentModel.DataAnnotations;
using CraftTrip.Core.Entities;
using CraftTrip.Services.DTOs;

namespace CraftTrip.Web.ViewModels;

public class AdminDashboardViewModel
{
    public AdminDashboardStatsDto Stats { get; set; } = default!;
    public List<Workshop> PendingWorkshops { get; set; } = new();
    public List<WorkshopRevision> PendingRevisions { get; set; } = new();
    public List<HostProfile> PendingHosts { get; set; } = new();
    public List<Booking> RecentBookings { get; set; } = new();
}

public class AdminSectionEditViewModel
{
    public Guid SectionId { get; set; }
    public string SectionKey { get; set; } = string.Empty;
    public string PageKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tiêu đề không được để trống.")]
    public string Title { get; set; } = string.Empty;

    public string? Subtitle { get; set; }
    public string? ContentHtml { get; set; }
    public string? PrimaryCtaText { get; set; }
    public string? PrimaryCtaUrl { get; set; }
    public string? SecondaryCtaText { get; set; }
    public string? SecondaryCtaUrl { get; set; }
    public string? BackgroundImageUrl { get; set; }
}

public class AdminCommissionSettingViewModel
{
    [Required]
    [Range(0, 1, ErrorMessage = "Tỷ lệ hoa hồng từ 0 đến 1 (ví dụ: 0.12 = 12%).")]
    public decimal SmallGroupRate { get; set; } = 0.12m;

    [Required]
    [Range(0, 1, ErrorMessage = "Tỷ lệ hoa hồng từ 0 đến 1 (ví dụ: 0.15 = 15%).")]
    public decimal LargeGroupRate { get; set; } = 0.15m;

    [Required]
    [Range(1, 100, ErrorMessage = "Ngưỡng số người từ 1 đến 100.")]
    public int GroupSizeThreshold { get; set; } = 5;
}

public class AdminFinancialDashboardViewModel
{
    public AdminFinancialSummaryDto Stats { get; set; } = new();
    public List<Booking> Bookings { get; set; } = new();
    public List<HostPayout> Payouts { get; set; } = new();
    public List<Refund> Refunds { get; set; } = new();
    public CommissionRule CommissionRule { get; set; } = new();
}
