using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Interfaces;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Controllers;

public class HomeController : Controller
{
    private readonly ICmsService _cmsService;
    private readonly IWorkshopService _workshopService;
    private readonly ITripService _tripService;
    private readonly CraftTripDbContext _context;

    public HomeController(
        ICmsService cmsService,
        IWorkshopService workshopService,
        ITripService tripService,
        CraftTripDbContext context)
    {
        _cmsService = cmsService;
        _workshopService = workshopService;
        _tripService = tripService;
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var hero = (await _cmsService.GetSectionsAsync("home")).FirstOrDefault(s => s.SectionKey == "hero");
        var howItWorks = (await _cmsService.GetSectionsAsync("home")).FirstOrDefault(s => s.SectionKey == "how_it_works");
        var becomeHost = (await _cmsService.GetSectionsAsync("home")).FirstOrDefault(s => s.SectionKey == "become_host");

        var featuredCategories = await _context.CraftCategories
            .AsNoTracking()
            .Where(c => c.IsFeatured && !c.IsDeleted)
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var featuredVillages = await _context.Villages
            .AsNoTracking()
            .Where(v => v.IsFeatured && !v.IsDeleted)
            .OrderBy(v => v.DisplayOrder)
            .Take(6)
            .ToListAsync();

        var featuredWorkshops = await _workshopService.GetFeaturedWorkshopsAsync(6);
        var featuredArtisans = await _context.ArtisanProfiles
            .AsNoTracking()
            .Where(a => a.IsFeatured && !a.IsDeleted)
            .OrderBy(a => a.DisplayOrder)
            .Take(4)
            .ToListAsync();

        var featuredTrips = await _tripService.GetFeaturedTripsAsync(3);

        var viewModel = new HomeViewModel
        {
            HeroSection = hero,
            HowItWorksSection = howItWorks,
            BecomeHostSection = becomeHost,
            FeaturedCategories = featuredCategories,
            FeaturedVillages = featuredVillages,
            FeaturedWorkshops = featuredWorkshops,
            FeaturedArtisans = featuredArtisans,
            FeaturedTrips = featuredTrips
        };

        return View(viewModel);
    }

    public IActionResult About() => View();
    public IActionResult Contact() => View();
    public IActionResult Terms() => View();
    public IActionResult Privacy() => View();
    public IActionResult Faq() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View();
    }
}
