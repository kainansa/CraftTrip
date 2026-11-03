using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using CraftTrip.Core.Entities;
using CraftTrip.Core.Enums;
using CraftTrip.Infrastructure.Data;
using CraftTrip.Services.DTOs;
using CraftTrip.Services.Implementations;
using CraftTrip.Services.Interfaces;
using CraftTrip.Web.Areas.Admin.Controllers;
using CraftTrip.Web.Areas.Host.Controllers;

namespace CraftTrip.Tests.Security;

public class SecurityAndAuthorizationTests
{
    private CraftTripDbContext CreateInMemoryContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<CraftTripDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;
        return new CraftTripDbContext(options);
    }

    [Theory]
    [InlineData(typeof(CraftTrip.Web.Areas.Host.Controllers.DashboardController))]
    [InlineData(typeof(CraftTrip.Web.Areas.Host.Controllers.WorkshopController))]
    [InlineData(typeof(CraftTrip.Web.Areas.Host.Controllers.ScheduleController))]
    [InlineData(typeof(CraftTrip.Web.Areas.Host.Controllers.BookingController))]
    [InlineData(typeof(CraftTrip.Web.Areas.Host.Controllers.FinanceController))]
    public void HostAreaControllers_Must_Have_Host_Or_Admin_Role_Authorization(Type controllerType)
    {
        var areaAttr = controllerType.GetCustomAttribute<AreaAttribute>();
        Assert.NotNull(areaAttr);
        Assert.Equal("Host", areaAttr.RouteValue);

        var authAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Contains("Host", authAttr.Roles!);
    }

    [Theory]
    [InlineData(typeof(CraftTrip.Web.Areas.Admin.Controllers.DashboardController))]
    [InlineData(typeof(CraftTrip.Web.Areas.Admin.Controllers.ApprovalController))]
    [InlineData(typeof(CraftTrip.Web.Areas.Admin.Controllers.FinancialController))]
    [InlineData(typeof(CraftTrip.Web.Areas.Admin.Controllers.CmsController))]
    [InlineData(typeof(CraftTrip.Web.Areas.Admin.Controllers.UserController))]
    public void AdminAreaControllers_Must_Strictly_Require_Admin_Role_Authorization(Type controllerType)
    {
        var areaAttr = controllerType.GetCustomAttribute<AreaAttribute>();
        Assert.NotNull(areaAttr);
        Assert.Equal("Admin", areaAttr.RouteValue);

        var authAttr = controllerType.GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(authAttr);
        Assert.Equal("Admin", authAttr.Roles);
    }

    [Fact]
    public async Task IDOR_Prevention_HostA_Cannot_Access_Or_Edit_HostB_Workshop()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var hostA = new ApplicationUser { Id = Guid.NewGuid(), UserName = "hostA@test.com" };
        var hostB = new ApplicationUser { Id = Guid.NewGuid(), UserName = "hostB@test.com" };
        context.Users.AddRange(hostA, hostB);

        var category = new CraftCategory { Id = Guid.NewGuid(), Name = "Gốm", Slug = "gom" };
        context.CraftCategories.Add(category);

        var workshopB = new Workshop
        {
            Id = Guid.NewGuid(),
            Title = "Xưởng của Host B",
            HostId = hostB.Id,
            CategoryId = category.Id,
            Status = WorkshopStatus.Draft
        };
        context.Workshops.Add(workshopB);
        await context.SaveChangesAsync();

        var workshopService = new WorkshopService(context);

        // Act - Host A attempts to load workshop owned by Host B for editing
        var editResult = await workshopService.GetWorkshopForEditAsync(workshopB.Id, hostA.Id);

        // Assert - strictly null, preventing IDOR access
        Assert.Null(editResult);

        // Act - Host A attempts to submit moderation for Host B's workshop
        var submitResult = await workshopService.SubmitForModerationAsync(workshopB.Id, hostA.Id);
        Assert.False(submitResult);
    }

    [Fact]
    public async Task Moderation_Workflow_FullLifecycle_Draft_Pending_Approved_Published()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var host = new ApplicationUser { Id = Guid.NewGuid(), UserName = "host@test.com" };
        var admin = new ApplicationUser { Id = Guid.NewGuid(), UserName = "admin@test.com" };
        var category = new CraftCategory { Id = Guid.NewGuid(), Name = "Lụa", Slug = "lua" };
        context.Users.AddRange(host, admin);
        context.CraftCategories.Add(category);
        await context.SaveChangesAsync();

        var workshopService = new WorkshopService(context);
        var adminService = new AdminService(context, Mock.Of<INotificationService>());

        // Step 1: Host creates workshop -> Initial status is Draft
        var createDto = new WorkshopCreateDto
        {
            Title = "Dệt lụa tơ tằm cổ truyền",
            CategoryId = category.Id,
            ShortDescription = "Tự tay quay tơ dệt lụa",
            FullDescription = "Trải nghiệm không gian dệt lụa trăm năm",
            BasePrice = 350000m,
            LocationAddress = "Vạn Phúc, Hà Đông"
        };
        var workshopId = await workshopService.CreateWorkshopAsync(createDto, host.Id);
        var created = await context.Workshops.FindAsync(workshopId);
        Assert.NotNull(created);
        Assert.Equal(WorkshopStatus.Draft, created.Status);

        // Verify public marketplace DOES NOT display Draft workshops
        var publicListBefore = await workshopService.GetFeaturedWorkshopsAsync(50);
        Assert.DoesNotContain(publicListBefore, w => w.Id == workshopId);

        // Step 2: Host submits for moderation -> Status changes to Pending
        var submitted = await workshopService.SubmitForModerationAsync(workshopId, host.Id);
        Assert.True(submitted);
        var pending = await context.Workshops.FindAsync(workshopId);
        Assert.Equal(WorkshopStatus.Pending, pending!.Status);

        // Verify public marketplace STILL DOES NOT display Pending workshops
        var publicListPending = await workshopService.GetFeaturedWorkshopsAsync(50);
        Assert.DoesNotContain(publicListPending, w => w.Id == workshopId);

        // Step 3: Admin reviews and approves -> Status becomes Published
        var approved = await adminService.ApproveWorkshopAsync(workshopId, admin.Id);
        Assert.True(approved);
        var published = await context.Workshops.FindAsync(workshopId);
        Assert.Equal(WorkshopStatus.Published, published!.Status);

        // Step 4: Verify public marketplace NOW displays the published workshop!
        var publicListAfter = await workshopService.GetFeaturedWorkshopsAsync(50);
        Assert.Contains(publicListAfter, w => w.Id == workshopId);
    }

    [Fact]
    public async Task Published_Workshop_Revision_Workflow_Maintains_Original_Until_Approved()
    {
        // Arrange
        var context = CreateInMemoryContext(Guid.NewGuid().ToString());
        var host = new ApplicationUser { Id = Guid.NewGuid(), UserName = "host@test.com" };
        var admin = new ApplicationUser { Id = Guid.NewGuid(), UserName = "admin@test.com" };
        var category = new CraftCategory { Id = Guid.NewGuid(), Name = "Gốm", Slug = "gom" };
        context.Users.AddRange(host, admin);
        context.CraftCategories.Add(category);

        var originalWorkshop = new Workshop
        {
            Id = Guid.NewGuid(),
            Title = "Lớp Gốm Bát Tràng Cơ Bản",
            BasePrice = 200000m,
            DurationMinutes = 90,
            HostId = host.Id,
            CategoryId = category.Id,
            Status = WorkshopStatus.Published
        };
        context.Workshops.Add(originalWorkshop);
        await context.SaveChangesAsync();

        var workshopService = new WorkshopService(context);
        var adminService = new AdminService(context, Mock.Of<INotificationService>());

        // Act 1: Host proposes a revision with new price 280k and new title
        var revisionDto = new WorkshopRevisionDto
        {
            WorkshopId = originalWorkshop.Id,
            ProposedTitle = "Lớp Gốm Bát Tràng Nâng Cao & Nung Men",
            ProposedBasePrice = 280000m,
            ProposedDurationMinutes = 120,
            ProposedShortDescription = "Trải nghiệm nung men hỏa biến",
            ProposedFullDescription = "Chi tiết buổi học nung men"
        };
        var proposeSuccess = await workshopService.ProposeRevisionAsync(revisionDto, host.Id);
        Assert.True(proposeSuccess);

        // Assert 1: Public workshop remains untouched while revision is pending
        var liveWorkshop = await context.Workshops.FindAsync(originalWorkshop.Id);
        Assert.Equal("Lớp Gốm Bát Tràng Cơ Bản", liveWorkshop!.Title);
        Assert.Equal(200000m, liveWorkshop.BasePrice);
        Assert.Equal(90, liveWorkshop.DurationMinutes);

        // Act 2: Admin approves revision
        var revision = await context.WorkshopRevisions.FirstOrDefaultAsync(r => r.WorkshopId == originalWorkshop.Id);
        Assert.NotNull(revision);

        var approved = await adminService.ApproveRevisionAsync(revision.Id, admin.Id);
        Assert.True(approved);

        // Assert 2: Workshop is now updated with the approved revision values
        var updatedWorkshop = await context.Workshops.FindAsync(originalWorkshop.Id);
        Assert.Equal("Lớp Gốm Bát Tràng Nâng Cao & Nung Men", updatedWorkshop!.Title);
        Assert.Equal(280000m, updatedWorkshop.BasePrice);
        Assert.Equal(120, updatedWorkshop.DurationMinutes);
    }
}
