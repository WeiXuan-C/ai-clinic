using AiClinic.Application.DTOs;

namespace AiClinic.Application.Services;

public interface IDoctorService
{
    Task<IEnumerable<DoctorDto>> GetAllDoctorsAsync();
    Task<IEnumerable<DoctorDto>> GetAvailableDoctorsAsync();
    Task<IEnumerable<DoctorDto>> GetDoctorsBySpecializationAsync(string specialization);
    Task<DoctorDto?> GetDoctorByIdAsync(Guid doctorId);
    Task<DoctorDto?> GetDoctorByUserIdAsync(Guid userId);
    Task UpdateDoctorStatusAsync(Guid doctorId, string status);
    Task<IEnumerable<ConversationDto>> GetDoctorConversationsAsync(Guid doctorUserId);
}
