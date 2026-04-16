using AiClinic.Application.DTOs;
using AiClinic.Application.Services;

namespace AiClinic.Presentation.Controllers;

/// <summary>
/// Adapter Pattern - Adapts doctor services to presentation layer
/// </summary>
public class DoctorController
{
    private readonly IDoctorService _doctorService;

    public DoctorController(IDoctorService doctorService)
    {
        _doctorService = doctorService;
    }

    public async Task<IEnumerable<DoctorDto>> GetAllDoctorsAsync()
    {
        return await _doctorService.GetAllDoctorsAsync();
    }

    public async Task<IEnumerable<DoctorDto>> GetAvailableDoctorsAsync()
    {
        return await _doctorService.GetAvailableDoctorsAsync();
    }

    public async Task<IEnumerable<DoctorDto>> GetDoctorsBySpecializationAsync(string specialization)
    {
        return await _doctorService.GetDoctorsBySpecializationAsync(specialization);
    }

    public async Task<DoctorDto?> GetDoctorByIdAsync(Guid doctorId)
    {
        return await _doctorService.GetDoctorByIdAsync(doctorId);
    }

    public async Task<DoctorDto?> GetDoctorByUserIdAsync(Guid userId)
    {
        return await _doctorService.GetDoctorByUserIdAsync(userId);
    }

    public async Task UpdateDoctorStatusAsync(Guid doctorId, string status)
    {
        await _doctorService.UpdateDoctorStatusAsync(doctorId, status);
    }

    public async Task<IEnumerable<ConversationDto>> GetDoctorConversationsAsync(Guid doctorUserId)
    {
        return await _doctorService.GetDoctorConversationsAsync(doctorUserId);
    }
}
