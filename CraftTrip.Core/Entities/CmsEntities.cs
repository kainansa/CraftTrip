namespace CraftTrip.Core.Entities;

public class ContentPage : BaseEntity
{
    public string PageKey { get; set; } = string.Empty; // "home", "about", "contact", "terms", "policy", "faq"
    public string Title { get; set; } = string.Empty;
    public string? MetaDescription { get; set; }
    public bool IsPublished { get; set; } = true;

    public virtual ICollection<ContentSection> Sections { get; set; } = new List<ContentSection>();
}

public class ContentSection : BaseEntity
{
    public Guid PageId { get; set; }
    public virtual ContentPage? Page { get; set; }

    public string SectionKey { get; set; } = string.Empty; // "hero", "categories", "villages", "featured_workshops", "artisan_story", "how_it_works", "become_host", "footer"
    public string Title { get; set; } = string.Empty;
    public string? Subtitle { get; set; }
    public string? ContentHtml { get; set; }
    public string? PrimaryCtaText { get; set; }
    public string? PrimaryCtaUrl { get; set; }
    public string? SecondaryCtaText { get; set; }
    public string? SecondaryCtaUrl { get; set; }
    public string? BackgroundImageUrl { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class MediaAsset : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string OriginalName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = "image/jpeg";
    public string? AltText { get; set; }
    public string? Caption { get; set; }
    public string Folder { get; set; } = "general";
}

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }
    public virtual ApplicationUser? User { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? ActionUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public string Type { get; set; } = "General"; // Booking, Payment, Moderation, Payout, System
}

public class AuditLog : BaseEntity
{
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
}

public class SystemSetting : BaseEntity
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = "General";
}
