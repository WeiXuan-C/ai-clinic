using AiClinic.Application.DTOs;

namespace AiClinic.Application.Services;

public interface IPatientService
{
    Task<PatientProfileDto?> GetProfileAsync(Guid userId);
    Task<PatientProfileDto> UpdateProfileAsync(Guid userId, UpdatePatientProfileRequest request);
    Task<IEnumerable<ConversationDto>> GetConversationsAsync(Guid patientId);
    Task<ConversationDto?> GetActiveConversationAsync(Guid patientId);
}
