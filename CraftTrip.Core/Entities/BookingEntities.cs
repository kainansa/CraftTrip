using CraftTrip.Core.Enums;

namespace CraftTrip.Core.Entities;

public class Booking : BaseEntity
{
    public string BookingCode { get; set; } = string.Empty;

    public Guid GuestId { get; set; }
    public virtual ApplicationUser? Guest { get; set; }

    public Guid WorkshopId { get; set; }
    public virtual Workshop? Workshop { get; set; }

    public Guid SlotId { get; set; }
    public virtual Slot? Slot { get; set; }

    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal TotalAmount { get; set; }

    // Commission & Financial allocation (BR_CM_01)
    public decimal CommissionRate { get; set; } = 0.12m;
    public decimal PlatformFee { get; set; } = 0m;
    public decimal HostEarnings { get; set; } = 0m;

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string? SpecialNotes { get; set; }

    // Expiration after 15 minutes (Part 2.1 & 9)
    public DateTime ExpiresAt { get; set; }
    public bool IsCapacityRestored { get; set; } = false;

    // Payment reference
    public string? PaymentTxnId { get; set; }

    // Attendance & QR Check-in (Part 12)
    public AttendanceStatus AttendanceStatus { get; set; } = AttendanceStatus.NotCheckedIn;
    public DateTime? CheckedInAt { get; set; }
    public Guid? CheckedInByHostId { get; set; }

    // Cancellation (BR_CN_03)
    public string? CancellationReason { get; set; }
    public DateTime? CanceledAt { get; set; }

    // Navigation
    public virtual BookingTicket? Ticket { get; set; }
    public virtual ICollection<BookingStatusHistory> StatusHistories { get; set; } = new List<BookingStatusHistory>();
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    public virtual Review? Review { get; set; }
}

public class BookingTicket : BaseEntity
{
    public Guid BookingId { get; set; }
    public virtual Booking? Booking { get; set; }

    public string TicketCode { get; set; } = string.Empty; // Unique secure token
    public string QrCodeData { get; set; } = string.Empty; // Base64 data URI for QR image
    public bool IsRedeemed { get; set; } = false;
    public DateTime? RedeemedAt { get; set; }
}

public class BookingStatusHistory : BaseEntity
{
    public Guid BookingId { get; set; }
    public virtual Booking? Booking { get; set; }

    public BookingStatus? PreviousStatus { get; set; }
    public BookingStatus NewStatus { get; set; }
    public string? Note { get; set; }
}

public class Favorite : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    public Guid WorkshopId { get; set; }
    public virtual Workshop? Workshop { get; set; }
}

public class Review : BaseEntity
{
    public Guid WorkshopId { get; set; }
    public virtual Workshop? Workshop { get; set; }

    public Guid GuestId { get; set; }
    public virtual ApplicationUser? Guest { get; set; }

    public Guid BookingId { get; set; } // Enforce 1 review per completed booking (BR_RV_04)
    public virtual Booking? Booking { get; set; }

    public int Rating { get; set; } // 1 to 5 stars
    public string Comment { get; set; } = string.Empty;

    public string? HostReply { get; set; }
    public DateTime? RepliedAt { get; set; }
}
