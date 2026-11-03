using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.Implementations;
using Xunit;

namespace CraftTrip.Tests.BusinessRules;

public class FinancialDashboardTests
{
    private CraftTripDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CraftTripDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new CraftTripDbContext(options);
    }

    [Fact]
    public async Task AdminFinancialSummary_CalculatesAccurately_And_PreservesDecimalPrecision()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var host = new ApplicationUser { Id = Guid.NewGuid(), UserName = "artisan@test.com" };
        var guest = new ApplicationUser { Id = Guid.NewGuid(), UserName = "customer@test.com" };
        var workshop = new Workshop { Id = Guid.NewGuid(), HostId = host.Id, Title = "Làm nón chuông" };
        context.Users.AddRange(host, guest);
        context.Workshops.Add(workshop);

        // Paid/Completed bookings: Gross = 500,000 + 1,000,000 = 1,500,000; Commission = 60,000 + 150,000 = 210,000
        context.Bookings.AddRange(
            new Booking
            {
                Id = Guid.NewGuid(),
                BookingCode = "CT-PAID1",
                WorkshopId = workshop.Id,
                GuestId = guest.Id,
                TotalAmount = 500000m,
                PlatformFee = 60000m,
                HostEarnings = 440000m,
                Status = BookingStatus.Confirmed
            },
            new Booking
            {
                Id = Guid.NewGuid(),
                BookingCode = "CT-PAID2",
                WorkshopId = workshop.Id,
                GuestId = guest.Id,
                TotalAmount = 1000000m,
                PlatformFee = 150000m,
                HostEarnings = 850000m,
                Status = BookingStatus.Completed
            },
            // Pending booking should NOT contribute to Gross / Commission
            new Booking
            {
                Id = Guid.NewGuid(),
                BookingCode = "CT-PENDING",
                WorkshopId = workshop.Id,
                GuestId = guest.Id,
                TotalAmount = 300000m,
                PlatformFee = 36000m,
                HostEarnings = 264000m,
                Status = BookingStatus.Pending
            }
        );

        // Payouts:
        // Eligible / Transferred to Hosts = 440,000 + 100,000 = 540,000
        // Pending Payouts (waiting for 24h) = 850,000
        context.HostPayouts.AddRange(
            new HostPayout
            {
                Id = Guid.NewGuid(),
                HostId = host.Id,
                Amount = 440000m,
                Status = PayoutStatus.Eligible
            },
            new HostPayout
            {
                Id = Guid.NewGuid(),
                HostId = host.Id,
                Amount = 100000m,
                Status = PayoutStatus.Transferred
            },
            new HostPayout
            {
                Id = Guid.NewGuid(),
                HostId = host.Id,
                Amount = 850000m,
                Status = PayoutStatus.PendingEligibility
            }
        );

        // Refunds: Completed or Processing = 200,000 + 50,000 = 250,000
        context.Refunds.AddRange(
            new Refund
            {
                Id = Guid.NewGuid(),
                Amount = 200000m,
                Status = RefundStatus.Completed
            },
            new Refund
            {
                Id = Guid.NewGuid(),
                Amount = 50000m,
                Status = RefundStatus.Processing
            },
            new Refund
            {
                Id = Guid.NewGuid(),
                Amount = 80000m,
                Status = RefundStatus.Rejected // Should NOT contribute
            }
        );

        await context.SaveChangesAsync();

        var financialService = new FinancialService(context);

        // Act
        var summary = await financialService.GetAdminFinancialSummaryAsync();

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(1500000m, summary.TotalGrossBookingAmount);
        Assert.Equal(210000m, summary.TotalPlatformCommission);
        Assert.Equal(540000m, summary.TotalPaidToHosts);
        Assert.Equal(850000m, summary.TotalPendingPayouts);
        Assert.Equal(250000m, summary.TotalRefundedAmount);
        Assert.Equal(2, summary.TotalPaidBookingsCount);
        Assert.Equal(3, summary.TotalPayoutsCount);
        Assert.Equal(2, summary.TotalRefundsCount);
    }

    [Fact]
    public async Task AdminFinancialSummary_WorksWithEmptyDatabase()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var financialService = new FinancialService(context);

        // Act
        var summary = await financialService.GetAdminFinancialSummaryAsync();

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(0m, summary.TotalGrossBookingAmount);
        Assert.Equal(0m, summary.TotalPlatformCommission);
        Assert.Equal(0m, summary.TotalPaidToHosts);
        Assert.Equal(0m, summary.TotalPendingPayouts);
        Assert.Equal(0m, summary.TotalRefundedAmount);
        Assert.Equal(0, summary.TotalPaidBookingsCount);
    }

    [Fact]
    public async Task HostFinancialSummary_PreservesHostIsolation_And_AggregatesWithoutProviderException()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var hostA = new ApplicationUser { Id = Guid.NewGuid(), UserName = "hostA@test.com" };
        var hostB = new ApplicationUser { Id = Guid.NewGuid(), UserName = "hostB@test.com" };
        context.Users.AddRange(hostA, hostB);

        var profileA = new HostProfile { Id = Guid.NewGuid(), UserId = hostA.Id, WalletBalance = 500000m };
        var profileB = new HostProfile { Id = Guid.NewGuid(), UserId = hostB.Id, WalletBalance = 9000000m };
        context.HostProfiles.AddRange(profileA, profileB);

        var workshopA = new Workshop { Id = Guid.NewGuid(), HostId = hostA.Id, Title = "Workshop A" };
        var workshopB = new Workshop { Id = Guid.NewGuid(), HostId = hostB.Id, Title = "Workshop B" };
        context.Workshops.AddRange(workshopA, workshopB);

        // Bookings for Host A: Total = 600,000, PlatformFee = 72,000, Net = 528,000
        context.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            BookingCode = "CTA-1",
            Workshop = workshopA,
            WorkshopId = workshopA.Id,
            TotalAmount = 600000m,
            PlatformFee = 72000m,
            HostEarnings = 528000m,
            Status = BookingStatus.Confirmed
        });

        // Bookings for Host B (must NOT bleed into Host A)
        context.Bookings.Add(new Booking
        {
            Id = Guid.NewGuid(),
            BookingCode = "CTB-1",
            Workshop = workshopB,
            WorkshopId = workshopB.Id,
            TotalAmount = 5000000m,
            PlatformFee = 600000m,
            HostEarnings = 4400000m,
            Status = BookingStatus.Confirmed
        });

        // Host A payouts: Pending = 300,000, Eligible = 200,000
        context.HostPayouts.AddRange(
            new HostPayout { Id = Guid.NewGuid(), HostId = hostA.Id, Amount = 300000m, Status = PayoutStatus.PendingEligibility },
            new HostPayout { Id = Guid.NewGuid(), HostId = hostA.Id, Amount = 200000m, Status = PayoutStatus.Eligible },
            // Host B payout
            new HostPayout { Id = Guid.NewGuid(), HostId = hostB.Id, Amount = 4400000m, Status = PayoutStatus.Eligible }
        );

        await context.SaveChangesAsync();

        var financialService = new FinancialService(context);

        // Act
        var summaryA = await financialService.GetHostFinancialSummaryAsync(hostA.Id);

        // Assert
        Assert.NotNull(summaryA);
        Assert.Equal(600000m, summaryA.TotalRevenue);
        Assert.Equal(72000m, summaryA.PlatformCommission);
        Assert.Equal(528000m, summaryA.NetHostRevenue);
        Assert.Equal(500000m, summaryA.WalletBalance);
        Assert.Equal(300000m, summaryA.PendingBalance);
        Assert.Equal(200000m, summaryA.EligiblePayout);
        Assert.Equal(1, summaryA.TotalBookingsCount);

        // Verify convenience aliases
        Assert.Equal(600000m, summaryA.TotalGross);
        Assert.Equal(528000m, summaryA.TotalNet);
        Assert.Equal(500000m, summaryA.AvailableBalance);
    }

    [Fact]
    public async Task HostFinancialSummary_WorksWithEmptyHostRecords()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var newHostId = Guid.NewGuid();
        var financialService = new FinancialService(context);

        // Act
        var summary = await financialService.GetHostFinancialSummaryAsync(newHostId);

        // Assert
        Assert.NotNull(summary);
        Assert.Equal(0m, summary.TotalRevenue);
        Assert.Equal(0m, summary.NetHostRevenue);
        Assert.Equal(0m, summary.WalletBalance);
        Assert.Equal(0m, summary.PendingBalance);
        Assert.Equal(0m, summary.EligiblePayout);
        Assert.Equal(0, summary.TotalBookingsCount);
    }
}
