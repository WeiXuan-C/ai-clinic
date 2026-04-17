using AiClinic.Application.DTOs;

namespace AiClinic.Application.Services;

public interface IPatientService
{
    // CRUD Operations
    Task<PatientProfileDto> CreateProfileAsync(CreatePatientProfileRequest request, string? ipAddress = null, string? userAgent = null);
    Task<PatientProfileDto?> GetProfileAsync(Guid userId);
    Task<PatientProfileDto?> GetProfileByIdAsync(Guid profileId);
    Task<PatientProfileDto> UpdateProfileAsync(Guid userId, UpdatePatientProfileRequest request, string? ipAddress = null, string? userAgent = null);
    Task<bool> DeleteProfileAsync(Guid userId, string? ipAddress = null, string? userAgent = null);
    
    // Conversation Management
    Task<IEnumerable<ConversationDto>> GetConversationsAsync(Guid patientId);
    Task<ConversationDto?> GetActiveConversationAsync(Guid patientId);
    
    // Activity Logs
    Task<IEnumerable<ActivityLogDto>> GetActivityLogsAsync(Guid userId, int limit = 50);
    
    // Utility
    Task<bool> CheckEmailExistsAsync(string email);
}
