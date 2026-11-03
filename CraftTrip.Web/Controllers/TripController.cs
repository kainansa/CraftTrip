using Microsoft.AspNetCore.Mvc;
using CraftTrip.Services.Interfaces;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Controllers;

public class TripController : Controller
{
    private readonly ITripService _tripService;

    public TripController(ITripService tripService)
    {
        _tripService = tripService;
    }

    public async Task<IActionResult> Index()
    {
        var trips = await _tripService.GetAllTripsAsync();
        return View(trips);
    }

    public async Task<IActionResult> Details(string slug)
    {
        if (string.IsNullOrEmpty(slug)) return NotFound();

        var trip = await _tripService.GetTripDetailsBySlugAsync(slug);
        if (trip == null) return NotFound("Không tìm thấy lộ trình chuyến đi.");

        var viewModel = new TripDetailViewModel
        {
            Trip = trip,
            LinkedWorkshop = trip.Workshop
        };

        return View(viewModel);
    }
}
