using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;

namespace AiClinic.Services;

/// <summary>
/// Patient Service - Business Logic Layer
/// Handles patient profile operations
/// </summary>
public class PatientService
{
    private readonly IPatientProfileRepository _patientProfileRepository;
    private readonly IUserRepository _userRepository;

    public PatientService(
        IPatientProfileRepository patientProfileRepository,
        IUserRepository userRepository)
    {
        _patientProfileRepository = patientProfileRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// Gets patient profile by user ID
    /// </summary>
    public async Task<PatientProfile?> GetProfileAsync(Guid userId)
    {
        return await _patientProfileRepository.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Updates patient profile
    /// </summary>
    public async Task<PatientProfile> UpdateProfileAsync(Guid userId, PatientProfile profile)
    {
        profile.UserId = userId;
        return await _patientProfileRepository.UpdateAsync(profile);
    }

    /// <summary>
    /// Checks if email exists
    /// </summary>
    public async Task<bool> CheckEmailExistsAsync(string email)
    {
        return await _userRepository.ExistsAsync(email);
    }
}
