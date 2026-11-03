using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Services.Implementations;

public class TripService : ITripService
{
    private readonly CraftTripDbContext _context;

    public TripService(CraftTripDbContext context)
    {
        _context = context;
    }

    public async Task<List<Trip>> GetFeaturedTripsAsync(int count = 4)
    {
        return await _context.Trips
            .AsNoTracking()
            .Include(t => t.Village)
            .Include(t => t.Workshop)
            .Include(t => t.Itineraries.OrderBy(i => i.StopOrder))
            .Where(t => t.IsFeatured && !t.IsDeleted)
            .OrderBy(t => t.DisplayOrder)
            .Take(count)
            .ToListAsync();
    }

    public async Task<List<Trip>> GetAllTripsAsync()
    {
        return await _context.Trips
            .AsNoTracking()
            .Include(t => t.Village)
            .Include(t => t.Workshop)
            .Include(t => t.Itineraries.OrderBy(i => i.StopOrder))
            .Where(t => !t.IsDeleted)
            .OrderBy(t => t.DisplayOrder)
            .ToListAsync();
    }

    public async Task<Trip?> GetTripDetailsBySlugAsync(string slug)
    {
        var trip = await _context.Trips
            .AsNoTracking()
            .Include(t => t.Village)
            .Include(t => t.Workshop)
                .ThenInclude(w => w!.Host)
                    .ThenInclude(h => h!.HostProfile)
            .Include(t => t.Workshop)
                .ThenInclude(w => w!.Schedules.Where(s => s.Date >= DateTime.UtcNow.Date).OrderBy(s => s.Date))
                    .ThenInclude(s => s.Slots)
            .Include(t => t.Itineraries.OrderBy(i => i.StopOrder))
            .Include(t => t.Images.OrderBy(i => i.DisplayOrder))
            .FirstOrDefaultAsync(t => t.Slug == slug && !t.IsDeleted);

        if (trip?.Workshop != null)
        {
            trip.Workshop.Schedules = trip.Workshop.Schedules
                .OrderBy(s => s.Date)
                .Select(s =>
                {
                    s.Slots = s.Slots.OrderBy(sl => sl.StartTime).ThenBy(sl => sl.EndTime).ToList();
                    return s;
                })
                .ToList();
        }

        return trip;
    }

    public async Task<Trip?> GetTripDetailsByIdAsync(Guid id)
    {
        return await _context.Trips
            .AsNoTracking()
            .Include(t => t.Village)
            .Include(t => t.Workshop)
            .Include(t => t.Itineraries.OrderBy(i => i.StopOrder))
            .Include(t => t.Images.OrderBy(i => i.DisplayOrder))
            .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
    }
}
