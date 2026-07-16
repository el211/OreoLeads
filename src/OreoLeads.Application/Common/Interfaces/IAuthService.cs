using OreoLeads.Application.Features.Auth.DTOs;

namespace OreoLeads.Application.Common.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto, string ipAddress, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginDto dto, string ipAddress, CancellationToken ct = default);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken, string ipAddress, CancellationToken ct = default);
    Task RevokeTokenAsync(string refreshToken, string ipAddress, CancellationToken ct = default);
    Task LogoutAsync(string userId, CancellationToken ct = default);
    Task ChangePasswordAsync(string userId, ChangePasswordDto dto, CancellationToken ct = default);
    Task ForgotPasswordAsync(ForgotPasswordDto dto, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordDto dto, CancellationToken ct = default);
}
