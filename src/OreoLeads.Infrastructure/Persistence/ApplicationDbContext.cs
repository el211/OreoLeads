using Microsoft.EntityFrameworkCore;
using OreoLeads.Application.Common.Interfaces;
using OreoLeads.Domain.Entities;

namespace OreoLeads.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
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
    public DbSet<BrevoConfiguration> BrevoConfigurations => Set<BrevoConfiguration>();
    public DbSet<EmailSendJob> EmailSendJobs => Set<EmailSendJob>();
    public DbSet<EmailEvent> EmailEvents => Set<EmailEvent>();
    public DbSet<UnsubscribeRecord> UnsubscribeRecords => Set<UnsubscribeRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
