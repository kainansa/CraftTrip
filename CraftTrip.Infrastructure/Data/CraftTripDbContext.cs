using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;

namespace CraftTrip.Infrastructure.Data;

public class CraftTripDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
{
    public CraftTripDbContext(DbContextOptions<CraftTripDbContext> options) : base(options)
    {
    }

    // Identity & Profiles
    public DbSet<HostProfile> HostProfiles => Set<HostProfile>();
    public DbSet<GuestProfile> GuestProfiles => Set<GuestProfile>();

    // Marketplace
    public DbSet<Village> Villages => Set<Village>();
    public DbSet<VillageImage> VillageImages => Set<VillageImage>();
    public DbSet<CraftCategory> CraftCategories => Set<CraftCategory>();
    public DbSet<ArtisanProfile> ArtisanProfiles => Set<ArtisanProfile>();

    // Workshops
    public DbSet<Workshop> Workshops => Set<Workshop>();
    public DbSet<WorkshopImage> WorkshopImages => Set<WorkshopImage>();
    public DbSet<WorkshopSchedule> WorkshopSchedules => Set<WorkshopSchedule>();
    public DbSet<Slot> Slots => Set<Slot>();
    public DbSet<WorkshopRevision> WorkshopRevisions => Set<WorkshopRevision>();

    // Trips
    public DbSet<Trip> Trips => Set<Trip>();
    public DbSet<TripItinerary> TripItineraries => Set<TripItinerary>();
    public DbSet<TripImage> TripImages => Set<TripImage>();

    // Bookings & Reviews
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingTicket> BookingTickets => Set<BookingTicket>();
    public DbSet<BookingStatusHistory> BookingStatusHistories => Set<BookingStatusHistory>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Favorite> Favorites => Set<Favorite>();

    // Financial & Payments
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<HostPayout> HostPayouts => Set<HostPayout>();
    public DbSet<HostWalletTransaction> HostWalletTransactions => Set<HostWalletTransaction>();
    public DbSet<CommissionRule> CommissionRules => Set<CommissionRule>();

    // CMS & System
    public DbSet<ContentPage> ContentPages => Set<ContentPage>();
    public DbSet<ContentSection> ContentSections => Set<ContentSection>();
    public DbSet<MediaAsset> MediaAssets => Set<MediaAsset>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Configure Decimal Precisions for monetary values
        foreach (var property in builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            property.SetPrecision(18);
            property.SetScale(2);
        }

        // HostProfile configuration
        builder.Entity<HostProfile>(b =>
        {
            b.HasOne(h => h.User)
             .WithOne(u => u.HostProfile)
             .HasForeignKey<HostProfile>(h => h.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(h => h.Village)
             .WithMany()
             .HasForeignKey(h => h.VillageId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // GuestProfile configuration
        builder.Entity<GuestProfile>(b =>
        {
            b.HasOne(g => g.User)
             .WithOne(u => u.GuestProfile)
             .HasForeignKey<GuestProfile>(g => g.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Village configuration
        builder.Entity<Village>(b =>
        {
            b.HasIndex(v => v.Slug).IsUnique();
        });

        // CraftCategory configuration
        builder.Entity<CraftCategory>(b =>
        {
            b.HasIndex(c => c.Slug).IsUnique();
        });

        // Workshop configuration
        builder.Entity<Workshop>(b =>
        {
            b.HasIndex(w => w.Slug).IsUnique();

            b.HasOne(w => w.Host)
             .WithMany()
             .HasForeignKey(w => w.HostId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(w => w.Category)
             .WithMany(c => c.Workshops)
             .HasForeignKey(w => w.CategoryId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(w => w.Village)
             .WithMany(v => v.Workshops)
             .HasForeignKey(w => w.VillageId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Slot configuration (Concurrency handling)
        builder.Entity<Slot>(b =>
        {
            b.HasOne(s => s.Schedule)
             .WithMany(sch => sch.Slots)
             .HasForeignKey(s => s.ScheduleId)
             .OnDelete(DeleteBehavior.Cascade);

            if (Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
                b.Property(s => s.RowVersion)
                 .IsRowVersion();
            }
        });

        // Booking configuration
        builder.Entity<Booking>(b =>
        {
            b.HasIndex(bkg => bkg.BookingCode).IsUnique();

            b.HasOne(bkg => bkg.Guest)
             .WithMany(u => u.Bookings)
             .HasForeignKey(bkg => bkg.GuestId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(bkg => bkg.Workshop)
             .WithMany(w => w.Bookings)
             .HasForeignKey(bkg => bkg.WorkshopId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(bkg => bkg.Slot)
             .WithMany(s => s.Bookings)
             .HasForeignKey(bkg => bkg.SlotId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // BookingTicket configuration
        builder.Entity<BookingTicket>(b =>
        {
            b.HasIndex(t => t.TicketCode).IsUnique();

            b.HasOne(t => t.Booking)
             .WithOne(bkg => bkg.Ticket)
             .HasForeignKey<BookingTicket>(t => t.BookingId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Review configuration (1 review per completed booking)
        builder.Entity<Review>(b =>
        {
            b.HasIndex(r => r.BookingId).IsUnique();

            b.HasOne(r => r.Booking)
             .WithOne(bkg => bkg.Review)
             .HasForeignKey<Review>(r => r.BookingId)
             .OnDelete(DeleteBehavior.Restrict);

            b.HasOne(r => r.Workshop)
             .WithMany(w => w.Reviews)
             .HasForeignKey(r => r.WorkshopId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(r => r.Guest)
             .WithMany(u => u.Reviews)
             .HasForeignKey(r => r.GuestId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Favorite configuration
        builder.Entity<Favorite>(b =>
        {
            b.HasIndex(f => new { f.UserId, f.WorkshopId }).IsUnique();

            b.HasOne(f => f.User)
             .WithMany(u => u.Favorites)
             .HasForeignKey(f => f.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(f => f.Workshop)
             .WithMany()
             .HasForeignKey(f => f.WorkshopId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ContentPage configuration
        builder.Entity<ContentPage>(b =>
        {
            b.HasIndex(p => p.PageKey).IsUnique();
        });

        // SystemSetting configuration
        builder.Entity<SystemSetting>(b =>
        {
            b.HasIndex(s => s.Key).IsUnique();
        });

        // HostPayout configuration
        builder.Entity<HostPayout>(b =>
        {
            b.HasIndex(hp => hp.BookingId).IsUnique(); // Prevent duplicate payout per booking
        });
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entityNames = string.Join(", ", ex.Entries.Select(e => $"{e.Entity.GetType().Name} (Id={((BaseEntity)e.Entity).Id}, State={e.State})"));
            throw new Exception($"Concurrency failure on entities: {entityNames}", ex);
        }
    }
}
