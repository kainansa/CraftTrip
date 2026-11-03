namespace CraftTrip.Core.Entities;

public class Village : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string HistoryDescription { get; set; } = string.Empty;
    public string CraftProcessDescription { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty; // e.g. "Gia Lâm, Hà Nội"
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string CoverImageUrl { get; set; } = string.Empty;
    public bool IsFeatured { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;

    // Navigation
    public virtual ICollection<VillageImage> Images { get; set; } = new List<VillageImage>();
    public virtual ICollection<Workshop> Workshops { get; set; } = new List<Workshop>();
    public virtual ICollection<ArtisanProfile> Artisans { get; set; } = new List<ArtisanProfile>();
    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

public class VillageImage : BaseEntity
{
    public Guid VillageId { get; set; }
    public virtual Village? Village { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public int DisplayOrder { get; set; } = 0;
}

public class CraftCategory : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string IconClass { get; set; } = "bi-palette";
    public string ImageUrl { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int DisplayOrder { get; set; } = 0;
    public bool IsFeatured { get; set; } = true;

    // Navigation
    public virtual ICollection<Workshop> Workshops { get; set; } = new List<Workshop>();
}

public class ArtisanProfile : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty; // e.g. "Nghệ nhân Ưu tú Bát Tràng"
    public string AvatarUrl { get; set; } = string.Empty;
    public int YearsOfExperience { get; set; }
    public string Story { get; set; } = string.Empty;
    public Guid? VillageId { get; set; }
    public virtual Village? Village { get; set; }
    public Guid? HostUserId { get; set; }
    public virtual ApplicationUser? HostUser { get; set; }
    public bool IsFeatured { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
}
