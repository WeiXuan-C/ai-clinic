using AiClinic.Application.Services;

namespace AiClinic.Application.Commands;

public class SendOtpCommand : ICommand<bool>
{
    public string Email { get; set; } = string.Empty;

    public Task<bool> ExecuteAsync()
    {
        throw new NotImplementedException("Use handler");
    }
}

public class SendOtpCommandHandler : ICommandHandler<SendOtpCommand, bool>
{
    private readonly IAuthService _authService;

    public SendOtpCommandHandler(IAuthService authService)
    {
        _authService = authService;
    }

    public async Task<bool> HandleAsync(SendOtpCommand command)
    {
        return await _authService.SendOtpAsync(command.Email);
    }
}
