namespace SkillHub.Application.DTOs.Auth;

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string? PhoneNumber = null
);

public record LoginRequest(
    string Email,
    string Password
);

public record AuthResponse(
    Guid UserId,
    string Token,
    string Email,
    string FullName,
    string Role,
    DateTime ExpiresAt
);
