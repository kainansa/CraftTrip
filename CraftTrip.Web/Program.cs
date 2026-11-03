using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Interfaces;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Infrastructure.Payments;
using CraftTrip.Infrastructure.Services;
using CraftTrip.Services.Background;
using CraftTrip.Services.Implementations;
using CraftTrip.Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration with SQL Server and automatic SQLite Fallback
var dbProvider = builder.Configuration["Database:Provider"] ?? "Auto";
var sqlServerConn = builder.Configuration.GetConnectionString("DefaultConnection");
var sqliteConn = builder.Configuration.GetConnectionString("SqliteConnection") ?? "Data Source=crafttrip.db";

bool useSqlite = false;
if (dbProvider.Equals("Sqlite", StringComparison.OrdinalIgnoreCase))
{
    useSqlite = true;
}
else if (dbProvider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase))
{
    useSqlite = false;
}
else
{
    // Auto: Test if SQL Server is active and reachable
    try
    {
        if (!string.IsNullOrEmpty(sqlServerConn))
        {
            using var sqlConn = new Microsoft.Data.SqlClient.SqlConnection(sqlServerConn);
            sqlConn.Open();
            sqlConn.Close();
            useSqlite = false;
        }
        else
        {
            useSqlite = true;
        }
    }
    catch
    {
        // SQL Server not running or inaccessible -> fallback to local SQLite
        useSqlite = true;
    }
}

builder.Services.AddDbContext<CraftTripDbContext>(options =>
{
    if (useSqlite)
    {
        options.UseSqlite(sqliteConn);
    }
    else
    {
        options.UseSqlServer(sqlServerConn, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(3);
        });
    }
});

// 2. ASP.NET Core Identity Configuration
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<CraftTripDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "CraftTrip.Auth";
    options.ExpireTimeSpan = TimeSpan.FromDays(14);
    options.SlidingExpiration = true;
});

// 3. Application Services Registration (Dependency Injection)
builder.Services.AddScoped<IWorkshopService, WorkshopService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<IFinancialService, FinancialService>();
builder.Services.AddScoped<ITripService, TripService>();
builder.Services.AddScoped<ICmsService, CmsService>();
builder.Services.AddScoped<IReviewService, ReviewService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IMediaStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IQrCodeService, QrCodeService>();
builder.Services.AddScoped<IVNPayService, VNPayService>();
builder.Services.AddScoped<IEmailService, EmailService>();

// 4. Background Hosted Service for 15m Expirations and 24h Payouts
builder.Services.AddHostedService<BookingExpirationBackgroundService>();

// 5. MVC with Views & Routing
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 6. Database Initialization & Seeding on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<CraftTripDbContext>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        await context.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(context, userManager, roleManager);
        logger.LogInformation("CraftTrip database initialized and seeded successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// 7. HTTP Pipeline Configuration
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 8. Route Mapping
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
