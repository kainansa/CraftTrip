namespace CraftTrip.Core.Entities;

public class Trip : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;

    public Guid? VillageId { get; set; }
    public virtual Village? Village { get; set; }

    public Guid? WorkshopId { get; set; }
    public virtual Workshop? Workshop { get; set; }

    public string DurationDescription { get; set; } = "1 ngày";
    public decimal EstimatedCost { get; set; }
    public string CoverImageUrl { get; set; } = string.Empty;
    public bool IsFeatured { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;

    // Navigation
    public virtual ICollection<TripItinerary> Itineraries { get; set; } = new List<TripItinerary>();
    public virtual ICollection<TripImage> Images { get; set; } = new List<TripImage>();
}

public class TripItinerary : BaseEntity
{
    public Guid TripId { get; set; }
    public virtual Trip? Trip { get; set; }

    public int StopOrder { get; set; }
    public string TimeSlot { get; set; } = string.Empty; // e.g. "08:30 - 10:00"
    public string Title { get; set; } = string.Empty; // e.g. "Ăn sáng bánh cuốn làng cổ & ngắm cảnh sông Đáy"
    public string LocationName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconClass { get; set; } = "bi-geo-alt";
    public string? ImageUrl { get; set; }
}

public class TripImage : BaseEntity
{
    public Guid TripId { get; set; }
    public virtual Trip? Trip { get; set; }

    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; } = 0;
}
