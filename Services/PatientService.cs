using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;

namespace AiClinic.Services;

/// <summary>
/// Patient Service - Business Logic Layer
/// Handles patient profile operations, settings, and support tickets
/// </summary>
public class PatientService
{
    private readonly IPatientProfileRepository _patientProfileRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISupportTicketRepository _supportTicketRepository;

    public PatientService(
        IPatientProfileRepository patientProfileRepository,
        IUserRepository userRepository,
        ISupportTicketRepository supportTicketRepository)
    {
        _patientProfileRepository = patientProfileRepository;
        _userRepository = userRepository;
        _supportTicketRepository = supportTicketRepository;
    }

    /// <summary>
    /// Gets patient profile by user ID
    /// </summary>
    public async Task<PatientProfile?> GetProfileAsync(Guid userId)
    {
        return await _patientProfileRepository.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Creates a new patient profile
    /// </summary>
    public async Task<PatientProfile> CreateProfileAsync(Guid userId, string fullName)
    {
        var profile = PatientProfile.Create(userId, fullName);
        return await _patientProfileRepository.AddAsync(profile);
    }

    /// <summary>
    /// Updates patient profile
    /// </summary>
    public async Task<PatientProfile> UpdateProfileAsync(PatientProfile profile)
    {
        return await _patientProfileRepository.UpdateAsync(profile);
    }

    /// <summary>
    /// Checks if email exists
    /// </summary>
    public async Task<bool> CheckEmailExistsAsync(string email)
    {
        return await _userRepository.ExistsAsync(email);
    }

    /// <summary>
    /// Gets user settings
    /// </summary>
    public async Task<User?> GetUserSettingsAsync(Guid userId)
    {
        return await _userRepository.GetByIdAsync(userId);
    }

    /// <summary>
    /// Updates user settings
    /// </summary>
    public async Task<User> UpdateUserSettingsAsync(User user)
    {
        return await _userRepository.UpdateAsync(user);
    }

    /// <summary>
    /// Creates a support ticket
    /// </summary>
    public async Task<SupportTicket> CreateSupportTicketAsync(SupportTicket ticket)
    {
        return await _supportTicketRepository.AddAsync(ticket);
    }

    /// <summary>
    /// Gets all support tickets for a user
    /// </summary>
    public async Task<IEnumerable<SupportTicket>> GetUserSupportTicketsAsync(Guid userId)
    {
        return await _supportTicketRepository.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Gets a specific support ticket
    /// </summary>
    public async Task<SupportTicket?> GetSupportTicketAsync(Guid ticketId)
    {
        return await _supportTicketRepository.GetByIdAsync(ticketId);
    }
}
