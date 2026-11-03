using CraftTrip.Core.Enums;

namespace CraftTrip.Services.DTOs;

public class WorkshopCreateDto
{
    public string Title { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Guid? VillageId { get; set; }
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public string ExperienceSteps { get; set; } = string.Empty;
    public string TakeHomeProduct { get; set; } = string.Empty;
    public string TargetAudience { get; set; } = string.Empty;
    public string PreparationGuide { get; set; } = string.Empty;
    public string IncludedServices { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public int DurationMinutes { get; set; } = 120;
    public int MinParticipants { get; set; } = 1;
    public int MaxParticipants { get; set; } = 15;
    public string LocationAddress { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    public List<string> GalleryImageUrls { get; set; } = new();
}

public class WorkshopRevisionDto
{
    public Guid WorkshopId { get; set; }
    public string ProposedTitle { get; set; } = string.Empty;
    public string ProposedShortDescription { get; set; } = string.Empty;
    public string ProposedFullDescription { get; set; } = string.Empty;
    public decimal ProposedBasePrice { get; set; }
    public int ProposedDurationMinutes { get; set; }
    public string ProposedExperienceSteps { get; set; } = string.Empty;
    public string ProposedIncludedServices { get; set; } = string.Empty;
    public string ProposedPreparationGuide { get; set; } = string.Empty;
    public string? ProposedCoverImageUrl { get; set; }
}

public class BookingCreateDto
{
    public Guid WorkshopId { get; set; }
    public Guid SlotId { get; set; }
    public int Quantity { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? SpecialNotes { get; set; }
}

public class BookingResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public Guid? BookingId { get; set; }
    public string? BookingCode { get; set; }
    public decimal TotalAmount { get; set; }

    public static BookingResult Failed(string error) => new() { Success = false, ErrorMessage = error };
    public static BookingResult Succeeded(Guid bookingId, string bookingCode, decimal totalAmount) =>
        new() { Success = true, BookingId = bookingId, BookingCode = bookingCode, TotalAmount = totalAmount };
}

public class HostFinancialSummaryDto
{
    public decimal TotalRevenue { get; set; }
    public decimal PlatformCommission { get; set; }
    public decimal NetHostRevenue { get; set; }
    public decimal WalletBalance { get; set; }
    public decimal PendingBalance { get; set; }
    public decimal EligiblePayout { get; set; }
    public int TotalBookingsCount { get; set; }
    public int CompletedBookingsCount { get; set; }

    public decimal TotalGross => TotalRevenue;
    public decimal TotalNet => NetHostRevenue;
    public decimal AvailableBalance => WalletBalance;
}

public class AdminDashboardStatsDto
{
    public int TotalUsersCount { get; set; }
    public int TotalHostsCount { get; set; }
    public int TotalWorkshopsCount { get; set; }
    public int PendingWorkshopsCount { get; set; }
    public int TotalBookingsCount { get; set; }
    public decimal TotalMarketplaceVolume { get; set; }
    public decimal TotalPlatformEarnings { get; set; }
}

public class AdminFinancialSummaryDto
{
    public decimal TotalGrossBookingAmount { get; set; }
    public decimal TotalPlatformCommission { get; set; }
    public decimal TotalPaidToHosts { get; set; }
    public decimal TotalPendingPayouts { get; set; }
    public decimal TotalRefundedAmount { get; set; }
    public int TotalPaidBookingsCount { get; set; }
    public int TotalRefundsCount { get; set; }
    public int TotalPayoutsCount { get; set; }
}
