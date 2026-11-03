using Microsoft.AspNetCore.Identity;
using CraftTrip.Core.Enums;

namespace CraftTrip.Core.Entities;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FullName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public UserRole UserRole { get; set; } = UserRole.Guest;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public virtual HostProfile? HostProfile { get; set; }
    public virtual GuestProfile? GuestProfile { get; set; }
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}

public class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole() : base() { }
    public ApplicationRole(string roleName) : base(roleName) { }
}

public class HostProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    public string StudioName { get; set; } = string.Empty;
    public string CraftType { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public Guid? VillageId { get; set; }
    public virtual Village? Village { get; set; }
    public string Address { get; set; } = string.Empty;

    public string? IdCardNumber { get; set; }
    public string? IdCardFrontUrl { get; set; }
    public string? IdCardBackUrl { get; set; }
    public HostVerificationStatus VerificationStatus { get; set; } = HostVerificationStatus.Pending;
    public string? VerificationNote { get; set; }

    public string? BankAccountName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankName { get; set; }

    public decimal WalletBalance { get; set; } = 0m;
    public decimal PendingBalance { get; set; } = 0m;
    public decimal TotalEarned { get; set; } = 0m;

    public virtual ICollection<HostWalletTransaction> WalletTransactions { get; set; } = new List<HostWalletTransaction>();
    public virtual ICollection<HostPayout> Payouts { get; set; } = new List<HostPayout>();
}

public class GuestProfile : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    public string? Preferences { get; set; }
    public string? City { get; set; }
}
