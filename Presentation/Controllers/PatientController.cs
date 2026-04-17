using AiClinic.Application.DTOs;
using AiClinic.Application.Services;
using Microsoft.AspNetCore.Http;

namespace AiClinic.Presentation.Controllers;

/// <summary>
/// Adapter Pattern - Adapts patient services to presentation layer
/// </summary>
public class PatientController
{
    private readonly IPatientService _patientService;
    private readonly IChatService _chatService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PatientController(
        IPatientService patientService,
        IChatService chatService,
        IHttpContextAccessor httpContextAccessor)
    {
        _patientService = patientService;
        _chatService = chatService;
        _httpContextAccessor = httpContextAccessor;
    }

    // CRUD Operations
    public async Task<PatientProfileDto> CreateProfileAsync(CreatePatientProfileRequest request)
    {
        var (ipAddress, userAgent) = GetRequestMetadata();
        return await _patientService.CreateProfileAsync(request, ipAddress, userAgent);
    }

    public async Task<PatientProfileDto?> GetProfileAsync(Guid userId)
    {
        return await _patientService.GetProfileAsync(userId);
    }

    public async Task<PatientProfileDto?> GetProfileByIdAsync(Guid profileId)
    {
        return await _patientService.GetProfileByIdAsync(profileId);
    }

    public async Task<PatientProfileDto> UpdateProfileAsync(Guid userId, UpdatePatientProfileRequest request)
    {
        var (ipAddress, userAgent) = GetRequestMetadata();
        return await _patientService.UpdateProfileAsync(userId, request, ipAddress, userAgent);
    }

    public async Task<bool> DeleteProfileAsync(Guid userId)
    {
        var (ipAddress, userAgent) = GetRequestMetadata();
        return await _patientService.DeleteProfileAsync(userId, ipAddress, userAgent);
    }

    // Conversation Management
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

    // Activity Logs
    public async Task<IEnumerable<ActivityLogDto>> GetActivityLogsAsync(Guid userId, int limit = 50)
    {
        return await _patientService.GetActivityLogsAsync(userId, limit);
    }

    // Utility
    public async Task<bool> CheckEmailExistsAsync(string email)
    {
        return await _patientService.CheckEmailExistsAsync(email);
    }

    private (string? ipAddress, string? userAgent) GetRequestMetadata()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context == null)
            return (null, null);

        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        var userAgent = context.Request.Headers["User-Agent"].ToString();

        return (ipAddress, userAgent);
    }
}
