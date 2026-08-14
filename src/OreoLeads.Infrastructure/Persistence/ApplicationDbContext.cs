using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
using OreoLeads.Domain.Entities.Analytics;
using OreoLeads.Domain.Entities.Automation;
using OreoLeads.Infrastructure.Identity;

namespace OreoLeads.Infrastructure.Persistence;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    private readonly TenantContext _tenant;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        TenantContext? tenant = null)
        : base(options)
    {
        // Always non-null — a fresh TenantContext with null OrganizationId means "no filter"
        _tenant = tenant ?? new TenantContext();
    }

    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<LeadActivity> LeadActivities => Set<LeadActivity>();
    public DbSet<LeadNote> LeadNotes => Set<LeadNote>();
    public DbSet<FollowUp> FollowUps => Set<FollowUp>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<LeadTag> LeadTags => Set<LeadTag>();
    public DbSet<WebsiteAnalysis> WebsiteAnalyses => Set<WebsiteAnalysis>();
    public DbSet<GeneratedEmail> GeneratedEmails => Set<GeneratedEmail>();
    public DbSet<CompanyContact> CompanyContacts => Set<CompanyContact>();
    public DbSet<SearchQuery> SearchQueries => Set<SearchQuery>();
    public DbSet<EmailDraftVersion> EmailDraftVersions => Set<EmailDraftVersion>();
    public DbSet<AiConfiguration> AiConfigurations => Set<AiConfiguration>();
    public DbSet<PromptTemplate> PromptTemplates => Set<PromptTemplate>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<BrevoConfiguration> BrevoConfigurations => Set<BrevoConfiguration>();
    public DbSet<EmailSendJob> EmailSendJobs => Set<EmailSendJob>();
    public DbSet<SmsSendJob>   SmsSendJobs   => Set<SmsSendJob>();
    public DbSet<LeadEnrichment> LeadEnrichments => Set<LeadEnrichment>();
    public DbSet<EmailEvent> EmailEvents => Set<EmailEvent>();
    public DbSet<UnsubscribeRecord> UnsubscribeRecords => Set<UnsubscribeRecord>();
    public DbSet<AirtableConfiguration> AirtableConfigurations => Set<AirtableConfiguration>();
    public DbSet<AirtableFieldMapping> AirtableFieldMappings => Set<AirtableFieldMapping>();
    public DbSet<AirtableSyncJob> AirtableSyncJobs => Set<AirtableSyncJob>();
    public DbSet<AirtableSyncLog> AirtableSyncLogs => Set<AirtableSyncLog>();
    public DbSet<AirtableRecordLink> AirtableRecordLinks => Set<AirtableRecordLink>();
    public DbSet<InviteCode>  InviteCodes  => Set<InviteCode>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    // Automation
    public DbSet<AutomationWorkflow> AutomationWorkflows => Set<AutomationWorkflow>();
    public DbSet<AutomationTrigger> AutomationTriggers => Set<AutomationTrigger>();
    public DbSet<AutomationAction> AutomationActions => Set<AutomationAction>();
    public DbSet<AutomationCondition> AutomationConditions => Set<AutomationCondition>();
    public DbSet<AutomationVariable> AutomationVariables => Set<AutomationVariable>();
    public DbSet<AutomationSchedule> AutomationSchedules => Set<AutomationSchedule>();
    public DbSet<AutomationExecution> AutomationExecutions => Set<AutomationExecution>();
    public DbSet<AutomationExecutionLog> AutomationExecutionLogs => Set<AutomationExecutionLog>();
    public DbSet<AutomationExecutionError> AutomationExecutionErrors => Set<AutomationExecutionError>();
    public DbSet<AutomationQueueItem> AutomationQueueItems => Set<AutomationQueueItem>();
    public DbSet<AutomationTemplate> AutomationTemplates => Set<AutomationTemplate>();
    public DbSet<AutomationFolder> AutomationFolders => Set<AutomationFolder>();
    public DbSet<AutomationVersion> AutomationVersions => Set<AutomationVersion>();

    // Analytics
    public DbSet<AnalyticsDashboard> AnalyticsDashboards => Set<AnalyticsDashboard>();
    public DbSet<AnalyticsWidget> AnalyticsWidgets => Set<AnalyticsWidget>();
    public DbSet<AnalyticsReport> AnalyticsReports => Set<AnalyticsReport>();
    public DbSet<AnalyticsScheduledReport> AnalyticsScheduledReports => Set<AnalyticsScheduledReport>();
    public DbSet<AnalyticsForecast> AnalyticsForecasts => Set<AnalyticsForecast>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Multi-tenant query filters — evaluated per-request using TenantContext.
        // When OrganizationId is null (no tenant set), all rows are visible (admin / migrations).
        modelBuilder.Entity<Lead>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<GeneratedEmail>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<WebsiteAnalysis>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<FollowUp>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<SearchQuery>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<LeadEnrichment>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);

        modelBuilder.Entity<LeadEnrichment>(b =>
        {
            b.HasOne(e => e.Lead)
                .WithMany(l => l.Enrichments)
                .HasForeignKey(e => e.LeadId)
                .OnDelete(DeleteBehavior.Cascade);
            b.HasIndex(e => new { e.Status, e.ScheduledAt });
            b.HasIndex(e => e.LeadId);
        });
        modelBuilder.Entity<AiConfiguration>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<PromptTemplate>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId || e.IsSystem);
        modelBuilder.Entity<AirtableConfiguration>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AirtableSyncJob>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AirtableSyncLog>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AirtableRecordLink>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);

        // Automation
        modelBuilder.Entity<AutomationWorkflow>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AutomationTrigger>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AutomationAction>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AutomationCondition>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AutomationVariable>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AutomationSchedule>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AutomationExecution>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AutomationExecutionLog>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AutomationExecutionError>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AutomationQueueItem>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AutomationTemplate>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId || e.IsBuiltIn);
        modelBuilder.Entity<AutomationFolder>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AutomationVersion>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);

        // Analytics
        modelBuilder.Entity<AnalyticsDashboard>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AnalyticsWidget>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AnalyticsReport>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AnalyticsScheduledReport>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
        modelBuilder.Entity<AnalyticsForecast>()
            .HasQueryFilter(e => !_tenant.OrganizationId.HasValue || e.OrganizationId == _tenant.OrganizationId);
    }
}
