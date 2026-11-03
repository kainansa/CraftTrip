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

namespace CraftTrip.Tests.Workflows;

public class BookingWorkflowIntegrationTests
{
    private CraftTripDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CraftTripDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new CraftTripDbContext(options);
    }

    [Fact]
    public async Task EndToEnd_Booking_Payment_Ticket_And_Host_CheckIn_Workflow()
    {
        // 1. Arrange Data
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var hostUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "artisan@crafttrip.vn", FullName = "Nghệ nhân An" };
        var guestUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "tourist@crafttrip.vn", FullName = "Khách Du Lịch" };
        context.Users.AddRange(hostUser, guestUser);

        var workshop = new Workshop
        {
            Id = Guid.NewGuid(),
            Title = "Trải nghiệm làm nón làng Chuông",
            HostId = hostUser.Id,
            BasePrice = 180000m,
            Status = WorkshopStatus.Published
        };
        context.Workshops.Add(workshop);

        var schedule = new WorkshopSchedule { Id = Guid.NewGuid(), WorkshopId = workshop.Id, Date = DateTime.UtcNow.Date.AddDays(7) };
        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            StartTime = new TimeSpan(8, 30, 0),
            EndTime = new TimeSpan(11, 0, 0),
            MaxCapacity = 10,
            AvailableCapacity = 10,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        context.WorkshopSchedules.Add(schedule);
        context.Slots.Add(slot);
        await context.SaveChangesAsync();

        var qrServiceMock = new Mock<IQrCodeService>();
        qrServiceMock.Setup(q => q.GenerateQrCodeBase64(It.IsAny<string>()))
            .Returns("data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==");

        var bookingService = new BookingService(context, qrServiceMock.Object, Mock.Of<INotificationService>());

        // 2. Act: Guest reserves 3 tickets
        var bookingDto = new BookingCreateDto
        {
            WorkshopId = workshop.Id,
            SlotId = slot.Id,
            Quantity = 3,
            CustomerName = "Khách Du Lịch",
            CustomerEmail = "tourist@crafttrip.vn",
            CustomerPhone = "0988776655"
        };
        var bookingResult = await bookingService.CreatePendingBookingAsync(bookingDto, guestUser.Id);

        // Assert Step 1: Booking is created in Pending status and AvailableCapacity decreased by 3
        Assert.True(bookingResult.Success);
        var bookingId = bookingResult.BookingId!.Value;
        var initialSlot = await context.Slots.FindAsync(slot.Id);
        Assert.Equal(7, initialSlot!.AvailableCapacity); // 10 - 3 = 7

        var initialBooking = await context.Bookings.FindAsync(bookingId);
        Assert.Equal(BookingStatus.Pending, initialBooking!.Status);
        Assert.Equal(540000m, initialBooking.TotalAmount); // 3 * 180k

        // 3. Act: Payment Gateway Callback Confirmed
        var paymentSuccess = await bookingService.ConfirmPaymentSuccessAsync(
            bookingCode: initialBooking.BookingCode,
            transactionNo: "VNP14892812",
            bankCode: "NCB",
            amount: initialBooking.TotalAmount);

        // Assert Step 2: Booking is Confirmed, QR ticket is generated
        Assert.True(paymentSuccess);
        var confirmedBooking = await context.Bookings.Include(b => b.Ticket).FirstOrDefaultAsync(b => b.Id == bookingId);
        Assert.Equal(BookingStatus.Confirmed, confirmedBooking!.Status);
        Assert.NotNull(confirmedBooking.Ticket);
        Assert.False(string.IsNullOrWhiteSpace(confirmedBooking.Ticket.TicketCode));
        Assert.False(string.IsNullOrWhiteSpace(confirmedBooking.Ticket.QrCodeData));

        var ticketCode = confirmedBooking.Ticket.TicketCode;

        // 4. Act: Guest arrives at studio, Host scans / enters ticket code to check-in
        var (checkInSuccess, checkInMsg, checkedInBooking) = await bookingService.CheckInAttendanceAsync(ticketCode, hostUser.Id);

        // Assert Step 3: Check-in succeeds, attendance marked CheckedIn, ticket is redeemed
        Assert.True(checkInSuccess);
        Assert.NotNull(checkedInBooking);
        Assert.Equal(AttendanceStatus.CheckedIn, checkedInBooking.AttendanceStatus);
        Assert.NotNull(checkedInBooking.CheckedInAt);

        var updatedTicket = await context.BookingTickets.FirstOrDefaultAsync(t => t.TicketCode == ticketCode);
        Assert.True(updatedTicket!.IsRedeemed);
        Assert.NotNull(updatedTicket.RedeemedAt);

        // 5. Act: Host attempts to check-in with the SAME ticket code a second time
        var (secondCheckInSuccess, secondCheckInMsg, _) = await bookingService.CheckInAttendanceAsync(ticketCode, hostUser.Id);

        // Assert Step 4: Duplicate check-in is rejected
        Assert.False(secondCheckInSuccess);
        Assert.Contains("đã được sử dụng", secondCheckInMsg);
    }

    [Fact]
    public async Task PaymentConfirmation_TriggersEmail_And_IsIdempotent()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var guestUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "traveler@crafttrip.vn", FullName = "Nguyễn Văn B" };
        var hostUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "artisan2@crafttrip.vn", FullName = "Bác Ba Đất Sét" };
        context.Users.AddRange(guestUser, hostUser);

        var village = new Village { Id = Guid.NewGuid(), Name = "Làng gốm Bát Tràng", Slug = "bat-trang", Location = "Gia Lâm, Hà Nội" };
        context.Villages.Add(village);

        var workshop = new Workshop
        {
            Id = Guid.NewGuid(),
            Title = "Vuốt gốm Bát Tràng cổ truyền",
            HostId = hostUser.Id,
            VillageId = village.Id,
            BasePrice = 250000m,
            Status = WorkshopStatus.Published
        };
        context.Workshops.Add(workshop);

        var schedule = new WorkshopSchedule { Id = Guid.NewGuid(), WorkshopId = workshop.Id, Date = DateTime.UtcNow.Date.AddDays(5) };
        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            StartTime = new TimeSpan(14, 0, 0),
            EndTime = new TimeSpan(16, 30, 0),
            MaxCapacity = 15,
            AvailableCapacity = 15,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        context.WorkshopSchedules.Add(schedule);
        context.Slots.Add(slot);
        await context.SaveChangesAsync();

        var qrServiceMock = new Mock<IQrCodeService>();
        qrServiceMock.Setup(q => q.GenerateQrCodeBase64(It.IsAny<string>()))
            .Returns("data:image/png;base64,mockqr");

        var emailServiceMock = new Mock<IEmailService>();
        emailServiceMock.Setup(e => e.SendBookingConfirmationEmailAsync(It.IsAny<Booking>()))
            .ReturnsAsync(true);

        var bookingService = new BookingService(context, qrServiceMock.Object, Mock.Of<INotificationService>(), emailServiceMock.Object);

        var bookingDto = new BookingCreateDto
        {
            WorkshopId = workshop.Id,
            SlotId = slot.Id,
            Quantity = 2,
            CustomerName = "Nguyễn Văn B",
            CustomerEmail = "traveler@crafttrip.vn",
            CustomerPhone = "0901234567"
        };
        var bookingResult = await bookingService.CreatePendingBookingAsync(bookingDto, guestUser.Id);
        Assert.True(bookingResult.Success);

        var booking = await context.Bookings.FindAsync(bookingResult.BookingId!.Value);
        Assert.NotNull(booking);

        // Act 1: First payment confirmation
        var confirmResult1 = await bookingService.ConfirmPaymentSuccessAsync(booking.BookingCode, "TXN-TEST-100", "VNPAY", 500000m);
        Assert.True(confirmResult1);

        // Verify email service was called exactly once
        emailServiceMock.Verify(e => e.SendBookingConfirmationEmailAsync(It.Is<Booking>(b => b.BookingCode == booking.BookingCode)), Times.Once);

        // Verify 1 ticket generated
        var tickets = await context.BookingTickets.Where(t => t.BookingId == booking.Id).ToListAsync();
        Assert.Single(tickets);

        // Verify 1 payment recorded
        var payments = await context.Payments.Where(p => p.BookingId == booking.Id).ToListAsync();
        Assert.Single(payments);
        Assert.Equal(PaymentStatus.Success, payments[0].Status);

        // Act 2: Duplicate payment confirmation (idempotency check)
        var confirmResult2 = await bookingService.ConfirmPaymentSuccessAsync(booking.BookingCode, "TXN-TEST-100", "VNPAY", 500000m);
        Assert.True(confirmResult2);

        // Verify still only 1 ticket and 1 payment
        tickets = await context.BookingTickets.Where(t => t.BookingId == booking.Id).ToListAsync();
        Assert.Single(tickets);

        payments = await context.Payments.Where(p => p.BookingId == booking.Id).ToListAsync();
        Assert.Single(payments);

        // Verify email was not sent again for duplicate confirmation
        emailServiceMock.Verify(e => e.SendBookingConfirmationEmailAsync(It.IsAny<Booking>()), Times.Once);
    }

    [Fact]
    public async Task Cancellation_RestoresCapacity_And_PreventsDuplicateRestoration()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var guestUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "guest3@crafttrip.vn", FullName = "Lê Thị C" };
        var hostUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "artisan3@crafttrip.vn", FullName = "Nghệ nhân Thêu" };
        context.Users.AddRange(guestUser, hostUser);

        var workshop = new Workshop
        {
            Id = Guid.NewGuid(),
            Title = "Thêu ren Quất Động",
            HostId = hostUser.Id,
            BasePrice = 120000m,
            Status = WorkshopStatus.Published
        };
        context.Workshops.Add(workshop);

        var schedule = new WorkshopSchedule { Id = Guid.NewGuid(), WorkshopId = workshop.Id, Date = DateTime.UtcNow.Date.AddDays(10) };
        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(11, 30, 0),
            MaxCapacity = 8,
            AvailableCapacity = 8,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        context.WorkshopSchedules.Add(schedule);
        context.Slots.Add(slot);
        await context.SaveChangesAsync();

        var bookingService = new BookingService(context, Mock.Of<IQrCodeService>(), Mock.Of<INotificationService>());

        var bookingDto = new BookingCreateDto
        {
            WorkshopId = workshop.Id,
            SlotId = slot.Id,
            Quantity = 4,
            CustomerName = "Lê Thị C",
            CustomerEmail = "guest3@crafttrip.vn",
            CustomerPhone = "0933333333"
        };
        var bookingResult = await bookingService.CreatePendingBookingAsync(bookingDto, guestUser.Id);
        Assert.True(bookingResult.Success);

        // Slot capacity reduced from 8 to 4
        var currentSlot = await context.Slots.FindAsync(slot.Id);
        Assert.Equal(4, currentSlot!.AvailableCapacity);

        // Confirm payment so booking is in Confirmed status
        var booking = await context.Bookings.FindAsync(bookingResult.BookingId!.Value);
        await bookingService.ConfirmPaymentSuccessAsync(booking!.BookingCode, "TXN-CANCEL-TEST", "VNPAY", booking.TotalAmount);

        // Act: Cancel booking (session is 10 days out, well beyond 48-hour policy)
        var cancelResult = await bookingService.CancelBookingAsync(bookingResult.BookingId!.Value, guestUser.Id, "Bận việc đột xuất");
        Assert.True(cancelResult);

        // Slot capacity restored to 8
        currentSlot = await context.Slots.FindAsync(slot.Id);
        Assert.Equal(8, currentSlot!.AvailableCapacity);

        // Act 2: Attempting to cancel again should fail gracefully without double restoring
        var cancelAgain = await bookingService.CancelBookingAsync(bookingResult.BookingId!.Value, guestUser.Id, "Hủy lại");
        Assert.False(cancelAgain);
        currentSlot = await context.Slots.FindAsync(slot.Id);
        Assert.Equal(8, currentSlot!.AvailableCapacity);
    }

    [Fact]
    public async Task GuestTickets_Query_Returns_All_Booked_And_Confirmed_Tickets()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var guestUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "ticketbuyer@crafttrip.vn", FullName = "Nguyễn Thị Mua Vé" };
        var hostUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "hostpottery@crafttrip.vn", FullName = "Nghệ nhân Gốm" };
        context.Users.AddRange(guestUser, hostUser);

        var workshop = new Workshop
        {
            Id = Guid.NewGuid(),
            Title = "Workshop Gốm Bát Tràng",
            HostId = hostUser.Id,
            BasePrice = 300000m,
            Status = WorkshopStatus.Published
        };
        context.Workshops.Add(workshop);

        var schedule = new WorkshopSchedule { Id = Guid.NewGuid(), WorkshopId = workshop.Id, Date = DateTime.UtcNow.Date.AddDays(3) };
        var slot = new Slot
        {
            Id = Guid.NewGuid(),
            ScheduleId = schedule.Id,
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(12, 0, 0),
            MaxCapacity = 10,
            AvailableCapacity = 10,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
        context.WorkshopSchedules.Add(schedule);
        context.Slots.Add(slot);
        await context.SaveChangesAsync();

        var bookingService = new BookingService(context, Mock.Of<IQrCodeService>(), Mock.Of<INotificationService>());

        var bookingDto = new BookingCreateDto
        {
            WorkshopId = workshop.Id,
            SlotId = slot.Id,
            Quantity = 2,
            CustomerName = "Nguyễn Thị Mua Vé",
            CustomerEmail = "ticketbuyer@crafttrip.vn",
            CustomerPhone = "0911223344"
        };
        var bookingResult = await bookingService.CreatePendingBookingAsync(bookingDto, guestUser.Id);
        Assert.True(bookingResult.Success);

        var booking = await context.Bookings.FindAsync(bookingResult.BookingId!.Value);
        await bookingService.ConfirmPaymentSuccessAsync(booking!.BookingCode, "TXN-TICKET-QUERY", "VNPAY", booking.TotalAmount);

        // Act: Retrieve guest bookings
        var guestBookings = await bookingService.GetGuestBookingsAsync(guestUser.Id);

        // Assert: Verified ticket is attached with correct QR Code, status, and details
        Assert.Single(guestBookings);
        var retrievedBooking = guestBookings[0];
        Assert.Equal(BookingStatus.Confirmed, retrievedBooking.Status);
        Assert.NotNull(retrievedBooking.Ticket);
        Assert.NotEmpty(retrievedBooking.Ticket.TicketCode);
        Assert.NotEmpty(retrievedBooking.Ticket.QrCodeData);
        Assert.Equal(workshop.Title, retrievedBooking.Workshop?.Title);
    }
}
