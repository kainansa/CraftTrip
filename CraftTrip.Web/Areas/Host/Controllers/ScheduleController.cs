using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CraftTrip.Core.Entities;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Web.ViewModels;

namespace CraftTrip.Web.Areas.Host.Controllers;

[Area("Host")]
[Authorize(Roles = "Host,Admin")]
public class ScheduleController : Controller
{
    private readonly CraftTripDbContext _context;

    public ScheduleController(CraftTripDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Manage(Guid workshopId)
    {
        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var workshop = await _context.Workshops
            .Include(w => w.Schedules.Where(s => s.Date >= DateTime.UtcNow.Date.AddDays(-1)).OrderBy(s => s.Date))
                .ThenInclude(s => s.Slots)
                    .ThenInclude(sl => sl.Bookings)
            .FirstOrDefaultAsync(w => w.Id == workshopId && w.HostId == hostId && !w.IsDeleted);

        if (workshop == null)
        {
            return NotFound("Không tìm thấy Workshop hoặc bạn không có quyền.");
        }

        foreach (var sch in workshop.Schedules)
        {
            sch.Slots = sch.Slots.OrderBy(sl => sl.StartTime).ThenBy(sl => sl.EndTime).ToList();
        }

        var viewModel = new HostScheduleManageViewModel
        {
            WorkshopId = workshop.Id,
            WorkshopTitle = workshop.Title,
            SelectedDate = DateTime.UtcNow.Date.AddDays(1),
            MaxCapacity = workshop.MaxParticipants > 0 ? workshop.MaxParticipants : 10,
            ExistingSchedules = workshop.Schedules.OrderBy(s => s.Date).ToList()
        };

        return View(viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddSlot(HostScheduleManageViewModel model)
    {
        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var workshop = await _context.Workshops
            .FirstOrDefaultAsync(w => w.Id == model.WorkshopId && w.HostId == hostId && !w.IsDeleted);

        if (workshop == null) return NotFound();

        var date = model.SelectedDate.Date;
        var schedule = await _context.WorkshopSchedules
            .Include(s => s.Slots)
            .FirstOrDefaultAsync(s => s.WorkshopId == model.WorkshopId && s.Date.Date == date);

        if (schedule == null)
        {
            schedule = new WorkshopSchedule
            {
                WorkshopId = model.WorkshopId,
                Date = date,
                IsActive = true
            };
            _context.WorkshopSchedules.Add(schedule);
        }

        schedule.Slots.Add(new Slot
        {
            Schedule = schedule,
            StartTime = model.StartTime,
            EndTime = model.EndTime,
            MaxCapacity = model.MaxCapacity,
            AvailableCapacity = model.MaxCapacity
        });

        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = $"Đã mở thêm ca {model.StartTime:hh\\:mm} - {model.EndTime:hh\\:mm} ngày {date:dd/MM/yyyy}!";

        return RedirectToAction("Manage", new { workshopId = model.WorkshopId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteSlot(Guid slotId, Guid workshopId)
    {
        var hostId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var slot = await _context.Slots
            .Include(s => s.Schedule)
                .ThenInclude(sch => sch!.Workshop)
            .Include(s => s.Bookings)
            .FirstOrDefaultAsync(s => s.Id == slotId);

        if (slot == null || slot.Schedule?.Workshop?.HostId != hostId)
        {
            return NotFound();
        }

        // UC_H03 alternative sequence A1: Cannot delete slot if there are already paid/confirmed bookings
        if (slot.Bookings.Any(b => b.Status == Core.Enums.BookingStatus.Confirmed || b.Status == Core.Enums.BookingStatus.Paid))
        {
            TempData["ErrorMessage"] = "Bạn không thể xóa ca này vì đã có khách đặt vé trước. Vui lòng liên hệ Admin để xử lý.";
            return RedirectToAction("Manage", new { workshopId });
        }

        _context.Slots.Remove(slot);
        await _context.SaveChangesAsync();
        TempData["SuccessMessage"] = "Đã xóa ca thành công.";

        return RedirectToAction("Manage", new { workshopId });
    }
}
