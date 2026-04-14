namespace AiClinic.Application.DTOs;

public record LoginRequest(string Email);

public record VerifyOtpRequest(string Email, string Code);

public record AuthResponse(
    bool Success,
    string? Token,
    string? Message,
    UserDto? User
);

public record UserDto(
    Guid Id,
    string Email,
    string? FullName,
    string Role
);
