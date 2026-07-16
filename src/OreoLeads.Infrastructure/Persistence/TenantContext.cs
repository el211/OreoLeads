namespace OreoLeads.Infrastructure.Persistence;

/// <summary>
/// Scoped service that holds the current tenant (organization) context.
/// Injected into ApplicationDbContext so HasQueryFilter can evaluate it at query time.
/// </summary>
public class TenantContext
{
    public Guid? OrganizationId { get; private set; }

    public void SetOrganization(Guid organizationId)
        => OrganizationId = organizationId;

    public void Clear()
        => OrganizationId = null;
}
