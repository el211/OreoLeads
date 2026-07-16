using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OreoLeads.Application.Common.Interfaces;

namespace OreoLeads.Infrastructure.Identity;

internal sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public string? UserId
        => Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserEmail
        => Principal?.FindFirstValue(ClaimTypes.Email);

    public string? UserName
        => Principal?.FindFirstValue(ClaimTypes.Name);

    public Guid? OrganizationId
    {
        get
        {
            var value = Principal?.FindFirstValue("organization_id");
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated
        => Principal?.Identity?.IsAuthenticated == true;

    public bool IsInRole(string role)
        => Principal?.IsInRole(role) == true;
}
