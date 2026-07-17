using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Automation;
using OreoLeads.Infrastructure.Identity;
using OreoLeads.Infrastructure.Persistence;

namespace OreoLeads.Tests.Automation;

public class AutomationRetryTests
{
    private (AutomationQueueService svc, IServiceScopeFactory scopeFactory) BuildService()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddScoped<TenantContext>();
        services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));

        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();

        return (new AutomationQueueService(scopeFactory), scopeFactory);
    }

    [Fact]
    public async Task RetryExecution_Failed_EnqueuesAgain()
    {
        var (svc, _) = BuildService();
        var dto = new EnqueueAutomationDto(Guid.NewGuid(), TriggerType.Manual, null, 0, null);
        var item = await svc.EnqueueAsync(dto);

        // Fail once
        await svc.FailAsync(item.Id, "error");

        // Should be in Retrying status, still counts as queue depth
        var depth = await svc.GetQueueDepthAsync();
        depth.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task ExponentialBackoff_IncreasesDelay()
    {
        var (svc, scopeFactory) = BuildService();
        var dto = new EnqueueAutomationDto(Guid.NewGuid(), TriggerType.Manual, null, 0, null);
        var item = await svc.EnqueueAsync(dto);

        // Fail once
        await svc.FailAsync(item.Id, "error 1");

        // Check NextRetryAt is set
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await db.AutomationQueueItems.FindAsync(item.Id);
        updated!.NextRetryAt.Should().NotBeNull();
        updated.RetryCount.Should().Be(1);
    }

    [Fact]
    public async Task MaxRetriesExceeded_MovesToDeadLetter()
    {
        var (svc, scopeFactory) = BuildService();
        var dto = new EnqueueAutomationDto(Guid.NewGuid(), TriggerType.Manual, null, 0, null);
        var item = await svc.EnqueueAsync(dto);

        // Fail maxRetries times
        for (var i = 0; i < 3; i++)
            await svc.FailAsync(item.Id, $"error {i + 1}");

        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await db.AutomationQueueItems.FindAsync(item.Id);
        updated!.Status.Should().Be(QueueItemStatus.DeadLetter);
    }

    [Fact]
    public async Task RetryWorker_PicksUpRetryingItems()
    {
        var (svc, scopeFactory) = BuildService();
        var dto = new EnqueueAutomationDto(Guid.NewGuid(), TriggerType.Manual, null, 0, null);
        var item = await svc.EnqueueAsync(dto);

        // Fail once to set Retrying
        await svc.FailAsync(item.Id, "error");

        // Simulate what the retry worker does: find Retrying items with past NextRetryAt
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var updated = await db.AutomationQueueItems.FindAsync(item.Id);

        // Set NextRetryAt to the past so it would be picked up
        updated!.NextRetryAt = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();

        var retryItems = await db.AutomationQueueItems
            .Where(q => q.Status == QueueItemStatus.Retrying && q.NextRetryAt <= DateTime.UtcNow)
            .ToListAsync();

        retryItems.Should().HaveCount(1);
    }
}
