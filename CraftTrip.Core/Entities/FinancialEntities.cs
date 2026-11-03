using CraftTrip.Core.Enums;

namespace CraftTrip.Core.Entities;

public class Payment : BaseEntity
{
    public Guid BookingId { get; set; }
    public virtual Booking? Booking { get; set; }

    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "VNPay";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string? TransactionNo { get; set; } // VNPay transaction number
    public string? BankCode { get; set; }
    public string? CardType { get; set; }
    public DateTime? PayDate { get; set; }
    public string? ResponseCode { get; set; }

    public virtual ICollection<PaymentTransaction> Transactions { get; set; } = new List<PaymentTransaction>();
}

public class PaymentTransaction : BaseEntity
{
    public Guid PaymentId { get; set; }
    public virtual Payment? Payment { get; set; }

    public string TransactionType { get; set; } = "Payment"; // e.g. "Create", "Callback", "IPN", "Refund"
    public decimal Amount { get; set; }
    public string? RawDataJson { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

public class Refund : BaseEntity
{
    public Guid BookingId { get; set; }
    public virtual Booking? Booking { get; set; }

    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public RefundStatus Status { get; set; } = RefundStatus.Requested;
    public DateTime? ProcessedAt { get; set; }
    public string? RefundTransactionRef { get; set; }
}

public class HostPayout : BaseEntity
{
    public Guid HostId { get; set; }
    public virtual ApplicationUser? Host { get; set; }

    public Guid BookingId { get; set; }
    public virtual Booking? Booking { get; set; }

    public decimal Amount { get; set; } // Host earnings from booking
    public DateTime EligibleAt { get; set; } // EndTime + 24 hours (BR_PO_02)
    public PayoutStatus Status { get; set; } = PayoutStatus.PendingEligibility;
    public DateTime? PaidAt { get; set; }
    public string? PayoutReference { get; set; }
}

public class HostWalletTransaction : BaseEntity
{
    public Guid HostId { get; set; }
    public virtual ApplicationUser? Host { get; set; }

    public decimal Amount { get; set; }
    public WalletTxType TransactionType { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? ReferenceId { get; set; }
}

public class CommissionRule : BaseEntity
{
    public string Name { get; set; } = "Mặc định hệ thống";
    public decimal SmallGroupRate { get; set; } = 0.12m; // 12% (BR_CM_01)
    public decimal LargeGroupRate { get; set; } = 0.15m; // 15% (BR_CM_01)
    public int GroupSizeThreshold { get; set; } = 5; // e.g. >= 5 is large group
    public bool IsActive { get; set; } = true;
}
