using System.ComponentModel.DataAnnotations;
using CraftTrip.Core.Entities;

namespace CraftTrip.Web.ViewModels;

public class HostDashboardViewModel
{
    public HostProfile Profile { get; set; } = default!;
    public int ActiveWorkshopsCount { get; set; }
    public int PendingWorkshopsCount { get; set; }
    public int UpcomingBookingsCount { get; set; }
    public int TotalServedGuestsCount { get; set; }
    public decimal TotalNetEarnings { get; set; }
    public decimal AvailableWalletBalance { get; set; }
    public decimal PendingEligibilityBalance { get; set; }
    public List<Booking> RecentBookings { get; set; } = new();
    public List<Workshop> MyWorkshops { get; set; } = new();
}

public class HostWorkshopWizardViewModel
{
    public Guid? Id { get; set; }

    // STEP 1 — Thông tin cơ bản
    [Required(ErrorMessage = "Bác/Cô/Chú vui lòng đặt tên cho buổi trải nghiệm.")]
    [StringLength(150, ErrorMessage = "Tên workshop không quá 150 ký tự.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn thể loại thủ công.")]
    public Guid CategoryId { get; set; }

    public Guid? VillageId { get; set; }

    [Required(ErrorMessage = "Vui lòng viết 1-2 câu tóm tắt để khách dễ hình dung.")]
    [StringLength(250, ErrorMessage = "Mô tả ngắn tối đa 250 ký tự.")]
    public string ShortDescription { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng giới thiệu chi tiết về không gian và trải nghiệm tại xưởng.")]
    public string FullDescription { get; set; } = string.Empty;

    // STEP 2 — Hình ảnh
    public string CoverImageUrl { get; set; } = string.Empty;
    public string? GalleryImagesJson { get; set; }

    // STEP 3 — Nội dung trải nghiệm
    [Required(ErrorMessage = "Khách sẽ được làm những gì? Vui lòng liệt kê các bước.")]
    public string ExperienceSteps { get; set; } = string.Empty;

    [Required(ErrorMessage = "Khách có được mang sản phẩm về không? Sản phẩm là gì?")]
    public string TakeHomeProduct { get; set; } = string.Empty;

    [Required(ErrorMessage = "Workshop này phù hợp nhất với những ai? (VD: Gia đình, Trẻ em, Cặp đôi...)")]
    public string TargetAudience { get; set; } = string.Empty;

    public string? PreparationGuide { get; set; }
    public string? IncludedServices { get; set; }

    // STEP 4 — Giá và thời lượng
    [Required(ErrorMessage = "Vui lòng nhập giá vé cho mỗi khách tham gia.")]
    [Range(50000, 10000000, ErrorMessage = "Giá vé từ 50.000 đ đến 10.000.000 đ.")]
    public decimal BasePrice { get; set; } = 250000m;

    [Required(ErrorMessage = "Thời lượng trải nghiệm (phút).")]
    [Range(30, 480, ErrorMessage = "Thời lượng từ 30 đến 480 phút.")]
    public int DurationMinutes { get; set; } = 120;

    [Range(1, 100)]
    public int MinParticipants { get; set; } = 1;

    [Range(1, 100)]
    public int MaxParticipants { get; set; } = 15;

    [Required(ErrorMessage = "Địa chỉ xưởng tổ chức để khách tìm đường đến.")]
    public string LocationAddress { get; set; } = string.Empty;
}

public class HostScheduleManageViewModel
{
    public Guid WorkshopId { get; set; }
    public string WorkshopTitle { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    public DateTime SelectedDate { get; set; } = DateTime.UtcNow.Date;

    public TimeSpan StartTime { get; set; } = new TimeSpan(9, 0, 0);
    public TimeSpan EndTime { get; set; } = new TimeSpan(11, 30, 0);

    [Range(1, 100, ErrorMessage = "Sức chứa tối đa từ 1 đến 100 khách.")]
    public int MaxCapacity { get; set; } = 10;

    public List<WorkshopSchedule> ExistingSchedules { get; set; } = new();
}

public class HostCheckInViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập hoặc quét mã vé QR.")]
    public string TicketCode { get; set; } = string.Empty;

    public Booking? CheckedInBooking { get; set; }
    public string? StatusMessage { get; set; }
    public bool IsSuccess { get; set; }
}

public class HostFinanceViewModel
{
    public CraftTrip.Services.DTOs.HostFinancialSummaryDto Summary { get; set; } = new();
    public List<CraftTrip.Core.Entities.HostWalletTransaction> Transactions { get; set; } = new();
    public List<CraftTrip.Core.Entities.HostPayout> Payouts { get; set; } = new();
}
