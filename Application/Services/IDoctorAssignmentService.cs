using AiClinic.Core.Entities;

namespace AiClinic.Application.Services;

public interface IDoctorAssignmentService
{
    Task<Doctor?> FindBestAvailableDoctorAsync(string? specialization = null);
    Task<bool> AssignDoctorToConversationAsync(Guid conversationId, Guid doctorId);
    Task<bool> UpdateDoctorAvailabilityAsync(Guid doctorId, DoctorStatus status);
}
