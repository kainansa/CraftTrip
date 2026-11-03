using CraftTrip.Core.Entities;

namespace CraftTrip.Web.ViewModels;

public class HomeViewModel
{
    public ContentSection? HeroSection { get; set; }
    public ContentSection? HowItWorksSection { get; set; }
    public ContentSection? BecomeHostSection { get; set; }
    public List<CraftCategory> FeaturedCategories { get; set; } = new();
    public List<Village> FeaturedVillages { get; set; } = new();
    public List<Workshop> FeaturedWorkshops { get; set; } = new();
    public List<ArtisanProfile> FeaturedArtisans { get; set; } = new();
    public List<Trip> FeaturedTrips { get; set; } = new();
}

public class WorkshopListViewModel
{
    public List<Workshop> Workshops { get; set; } = new();
    public List<CraftCategory> Categories { get; set; } = new();
    public List<Village> Villages { get; set; } = new();

    public string? Keyword { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? VillageId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public int? MaxDuration { get; set; }
    public DateTime? AvailableDate { get; set; }
    public string? SortBy { get; set; }

    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 9;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class WorkshopDetailViewModel
{
    public Workshop Workshop { get; set; } = default!;
    public bool CanReview { get; set; }
    public Guid? EligibleBookingId { get; set; }
    public List<Workshop> RelatedWorkshops { get; set; } = new();
}

public class VillageDetailViewModel
{
    public Village Village { get; set; } = default!;
    public List<Workshop> Workshops { get; set; } = new();
    public List<ArtisanProfile> Artisans { get; set; } = new();
    public List<Trip> Trips { get; set; } = new();
}

public class TripDetailViewModel
{
    public Trip Trip { get; set; } = default!;
    public Workshop? LinkedWorkshop { get; set; }
}
