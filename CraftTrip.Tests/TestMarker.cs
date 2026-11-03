using Microsoft.EntityFrameworkCore;
using CraftTrip.Core.Entities;
using CraftTrip.Infrastructure.Data;

namespace CraftTrip.Tests;

public class TestMarker
{
    [Fact]
    public void SanityCheck()
    {
        Assert.True(true);
    }

    [Fact]
    public void CheckSlotRowVersion()
    {
        var options = new DbContextOptionsBuilder<CraftTripDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new CraftTripDbContext(options);
        var prop = ctx.Model.FindEntityType(typeof(Slot))?.FindProperty("RowVersion");
        Assert.False(prop?.IsConcurrencyToken ?? false, $"IsConcurrencyToken is {prop?.IsConcurrencyToken}");
    }
}
