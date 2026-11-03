using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;
using CraftTrip.Core.Interfaces;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.DTOs;
using CraftTrip.Services.Implementations;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Tests.BusinessRules;

public class BusinessRuleTests
{
    private CraftTripDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CraftTripDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new CraftTripDbContext(options);
    }

    [Fact]
    public async Task BR_CM_01_SmallGroup_Commission_Is_12_Percent()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        context.CommissionRules.Add(new CommissionRule
        {
            SmallGroupRate = 0.12m,
            LargeGroupRate = 0.15m,
            GroupSizeThreshold = 5,
            IsActive = true
        });

        var hostUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "host@test.com", Email = "host@test.com" };
        var guestUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "guest@test.com", Email = "guest@test.com" };
        context.Users.AddRange(hostUser, guestUser);

        var workshop = new Workshop
        {
            Id = Guid.NewGuid(),
            Title = "Gốm Bát Tràng",
            HostId = hostUser.Id,
            BasePrice = 200000m,
            Status = WorkshopStatus.Published
        };
        context.Workshops.Add(workshop);

        var schedule = new WorkshopSchedule { Id = Guid.NewGuid(), WorkshopId = workshop.Id, Date = DateTime.UtcNow.Date.AddDays(5) };
        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(11, 0, 0),
            MaxCapacity = 10,
            AvailableCapacity = 10
        };
        context.WorkshopSchedules.Add(schedule);
        context.Slots.Add(slot);
        await context.SaveChangesAsync();

        var bookingService = new BookingService(context, Mock.Of<IQrCodeService>(), Mock.Of<INotificationService>());

        // Act - 3 participants (< threshold 5)
        var dto = new BookingCreateDto
        {
            WorkshopId = workshop.Id,
            SlotId = slot.Id,
            Quantity = 3,
            CustomerName = "Nguyễn Văn Test",
            CustomerEmail = "test@guest.com",
            CustomerPhone = "0901234567"
        };
        var result = await bookingService.CreatePendingBookingAsync(dto, guestUser.Id);

        // Assert
        Assert.True(result.Success);
        var booking = await context.Bookings.FindAsync(result.BookingId);
        Assert.NotNull(booking);
        Assert.Equal(0.12m, booking.CommissionRate);
        Assert.Equal(600000m, booking.TotalAmount); // 3 * 200k
        Assert.Equal(72000m, booking.PlatformFee); // 12% of 600k
        Assert.Equal(528000m, booking.HostEarnings); // 600k - 72k
    }

    [Fact]
    public async Task BR_CM_01_LargeGroup_Commission_Is_15_Percent()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        context.CommissionRules.Add(new CommissionRule
        {
            SmallGroupRate = 0.12m,
            LargeGroupRate = 0.15m,
            GroupSizeThreshold = 5,
            IsActive = true
        });

        var hostUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "host@test.com", Email = "host@test.com" };
        var guestUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "guest@test.com", Email = "guest@test.com" };
        context.Users.AddRange(hostUser, guestUser);

        var workshop = new Workshop
        {
            Id = Guid.NewGuid(),
            Title = "Lụa Vạn Phúc",
            HostId = hostUser.Id,
            BasePrice = 300000m,
            Status = WorkshopStatus.Published
        };
        context.Workshops.Add(workshop);

        var schedule = new WorkshopSchedule { Id = Guid.NewGuid(), WorkshopId = workshop.Id, Date = DateTime.UtcNow.Date.AddDays(5) };
        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            StartTime = new TimeSpan(14, 0, 0),
            EndTime = new TimeSpan(16, 0, 0),
            MaxCapacity = 20,
            AvailableCapacity = 20
        };
        context.WorkshopSchedules.Add(schedule);
        context.Slots.Add(slot);
        await context.SaveChangesAsync();

        var bookingService = new BookingService(context, Mock.Of<IQrCodeService>(), Mock.Of<INotificationService>());

        // Act - 6 participants (>= threshold 5)
        var dto = new BookingCreateDto
        {
            WorkshopId = workshop.Id,
            SlotId = slot.Id,
            Quantity = 6,
            CustomerName = "Trần Thị Đoàn",
            CustomerEmail = "doan@guest.com",
            CustomerPhone = "0912345678"
        };
        var result = await bookingService.CreatePendingBookingAsync(dto, guestUser.Id);

        // Assert
        Assert.True(result.Success);
        var booking = await context.Bookings.FindAsync(result.BookingId);
        Assert.NotNull(booking);
        Assert.Equal(0.15m, booking.CommissionRate);
        Assert.Equal(1800000m, booking.TotalAmount); // 6 * 300k
        Assert.Equal(270000m, booking.PlatformFee); // 15% of 1.8M
        Assert.Equal(1530000m, booking.HostEarnings); // 1.8M - 270k
    }

    [Fact]
    public async Task BR_CN_03_Cancellation_Eligible_When_48_Hours_Or_More_Remaining()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var guestUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "guest@test.com" };
        context.Users.Add(guestUser);

        var workshop = new Workshop { Id = Guid.NewGuid(), Title = "Mây Tre Đan", BasePrice = 150000m };
        var schedule = new WorkshopSchedule { Id = Guid.NewGuid(), WorkshopId = workshop.Id, Date = DateTime.UtcNow.Date.AddDays(4) }; // 96 hours away
        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            MaxCapacity = 10,
            AvailableCapacity = 8,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        context.Workshops.Add(workshop);
        context.WorkshopSchedules.Add(schedule);
        context.Slots.Add(slot);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingCode = "CT-CANCEL48",
            GuestId = guestUser.Id,
            WorkshopId = workshop.Id,
            SlotId = slot.Id,
            Quantity = 2,
            UnitPrice = 150000m,
            TotalAmount = 300000m,
            Status = BookingStatus.Confirmed
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var bookingService = new BookingService(context, Mock.Of<IQrCodeService>(), Mock.Of<INotificationService>());

        // Act
        var success = await bookingService.CancelBookingAsync(booking.Id, guestUser.Id, "Đổi lịch trình cá nhân");

        // Assert
        Assert.True(success);
        var updatedBooking = await context.Bookings.Include(b => b.Refunds).FirstOrDefaultAsync(b => b.Id == booking.Id);
        Assert.NotNull(updatedBooking);
        Assert.Equal(BookingStatus.Canceled, updatedBooking.Status);
        Assert.Single(updatedBooking.Refunds);
        Assert.Equal(300000m, updatedBooking.Refunds.First().Amount); // 100% refund
        Assert.Equal(10, slot.AvailableCapacity); // Capacity restored (+2)
    }

    [Fact]
    public async Task BR_CN_03_Cancellation_Blocked_When_Less_Than_48_Hours_Remaining()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var guestUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "guest@test.com" };
        context.Users.Add(guestUser);

        var workshop = new Workshop { Id = Guid.NewGuid(), Title = "Gốm Bát Tràng", BasePrice = 200000m };
        var schedule = new WorkshopSchedule { Id = Guid.NewGuid(), WorkshopId = workshop.Id, Date = DateTime.UtcNow.Date.AddHours(24) }; // only 24h away (< 48h)
        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            StartTime = DateTime.UtcNow.TimeOfDay,
            EndTime = DateTime.UtcNow.TimeOfDay.Add(TimeSpan.FromHours(2)),
            MaxCapacity = 10,
            AvailableCapacity = 8,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        context.Workshops.Add(workshop);
        context.WorkshopSchedules.Add(schedule);
        context.Slots.Add(slot);

        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingCode = "CT-NOCANCEL",
            GuestId = guestUser.Id,
            WorkshopId = workshop.Id,
            SlotId = slot.Id,
            Quantity = 2,
            UnitPrice = 200000m,
            TotalAmount = 400000m,
            Status = BookingStatus.Confirmed
        };
        context.Bookings.Add(booking);
        await context.SaveChangesAsync();

        var bookingService = new BookingService(context, Mock.Of<IQrCodeService>(), Mock.Of<INotificationService>());

        // Act
        var success = await bookingService.CancelBookingAsync(booking.Id, guestUser.Id, "Xin hủy phút chót");

        // Assert
        Assert.False(success);
        var unchangedBooking = await context.Bookings.FindAsync(booking.Id);
        Assert.Equal(BookingStatus.Confirmed, unchangedBooking!.Status);
        Assert.Equal(8, slot.AvailableCapacity); // Capacity NOT restored
    }

    [Fact]
    public async Task BR_PO_02_HostPayout_Only_Eligible_24_Hours_After_Session_End()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var hostUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "host@test.com" };
        var profile = new HostProfile { Id = Guid.NewGuid(), UserId = hostUser.Id, WalletBalance = 0m, PendingBalance = 500000m };
        context.Users.Add(hostUser);
        context.HostProfiles.Add(profile);

        var booking1 = new Booking { Id = Guid.NewGuid(), BookingCode = "CT-BK1", Status = BookingStatus.Completed };
        var booking2 = new Booking { Id = Guid.NewGuid(), BookingCode = "CT-BK2", Status = BookingStatus.Completed };
        context.Bookings.AddRange(booking1, booking2);

        // Payout 1: Session ended 10 hours ago -> NOT eligible (< 24h)
        var payoutNotEligible = new HostPayout
        {
            Id = Guid.NewGuid(),
            HostId = hostUser.Id,
            BookingId = booking1.Id,
            Booking = booking1,
            Amount = 300000m,
            EligibleAt = DateTime.UtcNow.AddHours(14), // eligible in future
            Status = PayoutStatus.PendingEligibility
        };

        // Payout 2: Session ended 25 hours ago -> ELIGIBLE (> 24h)
        var payoutEligible = new HostPayout
        {
            Id = Guid.NewGuid(),
            HostId = hostUser.Id,
            BookingId = booking2.Id,
            Booking = booking2,
            Amount = 200000m,
            EligibleAt = DateTime.UtcNow.AddHours(-1), // already eligible
            Status = PayoutStatus.PendingEligibility
        };

        context.HostPayouts.AddRange(payoutNotEligible, payoutEligible);
        await context.SaveChangesAsync();

        var financialService = new FinancialService(context);

        // Act
        var processedCount = await financialService.ProcessEligiblePayoutsAsync();

        // Assert
        Assert.Equal(1, processedCount);
        Assert.Equal(PayoutStatus.Eligible, payoutEligible.Status);
        Assert.Equal(PayoutStatus.PendingEligibility, payoutNotEligible.Status);
        Assert.Equal(200000m, profile.WalletBalance); // credited to wallet
    }

    [Fact]
    public async Task BR_PO_02_Prevents_Duplicate_Host_Payouts()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var hostUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "host@test.com" };
        var profile = new HostProfile { Id = Guid.NewGuid(), UserId = hostUser.Id, WalletBalance = 0m, PendingBalance = 200000m };
        context.Users.Add(hostUser);
        context.HostProfiles.Add(profile);

        var booking = new Booking { Id = Guid.NewGuid(), BookingCode = "CT-BKDUP", Status = BookingStatus.Completed };
        context.Bookings.Add(booking);

        var payout = new HostPayout
        {
            Id = Guid.NewGuid(),
            HostId = hostUser.Id,
            BookingId = booking.Id,
            Booking = booking,
            Amount = 200000m,
            EligibleAt = DateTime.UtcNow.AddHours(-2),
            Status = PayoutStatus.PendingEligibility
        };
        context.HostPayouts.Add(payout);
        await context.SaveChangesAsync();

        var financialService = new FinancialService(context);

        // Act 1st run
        var firstCount = await financialService.ProcessEligiblePayoutsAsync();
        // Act 2nd run immediately
        var secondCount = await financialService.ProcessEligiblePayoutsAsync();

        // Assert
        Assert.Equal(1, firstCount);
        Assert.Equal(0, secondCount);
        Assert.Equal(200000m, profile.WalletBalance); // Exactly 200k, not 400k
        Assert.Single(context.HostWalletTransactions.Where(t => t.HostId == hostUser.Id));
    }

    [Fact]
    public async Task Pending_Booking_Expires_After_15_Minutes_And_Restores_Capacity_Once()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            MaxCapacity = 10,
            AvailableCapacity = 6,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        context.Slots.Add(slot);

        var expiredBooking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingCode = "CT-EXP15M",
            SlotId = slot.Id,
            Quantity = 4,
            Status = BookingStatus.Pending,
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5), // Expired 5 mins ago
            IsCapacityRestored = false
        };
        context.Bookings.Add(expiredBooking);
        await context.SaveChangesAsync();

        var bookingService = new BookingService(context, Mock.Of<IQrCodeService>(), Mock.Of<INotificationService>());

        // Act 1st expiration check
        var expiredCount = await bookingService.ExpirePendingBookingsAsync();

        // Assert
        Assert.Equal(1, expiredCount);
        Assert.Equal(BookingStatus.Canceled, expiredBooking.Status);
        Assert.True(expiredBooking.IsCapacityRestored);
        Assert.Equal(10, slot.AvailableCapacity); // 6 + 4 = 10 restored

        // Act 2nd expiration check
        var secondRunCount = await bookingService.ExpirePendingBookingsAsync();
        Assert.Equal(0, secondRunCount);
        Assert.Equal(10, slot.AvailableCapacity); // Still 10, no duplicate restoration
    }

    [Fact]
    public async Task BR_RV_04_Review_Allowed_Only_For_Completed_Bookings()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var guestUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "guest@test.com" };
        var workshop = new Workshop { Id = Guid.NewGuid(), Title = "Gốm", BasePrice = 100000m };
        context.Users.Add(guestUser);
        context.Workshops.Add(workshop);

        var completedBooking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingCode = "CT-COMPLETED",
            GuestId = guestUser.Id,
            WorkshopId = workshop.Id,
            Status = BookingStatus.Completed
        };
        var confirmedBooking = new Booking
        {
            Id = Guid.NewGuid(),
            BookingCode = "CT-CONFIRMED",
            GuestId = guestUser.Id,
            WorkshopId = workshop.Id,
            Status = BookingStatus.Confirmed
        };
        context.Bookings.AddRange(completedBooking, confirmedBooking);
        await context.SaveChangesAsync();

        var reviewService = new ReviewService(context);

        // Act & Assert 1: Confirmed booking cannot review
        var (canConfirmedReview, message1) = await reviewService.CreateReviewAsync(workshop.Id, guestUser.Id, confirmedBooking.Id, 5, "Nhận xét sớm");
        Assert.False(canConfirmedReview);

        // Act & Assert 2: Completed booking CAN review
        var (canCompletedReview, message2) = await reviewService.CreateReviewAsync(workshop.Id, guestUser.Id, completedBooking.Id, 5, "Trải nghiệm gốm rất tuyệt vời!");
        Assert.True(canCompletedReview);

        // Act & Assert 3: Duplicate review for same booking is PREVENTED
        var (secondSubmitSuccess, secondSubmitMsg) = await reviewService.CreateReviewAsync(workshop.Id, guestUser.Id, completedBooking.Id, 4, "Đánh giá lần 2");
        Assert.False(secondSubmitSuccess);
        Assert.Contains("đã gửi đánh giá", secondSubmitMsg);
    }
}
