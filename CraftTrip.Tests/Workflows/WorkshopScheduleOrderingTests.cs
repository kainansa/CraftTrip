using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Implementations;
using Xunit;

namespace CraftTrip.Tests.Workflows;

public class WorkshopScheduleOrderingTests
{
    private (CraftTripDbContext Context, SqliteConnection Connection) CreateSqliteInMemoryContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<CraftTripDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new CraftTripDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }

    [Fact]
    public async Task GetWorkshopDetailsById_OnSqlite_LoadsWithoutTimeSpanOrderingException_And_MaintainsChronologicalOrder()
    {
        // Arrange
        var (context, connection) = CreateSqliteInMemoryContext();
        try
        {
            var category = new CraftCategory { Id = Guid.NewGuid(), Name = "Gốm sứ", Slug = "gom-su" };
            context.CraftCategories.Add(category);

            var host = new ApplicationUser { Id = Guid.NewGuid(), UserName = "artisan@test.com" };
            context.Users.Add(host);

            var workshop = new Workshop
            {
                Id = Guid.NewGuid(),
                HostId = host.Id,
                CategoryId = category.Id,
                Title = "Trải nghiệm gốm Bát Tràng",
                Slug = "trai-nghiem-gom-bat-trang",
                Status = WorkshopStatus.Published,
                BasePrice = 250000m
            };
            context.Workshops.Add(workshop);

            var tomorrow = DateTime.UtcNow.Date.AddDays(1);
            var schedule = new WorkshopSchedule
            {
                Id = Guid.NewGuid(),
                WorkshopId = workshop.Id,
                Date = tomorrow,
                IsActive = true
            };
            context.WorkshopSchedules.Add(schedule);

            // Add slots in mixed order: 13:30, 08:30, 18:00, 09:00
            var slot1330 = new Slot { Id = Guid.NewGuid(), ScheduleId = schedule.Id, StartTime = new TimeSpan(13, 30, 0), EndTime = new TimeSpan(15, 30, 0), MaxCapacity = 10, AvailableCapacity = 10 };
            var slot0830 = new Slot { Id = Guid.NewGuid(), ScheduleId = schedule.Id, StartTime = new TimeSpan(8, 30, 0), EndTime = new TimeSpan(10, 30, 0), MaxCapacity = 10, AvailableCapacity = 10 };
            var slot1800 = new Slot { Id = Guid.NewGuid(), ScheduleId = schedule.Id, StartTime = new TimeSpan(18, 0, 0), EndTime = new TimeSpan(20, 0, 0), MaxCapacity = 10, AvailableCapacity = 10 };
            var slot0900 = new Slot { Id = Guid.NewGuid(), ScheduleId = schedule.Id, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(11, 0, 0), MaxCapacity = 10, AvailableCapacity = 10 };

            context.Slots.AddRange(slot1330, slot0830, slot1800, slot0900);
            await context.SaveChangesAsync();

            var workshopService = new WorkshopService(context);

            // Act: fetch workshop details using EF Core on SQLite
            var result = await workshopService.GetWorkshopDetailsByIdAsync(workshop.Id);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.Schedules);

            var retrievedSchedule = result.Schedules.First();
            var slots = retrievedSchedule.Slots.ToList();

            Assert.Equal(4, slots.Count);

            // TEST C Requirement: Verify exact chronological order: 08:30 -> 09:00 -> 13:30 -> 18:00
            Assert.Equal(new TimeSpan(8, 30, 0), slots[0].StartTime);
            Assert.Equal(new TimeSpan(9, 0, 0), slots[1].StartTime);
            Assert.Equal(new TimeSpan(13, 30, 0), slots[2].StartTime);
            Assert.Equal(new TimeSpan(18, 0, 0), slots[3].StartTime);

            // Verify StartTimeMinutes helper values
            Assert.Equal(510, slots[0].StartTimeMinutes);
            Assert.Equal(540, slots[1].StartTimeMinutes);
            Assert.Equal(810, slots[2].StartTimeMinutes);
            Assert.Equal(1080, slots[3].StartTimeMinutes);
        }
        finally
        {
            connection.Close();
        }
    }

    [Fact]
    public async Task GetWorkshopDetailsBySlug_OnSqlite_OrdersMultipleDatesAndTimesChronologically()
    {
        // Arrange
        var (context, connection) = CreateSqliteInMemoryContext();
        try
        {
            var category = new CraftCategory { Id = Guid.NewGuid(), Name = "Dệt lụa", Slug = "det-lua" };
            context.CraftCategories.Add(category);

            var host = new ApplicationUser { Id = Guid.NewGuid(), UserName = "host2@test.com" };
            context.Users.Add(host);

            var workshop = new Workshop
            {
                Id = Guid.NewGuid(),
                HostId = host.Id,
                CategoryId = category.Id,
                Title = "Dệt lụa Vạn Phúc",
                Slug = "det-lua-van-phuc",
                Status = WorkshopStatus.Published,
                BasePrice = 300000m
            };
            context.Workshops.Add(workshop);

            var day1 = DateTime.UtcNow.Date.AddDays(2);
            var day2 = DateTime.UtcNow.Date.AddDays(1); // Earlier date added second to test date ordering

            var schDay1 = new WorkshopSchedule { Id = Guid.NewGuid(), WorkshopId = workshop.Id, Date = day1, IsActive = true };
            var schDay2 = new WorkshopSchedule { Id = Guid.NewGuid(), WorkshopId = workshop.Id, Date = day2, IsActive = true };
            context.WorkshopSchedules.AddRange(schDay1, schDay2);

            // Day 2 slots (earlier date)
            var slotD2_Afternoon = new Slot { Id = Guid.NewGuid(), ScheduleId = schDay2.Id, StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(16, 0, 0), MaxCapacity = 8, AvailableCapacity = 8 };
            var slotD2_Morning = new Slot { Id = Guid.NewGuid(), ScheduleId = schDay2.Id, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(11, 0, 0), MaxCapacity = 8, AvailableCapacity = 8 };

            // Day 1 slots (later date)
            var slotD1_Morning = new Slot { Id = Guid.NewGuid(), ScheduleId = schDay1.Id, StartTime = new TimeSpan(8, 30, 0), EndTime = new TimeSpan(10, 30, 0), MaxCapacity = 8, AvailableCapacity = 8 };

            context.Slots.AddRange(slotD2_Afternoon, slotD2_Morning, slotD1_Morning);
            await context.SaveChangesAsync();

            var workshopService = new WorkshopService(context);

            // Act
            var result = await workshopService.GetWorkshopDetailsBySlugAsync("det-lua-van-phuc");

            // Assert
            Assert.NotNull(result);
            var schedules = result.Schedules.ToList();
            Assert.Equal(2, schedules.Count);

            // Dates ordered first: day2 (AddDays 1) before day1 (AddDays 2)
            Assert.Equal(day2, schedules[0].Date);
            Assert.Equal(day1, schedules[1].Date);

            // Day 2 slots ordered chronologically: 09:00 -> 14:00
            var day2Slots = schedules[0].Slots.ToList();
            Assert.Equal(2, day2Slots.Count);
            Assert.Equal(new TimeSpan(9, 0, 0), day2Slots[0].StartTime);
            Assert.Equal(new TimeSpan(14, 0, 0), day2Slots[1].StartTime);
        }
        finally
        {
            connection.Close();
        }
    }

    [Fact]
    public async Task GetWorkshopDetails_BoundaryCases_HandlesEmptySchedulesAndZeroCapacitySafely()
    {
        // Arrange
        var (context, connection) = CreateSqliteInMemoryContext();
        try
        {
            var category = new CraftCategory { Id = Guid.NewGuid(), Name = "Mây tre đan", Slug = "may-tre-dan" };
            context.CraftCategories.Add(category);

            var host = new ApplicationUser { Id = Guid.NewGuid(), UserName = "host3@test.com" };
            context.Users.Add(host);

            // Workshop with NO schedules
            var emptyWorkshop = new Workshop
            {
                Id = Guid.NewGuid(),
                HostId = host.Id,
                CategoryId = category.Id,
                Title = "Workshop Chưa Có Lịch",
                Slug = "workshop-chua-co-lich",
                Status = WorkshopStatus.Published,
                BasePrice = 150000m
            };
            context.Workshops.Add(emptyWorkshop);
            await context.SaveChangesAsync();

            var workshopService = new WorkshopService(context);

            // Act & Assert 1: Empty workshop does not throw
            var emptyResult = await workshopService.GetWorkshopDetailsByIdAsync(emptyWorkshop.Id);
            Assert.NotNull(emptyResult);
            Assert.Empty(emptyResult.Schedules);

            // Workshop with zero remaining capacity
            var fullWorkshop = new Workshop
            {
                Id = Guid.NewGuid(),
                HostId = host.Id,
                CategoryId = category.Id,
                Title = "Workshop Hết Chỗ",
                Slug = "workshop-het-cho",
                Status = WorkshopStatus.Published,
                BasePrice = 200000m
            };
            var fullSch = new WorkshopSchedule { Id = Guid.NewGuid(), WorkshopId = fullWorkshop.Id, Date = DateTime.UtcNow.Date.AddDays(1) };
            var fullSlot = new Slot { Id = Guid.NewGuid(), ScheduleId = fullSch.Id, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(11, 0, 0), MaxCapacity = 5, AvailableCapacity = 0 };

            context.Workshops.Add(fullWorkshop);
            context.WorkshopSchedules.Add(fullSch);
            context.Slots.Add(fullSlot);
            await context.SaveChangesAsync();

            // Act & Assert 2: Full workshop loads safely
            var fullResult = await workshopService.GetWorkshopDetailsByIdAsync(fullWorkshop.Id);
            Assert.NotNull(fullResult);
            Assert.Single(fullResult.Schedules);
            Assert.Equal(0, fullResult.Schedules.First().Slots.First().AvailableCapacity);
        }
        finally
        {
            connection.Close();
        }
    }
}
