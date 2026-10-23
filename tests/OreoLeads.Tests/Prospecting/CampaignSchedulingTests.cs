using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Api.Controllers;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Prospecting;

public class CampaignSchedulingTests
{
    [Fact]
    public async Task DiscoverNowAdvancesScheduleWithoutResettingCountersOrLimits()
    {
        await using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var user = new User();
        var campaign = new ProspectingCampaign
        {
            OrganizationId = user.OrganizationId!.Value, Status = "Active", DiscoveredToday = 15,
            PagesToday = 1, DailyLimit = 100, DailyPageLimit = 20, MaxPendingEnrichments = 20,
            NextDiscoveryAt = DateTime.UtcNow.AddHours(1)
        };
        db.ProspectingCampaigns.Add(campaign);
        await db.SaveChangesAsync();
        var controller = new ProspectingController(db, user);
        Assert.IsType<OkObjectResult>(await controller.Transition(campaign.Id, "discover", default));
        Assert.True(campaign.NextDiscoveryAt <= DateTime.UtcNow);
        Assert.Equal(15, campaign.DiscoveredToday);
        Assert.Equal(1, campaign.PagesToday);
        Assert.Equal(100, campaign.DailyLimit);
        Assert.Equal(20, campaign.MaxPendingEnrichments);

        campaign.Status = "Cancelled";
        Assert.IsType<ConflictObjectResult>(await controller.Transition(campaign.Id, "discover", default));
        Assert.Equal("Cancelled", campaign.Status);
    }

    private sealed class User : ICurrentUserService
    {
        public Guid? OrganizationId { get; } = Guid.NewGuid();
        public string? UserId => null;
        public string? UserEmail => null;
        public string? UserName => null;
        public bool IsAuthenticated => true;
        public bool IsInRole(string role) => false;
    }
}
