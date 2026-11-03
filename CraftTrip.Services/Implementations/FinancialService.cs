using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.DTOs;
using CraftTrip.Services.Interfaces;

namespace CraftTrip.Services.Implementations;

public class FinancialService : IFinancialService
{
    private readonly CraftTripDbContext _context;

    public FinancialService(CraftTripDbContext context)
    {
        _context = context;
    }

    public async Task<int> ProcessEligiblePayoutsAsync()
    {
        var now = DateTime.UtcNow;

        // BR_PO_02: Find payouts whose session ended >= 24h ago
        var eligiblePayouts = await _context.HostPayouts
            .Include(p => p.Booking)
            .Where(p => p.Status == PayoutStatus.PendingEligibility && p.EligibleAt <= now)
            .ToListAsync();

        if (!eligiblePayouts.Any()) return 0;

        int processed = 0;
        foreach (var payout in eligiblePayouts)
        {
            var booking = payout.Booking;
            if (booking == null) continue;

            // Only successfully paid, non-refunded bookings may contribute to payout
            if (booking.Status != BookingStatus.Confirmed && booking.Status != BookingStatus.Completed)
            {
                continue;
            }

            payout.Status = PayoutStatus.Eligible;
            payout.PaidAt = now;

            // Credit Host Profile Wallet
            var hostProfile = await _context.HostProfiles.FirstOrDefaultAsync(h => h.UserId == payout.HostId);
            if (hostProfile != null)
            {
                hostProfile.WalletBalance += payout.Amount;
                hostProfile.TotalEarned += payout.Amount;

                _context.HostWalletTransactions.Add(new HostWalletTransaction
                {
                    HostId = payout.HostId,
                    Amount = payout.Amount,
                    TransactionType = WalletTxType.Credit,
                    BalanceAfter = hostProfile.WalletBalance,
                    Description = $"Cộng doanh thu từ đơn đặt chỗ {booking.BookingCode} (Đủ điều kiện sau 24h kết thúc ca)",
                    ReferenceId = booking.BookingCode
                });
            }

            processed++;
        }

        await _context.SaveChangesAsync();
        return processed;
    }

    public async Task<HostFinancialSummaryDto> GetHostFinancialSummaryAsync(Guid hostId)
    {
        var bookingStats = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Workshop != null && b.Workshop.HostId == hostId &&
                        (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed))
            .Select(b => new { b.TotalAmount, b.PlatformFee, b.HostEarnings, b.Status })
            .ToListAsync();

        var hostProfile = await _context.HostProfiles.AsNoTracking().FirstOrDefaultAsync(h => h.UserId == hostId);

        var totalRevenue = bookingStats.Sum(b => b.TotalAmount);
        var platformCommission = bookingStats.Sum(b => b.PlatformFee);
        var netHostRevenue = bookingStats.Sum(b => b.HostEarnings);

        var pendingPayoutAmounts = await _context.HostPayouts
            .AsNoTracking()
            .Where(p => p.HostId == hostId && p.Status == PayoutStatus.PendingEligibility)
            .Select(p => p.Amount)
            .ToListAsync();
        var pendingPayout = pendingPayoutAmounts.Sum();

        var eligiblePayoutAmounts = await _context.HostPayouts
            .AsNoTracking()
            .Where(p => p.HostId == hostId && p.Status == PayoutStatus.Eligible)
            .Select(p => p.Amount)
            .ToListAsync();
        var eligiblePayout = eligiblePayoutAmounts.Sum();

        return new HostFinancialSummaryDto
        {
            TotalRevenue = totalRevenue,
            PlatformCommission = platformCommission,
            NetHostRevenue = netHostRevenue,
            WalletBalance = hostProfile?.WalletBalance ?? 0m,
            PendingBalance = pendingPayout,
            EligiblePayout = eligiblePayout,
            TotalBookingsCount = bookingStats.Count,
            CompletedBookingsCount = bookingStats.Count(b => b.Status == BookingStatus.Completed)
        };
    }

    public async Task<AdminFinancialSummaryDto> GetAdminFinancialSummaryAsync()
    {
        // 1. Gross Booking Volume & Platform Commission (Confirmed or Completed)
        var bookingAmounts = await _context.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed)
            .Select(b => new { b.TotalAmount, b.PlatformFee })
            .ToListAsync();

        var totalGross = bookingAmounts.Sum(b => b.TotalAmount);
        var totalCommission = bookingAmounts.Sum(b => b.PlatformFee);

        // 2. Total Paid / Transferred to Hosts (Eligible, Transferred, or Withdrawn)
        var paidPayoutAmounts = await _context.HostPayouts
            .AsNoTracking()
            .Where(p => p.Status == PayoutStatus.Eligible || p.Status == PayoutStatus.Transferred || p.Status == PayoutStatus.Withdrawn)
            .Select(p => p.Amount)
            .ToListAsync();

        var totalPaidToHosts = paidPayoutAmounts.Sum();

        // 3. Total Pending Payouts (Waiting for 24h eligibility)
        var pendingPayoutAmounts = await _context.HostPayouts
            .AsNoTracking()
            .Where(p => p.Status == PayoutStatus.PendingEligibility)
            .Select(p => p.Amount)
            .ToListAsync();

        var totalPendingPayouts = pendingPayoutAmounts.Sum();

        // 4. Total Refunded Amount (Completed or Processing refunds)
        var refundedAmounts = await _context.Refunds
            .AsNoTracking()
            .Where(r => r.Status == RefundStatus.Completed || r.Status == RefundStatus.Processing)
            .Select(r => r.Amount)
            .ToListAsync();

        var totalRefunded = refundedAmounts.Sum();

        return new AdminFinancialSummaryDto
        {
            TotalGrossBookingAmount = totalGross,
            TotalPlatformCommission = totalCommission,
            TotalPaidToHosts = totalPaidToHosts,
            TotalPendingPayouts = totalPendingPayouts,
            TotalRefundedAmount = totalRefunded,
            TotalPaidBookingsCount = bookingAmounts.Count,
            TotalPayoutsCount = paidPayoutAmounts.Count + pendingPayoutAmounts.Count,
            TotalRefundsCount = refundedAmounts.Count
        };
    }

    public async Task<bool> RequestPayoutWithdrawalAsync(Guid hostId, decimal amount, string? note)
    {
        if (amount <= 0) return false;

        var hostProfile = await _context.HostProfiles.FirstOrDefaultAsync(h => h.UserId == hostId);
        if (hostProfile == null || hostProfile.WalletBalance < amount)
        {
            return false;
        }

        hostProfile.WalletBalance -= amount;

        _context.HostWalletTransactions.Add(new HostWalletTransaction
        {
            HostId = hostId,
            Amount = -amount,
            TransactionType = WalletTxType.Payout,
            BalanceAfter = hostProfile.WalletBalance,
            Description = $"Rút tiền về tài khoản ngân hàng ({hostProfile.BankName} - {hostProfile.BankAccountNumber}). {note}",
            ReferenceId = $"WD-{DateTime.UtcNow:yyMMddHHmmss}"
        });

        await _context.SaveChangesAsync();
        return true;
    }
}
