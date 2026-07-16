namespace OreoLeads.Application.Features.Auth.DTOs;

public record RegisterDto(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? OrganizationName = null);

public record LoginDto(
    string Email,
    string Password);

public record AuthResponseDto(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);

public record UserDto(
    string Id,
    string Email,
    string FirstName,
    string LastName,
    string FullName,
    Guid? OrganizationId,
    string? OrganizationName,
    IEnumerable<string> Roles);

public record RefreshTokenDto(string Token);

public record ChangePasswordDto(
    string CurrentPassword,
    string NewPassword);

public record ForgotPasswordDto(string Email);

public record ResetPasswordDto(
    string Email,
    string Token,
    string NewPassword);
