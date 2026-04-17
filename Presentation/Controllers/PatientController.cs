using AiClinic.Application.DTOs;
using AiClinic.Application.Services;

namespace AiClinic.Presentation.Controllers;

/// <summary>
/// Adapter Pattern - Adapts patient services to presentation layer
/// </summary>
public class PatientController
{
    private readonly IPatientService _patientService;
    private readonly IChatService _chatService;

    public PatientController(
        IPatientService patientService,
        IChatService chatService)
    {
        _patientService = patientService;
        _chatService = chatService;
    }

    public async Task<PatientProfileDto?> GetProfileAsync(Guid userId)
    {
        return await _patientService.GetProfileAsync(userId);
    }

    public async Task<PatientProfileDto> UpdateProfileAsync(Guid userId, UpdatePatientProfileRequest request)
    {
        return await _patientService.UpdateProfileAsync(userId, request);
    }

    public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(Guid patientId)
    {
        return await _patientService.GetConversationsAsync(patientId);
    }

    public async Task<ConversationDto?> GetActiveConversationAsync(Guid patientId)
    {
        return await _patientService.GetActiveConversationAsync(patientId);
    }

    public async Task<ConversationDto> StartNewConversationAsync(Guid patientId)
    {
        return await _chatService.CreateConversationAsync(patientId);
    }

    public async Task<MessageDto> SendMessageAsync(SendMessageRequest request)
    {
        return await _chatService.SendMessageAsync(request);
    }
    
    public async Task<bool> CheckEmailExistsAsync(string email)
    {
        return await _patientService.CheckEmailExistsAsync(email);
    }
}
