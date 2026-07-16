namespace OreoLeads.Application.Common.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserEmail { get; }
    string? UserName { get; }
    Guid? OrganizationId { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
