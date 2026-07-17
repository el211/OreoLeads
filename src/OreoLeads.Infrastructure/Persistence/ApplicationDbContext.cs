using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;
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
    public DbSet<EmailEvent> EmailEvents => Set<EmailEvent>();
    public DbSet<UnsubscribeRecord> UnsubscribeRecords => Set<UnsubscribeRecord>();
    public DbSet<AirtableConfiguration> AirtableConfigurations => Set<AirtableConfiguration>();
    public DbSet<AirtableFieldMapping> AirtableFieldMappings => Set<AirtableFieldMapping>();
    public DbSet<AirtableSyncJob> AirtableSyncJobs => Set<AirtableSyncJob>();
    public DbSet<AirtableSyncLog> AirtableSyncLogs => Set<AirtableSyncLog>();
    public DbSet<AirtableRecordLink> AirtableRecordLinks => Set<AirtableRecordLink>();

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
    }
}
