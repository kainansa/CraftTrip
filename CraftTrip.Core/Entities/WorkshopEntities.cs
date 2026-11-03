using System.ComponentModel.DataAnnotations;
using CraftTrip.Core.Enums;

namespace CraftTrip.Core.Entities;

public class Workshop : BaseEntity
{
    public Guid HostId { get; set; }
    public virtual ApplicationUser? Host { get; set; }

    public Guid CategoryId { get; set; }
    public virtual CraftCategory? Category { get; set; }

    public Guid? VillageId { get; set; }
    public virtual Village? Village { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;

    // Rich structured experience info (URD Step 3)
    public string ExperienceSteps { get; set; } = string.Empty; // Khách sẽ làm gì, các công đoạn
    public string TakeHomeProduct { get; set; } = string.Empty; // Sản phẩm tự tay làm mang về
    public string TargetAudience { get; set; } = string.Empty; // Phù hợp với ai
    public string PreparationGuide { get; set; } = string.Empty; // Những điều cần chuẩn bị
    public string IncludedServices { get; set; } = string.Empty; // Dịch vụ bao gồm

    public decimal BasePrice { get; set; }
    public int DurationMinutes { get; set; } = 120;
    public int MinParticipants { get; set; } = 1;
    public int MaxParticipants { get; set; } = 20;

    public string LocationAddress { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;

    public WorkshopStatus Status { get; set; } = WorkshopStatus.Draft;
    public string? RejectionReason { get; set; }
    public bool IsFeatured { get; set; } = false;

    public double AverageRating { get; set; } = 0;
    public int ReviewCount { get; set; } = 0;

    // Navigation
    public virtual ICollection<WorkshopImage> Images { get; set; } = new List<WorkshopImage>();
    public virtual ICollection<WorkshopSchedule> Schedules { get; set; } = new List<WorkshopSchedule>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public virtual ICollection<WorkshopRevision> Revisions { get; set; } = new List<WorkshopRevision>();
    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

public class WorkshopImage : BaseEntity
{
    public Guid WorkshopId { get; set; }
    public virtual Workshop? Workshop { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public bool IsCover { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
}

public class WorkshopSchedule : BaseEntity
{
    public Guid WorkshopId { get; set; }
    public virtual Workshop? Workshop { get; set; }

    public DateTime Date { get; set; }
    public bool IsActive { get; set; } = true;

    public virtual ICollection<Slot> Slots { get; set; } = new List<Slot>();
}

public class Slot : BaseEntity
{
    public Guid ScheduleId { get; set; }
    public virtual WorkshopSchedule? Schedule { get; set; }

    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public int MaxCapacity { get; set; }
    public int AvailableCapacity { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int StartTimeMinutes => (int)StartTime.TotalMinutes;

    [System.ComponentModel.DataAnnotations.Schema.NotMapped]
    public int EndTimeMinutes => (int)EndTime.TotalMinutes;

    public byte[]? RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}

public class WorkshopRevision : BaseEntity
{
    public Guid WorkshopId { get; set; }
    public virtual Workshop? Workshop { get; set; }

    public string ProposedTitle { get; set; } = string.Empty;
    public string ProposedShortDescription { get; set; } = string.Empty;
    public string ProposedFullDescription { get; set; } = string.Empty;
    public decimal ProposedBasePrice { get; set; }
    public int ProposedDurationMinutes { get; set; }
    public string ProposedExperienceSteps { get; set; } = string.Empty;
    public string ProposedIncludedServices { get; set; } = string.Empty;
    public string ProposedPreparationGuide { get; set; } = string.Empty;
    public string? ProposedCoverImageUrl { get; set; }

    public RevisionStatus Status { get; set; } = RevisionStatus.Pending;
    public string? AdminFeedback { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
}
