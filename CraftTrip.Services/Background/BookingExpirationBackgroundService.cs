using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Services.Background;

public class BookingExpirationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BookingExpirationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1);

    public BookingExpirationBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<BookingExpirationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BookingExpirationBackgroundService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();
                var financialService = scope.ServiceProvider.GetRequiredService<IFinancialService>();

                // 1. Expire pending bookings after 15 minutes and restore capacity
                var expiredCount = await bookingService.ExpirePendingBookingsAsync();
                if (expiredCount > 0)
                {
                    _logger.LogInformation("Expired {ExpiredCount} pending bookings and restored capacity.", expiredCount);
                }

                // 2. Process eligible host payouts 24h after workshop ends (BR_PO_02)
                var payoutCount = await financialService.ProcessEligiblePayoutsAsync();
                if (payoutCount > 0)
                {
                    _logger.LogInformation("Processed {PayoutCount} eligible host payouts.", payoutCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in BookingExpirationBackgroundService.");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("BookingExpirationBackgroundService stopped.");
    }
}
