using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OreoLeads.Application.Features.Automation.DTOs;
using OreoLeads.Domain.Enums;
using OreoLeads.Infrastructure.Automation;
using OreoLeads.Infrastructure.Identity;
using OreoLeads.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace OreoLeads.Tests.Automation;

public class AutomationQueueTests
{
    private (AutomationQueueService svc, ApplicationDbContext db) BuildService()
    {
        var dbName = Guid.NewGuid().ToString();
        var services = new ServiceCollection();
        services.AddScoped<TenantContext>();
        services.AddDbContext<ApplicationDbContext>(opt => opt.UseInMemoryDatabase(dbName));

        var sp = services.BuildServiceProvider();
        var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
        var db = sp.GetRequiredService<ApplicationDbContext>();

        return (new AutomationQueueService(scopeFactory), db);
    }

    [Fact]
    public async Task Enqueue_ValidItem_ReturnsPending()
    {
        var (svc, _) = BuildService();
        var dto = new EnqueueAutomationDto(Guid.NewGuid(), TriggerType.Manual, null, 0, null);

        var item = await svc.EnqueueAsync(dto);

        item.Should().NotBeNull();
        item.Status.Should().Be(QueueItemStatus.Pending);
    }

    [Fact]
    public async Task Dequeue_HasPendingItem_ReturnsItem()
    {
        var (svc, _) = BuildService();
        var dto = new EnqueueAutomationDto(Guid.NewGuid(), TriggerType.Manual, null, 0, null);
        await svc.EnqueueAsync(dto);

        var item = await svc.DequeueAsync("worker-1");

        item.Should().NotBeNull();
        item!.Status.Should().Be(QueueItemStatus.Running);
        item.LockedBy.Should().Be("worker-1");
    }

    [Fact]
    public async Task Dequeue_EmptyQueue_ReturnsNull()
    {
        var (svc, _) = BuildService();

        var item = await svc.DequeueAsync("worker-1");

        item.Should().BeNull();
    }

    [Fact]
    public async Task Complete_MarksCompleted()
    {
        var (svc, _) = BuildService();
        var dto = new EnqueueAutomationDto(Guid.NewGuid(), TriggerType.Manual, null, 0, null);
        var enqueued = await svc.EnqueueAsync(dto);
        var dequeued = await svc.DequeueAsync("worker-1");

        await svc.CompleteAsync(dequeued!.Id);

        // Verify by getting queue depth (completed items not counted)
        var depth = await svc.GetQueueDepthAsync();
        depth.Should().Be(0);
    }

    [Fact]
    public async Task Fail_BelowMaxRetries_SetsRetrying()
    {
        var (svc, _) = BuildService();
        var dto = new EnqueueAutomationDto(Guid.NewGuid(), TriggerType.Manual, null, 0, null);
        var enqueued = await svc.EnqueueAsync(dto);

        await svc.FailAsync(enqueued.Id, "test error");

        // Should be retrying (1 retry < 3 max)
        var depth = await svc.GetQueueDepthAsync();
        depth.Should().Be(1); // Retrying counts
    }

    [Fact]
    public async Task Fail_AtMaxRetries_MovesToDeadLetter()
    {
        var (svc, db) = BuildService();
        var dto = new EnqueueAutomationDto(Guid.NewGuid(), TriggerType.Manual, null, 0, null);
        var enqueued = await svc.EnqueueAsync(dto);

        // Fail 3 times (MaxRetries = 3)
        await svc.FailAsync(enqueued.Id, "error 1");
        await svc.FailAsync(enqueued.Id, "error 2");
        await svc.FailAsync(enqueued.Id, "error 3");

        var depth = await svc.GetQueueDepthAsync();
        depth.Should().Be(0); // DeadLetter not counted
    }

    [Fact]
    public async Task Dequeue_HighPriorityFirst()
    {
        var (svc, _) = BuildService();
        var workflowId = Guid.NewGuid();

        await svc.EnqueueAsync(new EnqueueAutomationDto(workflowId, TriggerType.Manual, "low", 1, null));
        await svc.EnqueueAsync(new EnqueueAutomationDto(workflowId, TriggerType.Manual, "high", 10, null));

        var item = await svc.DequeueAsync("worker-1");

        item.Should().NotBeNull();
        item!.Payload.Should().Be("high");
    }

    [Fact]
    public async Task GetQueueDepth_ReturnsCount()
    {
        var (svc, _) = BuildService();
        await svc.EnqueueAsync(new EnqueueAutomationDto(Guid.NewGuid(), TriggerType.Manual, null, 0, null));
        await svc.EnqueueAsync(new EnqueueAutomationDto(Guid.NewGuid(), TriggerType.Manual, null, 0, null));

        var depth = await svc.GetQueueDepthAsync();

        depth.Should().Be(2);
    }
}
