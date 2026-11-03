namespace CraftTrip.Core.Enums;

public enum UserRole
{
    Guest = 1,
    Host = 2,
    Admin = 3
}

public enum HostVerificationStatus
{
    Pending = 1,
    Verified = 2,
    Rejected = 3
}

public enum WorkshopStatus
{
    Draft = 1,
    Pending = 2,
    Published = 3,
    Rejected = 4
}

public enum RevisionStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3
}

public enum BookingStatus
{
    Pending = 1,
    Paid = 2,
    Confirmed = 3,
    Canceled = 4,
    Completed = 5
}

public enum AttendanceStatus
{
    NotCheckedIn = 1,
    CheckedIn = 2,
    Absent = 3
}

public enum PaymentStatus
{
    Pending = 1,
    Success = 2,
    Failed = 3,
    Refunded = 4
}

public enum RefundStatus
{
    Requested = 1,
    Processing = 2,
    Completed = 3,
    Rejected = 4
}

public enum PayoutStatus
{
    PendingEligibility = 1,
    Eligible = 2,
    Transferred = 3,
    Withdrawn = 4
}

public enum WalletTxType
{
    Credit = 1,
    Payout = 2,
    Adjustment = 3
}
