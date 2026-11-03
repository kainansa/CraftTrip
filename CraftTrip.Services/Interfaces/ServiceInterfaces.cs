using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;
using CraftTrip.Services.DTOs;

namespace CraftTrip.Services.Interfaces;

public interface IWorkshopService
{
    Task<List<Workshop>> GetFeaturedWorkshopsAsync(int count = 6);
    Task<(List<Workshop> Workshops, int TotalCount)> SearchWorkshopsAsync(
        string? keyword,
        Guid? categoryId,
        Guid? villageId,
        decimal? minPrice,
        decimal? maxPrice,
        int? maxDuration,
        DateTime? availableDate,
        string? sortBy,
        int page = 1,
        int pageSize = 9);
    Task<Workshop?> GetWorkshopDetailsByIdAsync(Guid id);
    Task<Workshop?> GetWorkshopDetailsBySlugAsync(string slug);
    Task<List<Workshop>> GetHostWorkshopsAsync(Guid hostId);
    Task<Workshop?> GetWorkshopForEditAsync(Guid workshopId, Guid hostId);
    Task<Guid> CreateWorkshopAsync(WorkshopCreateDto dto, Guid hostId);
    Task<bool> SubmitForModerationAsync(Guid workshopId, Guid hostId);
    Task<bool> ProposeRevisionAsync(WorkshopRevisionDto dto, Guid hostId);
    Task<bool> DeleteWorkshopAsync(Guid workshopId, Guid hostId);
}

public interface IBookingService
{
    Task<BookingResult> CreatePendingBookingAsync(BookingCreateDto dto, Guid guestId);
    Task<bool> ConfirmPaymentSuccessAsync(string bookingCode, string transactionNo, string bankCode, decimal amount);
    Task<int> ExpirePendingBookingsAsync();
    Task<bool> CancelBookingAsync(Guid bookingId, Guid guestId, string reason);
    Task<List<Booking>> GetGuestBookingsAsync(Guid guestId);
    Task<Booking?> GetBookingDetailsAsync(Guid bookingId, Guid? userId = null, bool isAdmin = false);
    Task<Booking?> GetBookingByCodeAsync(string bookingCode);
    Task<List<Booking>> GetHostBookingsAsync(Guid hostId);
    Task<(bool Success, string Message, Booking? Booking)> CheckInAttendanceAsync(string ticketOrBookingCode, Guid hostId);
}

public interface IPaymentService
{
    Task<string> GenerateVNPayUrlAsync(Guid bookingId, string returnUrl, string clientIp);
    Task<(bool Success, string Message, string? BookingCode)> ProcessVNPayReturnAsync(IDictionary<string, string> queryParams);
}

public interface IFinancialService
{
    Task<int> ProcessEligiblePayoutsAsync();
    Task<HostFinancialSummaryDto> GetHostFinancialSummaryAsync(Guid hostId);
    Task<AdminFinancialSummaryDto> GetAdminFinancialSummaryAsync();
    Task<bool> RequestPayoutWithdrawalAsync(Guid hostId, decimal amount, string? note);
}

public interface ITripService
{
    Task<List<Trip>> GetFeaturedTripsAsync(int count = 4);
    Task<List<Trip>> GetAllTripsAsync();
    Task<Trip?> GetTripDetailsBySlugAsync(string slug);
    Task<Trip?> GetTripDetailsByIdAsync(Guid id);
}

public interface ICmsService
{
    Task<ContentPage?> GetPageContentAsync(string pageKey);
    Task<List<ContentSection>> GetSectionsAsync(string pageKey);
    Task<bool> UpdateSectionAsync(Guid sectionId, string title, string? subtitle, string? contentHtml, string? ctaText, string? ctaUrl, string? bgUrl);
    Task<List<MediaAsset>> GetAllMediaAssetsAsync();
    Task<MediaAsset> SaveMediaAssetAsync(string fileName, string originalName, string url, long fileSize, string contentType, string? caption = null);
}

public interface IReviewService
{
    Task<bool> CanUserReviewWorkshopAsync(Guid workshopId, Guid guestId);
    Task<(bool Success, string Message)> CreateReviewAsync(Guid workshopId, Guid guestId, Guid bookingId, int rating, string comment);
    Task<bool> HostReplyReviewAsync(Guid reviewId, Guid hostId, string reply);
}

public interface IAdminService
{
    Task<AdminDashboardStatsDto> GetDashboardStatsAsync();
    Task<List<Workshop>> GetPendingWorkshopsAsync();
    Task<bool> ApproveWorkshopAsync(Guid workshopId, Guid adminId);
    Task<bool> RejectWorkshopAsync(Guid workshopId, Guid adminId, string reason);
    Task<List<WorkshopRevision>> GetPendingRevisionsAsync();
    Task<bool> ApproveRevisionAsync(Guid revisionId, Guid adminId);
    Task<bool> RejectRevisionAsync(Guid revisionId, Guid adminId, string reason);
    Task<List<ApplicationUser>> GetAllUsersAsync();
    Task<bool> ToggleUserStatusAsync(Guid userId);
    Task<List<HostProfile>> GetPendingHostVerificationsAsync();
    Task<bool> VerifyHostAsync(Guid hostProfileId, bool isApproved, string? note);
    Task<List<SystemSetting>> GetSystemSettingsAsync();
    Task<bool> UpdateSystemSettingAsync(string key, string value);
}

public interface INotificationService
{
    Task SendNotificationAsync(Guid userId, string title, string message, string? actionUrl = null, string type = "General");
    Task<List<Notification>> GetUserNotificationsAsync(Guid userId, int limit = 10);
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId);
}

public interface IEmailService
{
    Task<bool> SendBookingConfirmationEmailAsync(Booking booking);
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
}
