using AiClinic.Application.Commands;
using AiClinic.Application.DTOs;
using AiClinic.Presentation.State;

namespace AiClinic.Presentation.Controllers;

// Adapter Pattern - Adapts application services to presentation layer
public class AuthController
{
    private readonly ICommandHandler<SendOtpCommand, bool> _sendOtpHandler;
    private readonly ICommandHandler<VerifyOtpCommand, AuthResponse> _verifyOtpHandler;
    private readonly AppState _appState;

    public AuthController(
        ICommandHandler<SendOtpCommand, bool> sendOtpHandler,
        ICommandHandler<VerifyOtpCommand, AuthResponse> verifyOtpHandler,
        AppState appState)
    {
        _sendOtpHandler = sendOtpHandler;
        _verifyOtpHandler = verifyOtpHandler;
        _appState = appState;
    }

    public async Task<bool> RequestOtpAsync(string email)
    {
        var command = new SendOtpCommand { Email = email };
        return await _sendOtpHandler.HandleAsync(command);
    }

    public async Task<AuthResponse> VerifyOtpAsync(string email, string code)
    {
        var command = new VerifyOtpCommand { Email = email, Code = code };
        var response = await _verifyOtpHandler.HandleAsync(command);

        if (response.Success && response.Token != null && response.User != null)
        {
            _appState.SetAuthData(response.Token, response.User);
        }

        return response;
    }

    public void Logout()
    {
        _appState.ClearAuthData();
    }

    public bool IsAuthenticated()
    {
        return _appState.IsAuthenticated;
    }

    public UserDto? GetCurrentUser()
    {
        return _appState.CurrentUser;
    }
}
