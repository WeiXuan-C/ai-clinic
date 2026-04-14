using AiClinic.Application.DTOs;
using AiClinic.Application.Services;

namespace AiClinic.Application.Commands;

public class VerifyOtpCommand : ICommand<AuthResponse>
{
    public string Email { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    public Task<AuthResponse> ExecuteAsync()
    {
        throw new NotImplementedException("Use handler");
    }
}

public class VerifyOtpCommandHandler : ICommandHandler<VerifyOtpCommand, AuthResponse>
{
    private readonly IAuthService _authService;

    public VerifyOtpCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<AuthResponse> HandleAsync(VerifyOtpCommand command)
    {
        return await _authService.VerifyOtpAsync(command.Email, command.Code);
    }
}
