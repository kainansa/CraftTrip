using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;

namespace CraftTrip.Web.ViewModels;

public class BookingCheckoutViewModel
{
    public Guid SlotId { get; set; }

    [ValidateNever]
    public Workshop? Workshop { get; set; }

    [ValidateNever]
    public Slot? Slot { get; set; }

    [Range(1, 50, ErrorMessage = "Số lượng khách phải từ 1 đến 50 người.")]
    public int Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Vui lòng nhập họ và tên của bạn.")]
    [StringLength(100)]
    public string CustomerName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ email.")]
    [EmailAddress(ErrorMessage = "Địa chỉ email không hợp lệ.")]
    public string CustomerEmail { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ.")]
    public string CustomerPhone { get; set; } = string.Empty;

    [StringLength(500)]
    public string? SpecialNotes { get; set; }

    public decimal EstimatedTotal => Workshop != null ? Workshop.BasePrice * Quantity : 0m;
}

public class GuestTicketViewModel
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string TicketCode { get; set; } = string.Empty;
    public string WorkshopTitle { get; set; } = string.Empty;
    public string? WorkshopCoverImage { get; set; }
    public string VillageName { get; set; } = string.Empty;
    public string WorkshopAddress { get; set; } = string.Empty;
    public DateTime SessionDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public BookingStatus BookingStatus { get; set; }
    public AttendanceStatus AttendanceStatus { get; set; }
    public string? QrCodeData { get; set; }
    public bool CanCancel { get; set; }
}

public class GuestTicketListViewModel
{
    public List<GuestTicketViewModel> Tickets { get; set; } = new();
    public int ActiveTicketsCount => Tickets.Count(t => t.BookingStatus == BookingStatus.Confirmed && t.AttendanceStatus == AttendanceStatus.NotCheckedIn);
    public int UsedTicketsCount => Tickets.Count(t => t.AttendanceStatus == AttendanceStatus.CheckedIn || t.BookingStatus == BookingStatus.Completed);
}

public class BookingConfirmationViewModel
{
    public Booking Booking { get; set; } = default!;
    public bool PaymentSuccess { get; set; }
    public string? Message { get; set; }
}

public class BookingCancelViewModel
{
    public Guid BookingId { get; set; }
    public string BookingCode { get; set; } = string.Empty;
    public string WorkshopTitle { get; set; } = string.Empty;
    public DateTime SessionStartTime { get; set; }
    public decimal TotalAmount { get; set; }
    public bool IsEligibleForFullRefund { get; set; }

    [Required(ErrorMessage = "Vui lòng cho biết lý do bạn muốn hủy vé.")]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class ReviewSubmitViewModel
{
    public Guid WorkshopId { get; set; }
    public Guid BookingId { get; set; }
    public string WorkshopTitle { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Vui lòng chọn số sao từ 1 đến 5.")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "Vui lòng viết đôi dòng cảm nhận về buổi trải nghiệm.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Nội dung đánh giá cần từ 10 đến 1000 ký tự.")]
    public string Comment { get; set; } = string.Empty;
}
