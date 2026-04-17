using AiClinic.Application.DTOs;
using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;

namespace AiClinic.Application.Commands;

/// <summary>
/// Command Pattern - Encapsulates patient profile creation request
/// </summary>
public class CreatePatientProfileCommand : ICommand<PatientProfileDto>
{
    private readonly IPatientProfileRepository _repository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly CreatePatientProfileRequest _request;
    private readonly string? _ipAddress;
    private readonly string? _userAgent;

    public CreatePatientProfileCommand(
        IPatientProfileRepository repository,
        IActivityLogRepository activityLogRepository,
        CreatePatientProfileRequest request,
        string? ipAddress = null,
        string? userAgent = null)
    {
        _repository = repository;
        _activityLogRepository = activityLogRepository;
        _request = request;
        _ipAddress = ipAddress;
        _userAgent = userAgent;
    }

    public async Task<PatientProfileDto> ExecuteAsync()
    {
        // Check if profile already exists
        var existingProfile = await _repository.GetByUserIdAsync(_request.UserId);
        if (existingProfile != null)
        {
            throw new InvalidOperationException("Patient profile already exists for this user");
        }

        var profile = new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = _request.UserId,
            FullName = _request.FullName,
            DateOfBirth = _request.DateOfBirth,
            Gender = _request.Gender,
            Address = _request.Address,
            EmergencyContactName = _request.EmergencyContactName,
            EmergencyContactPhone = _request.EmergencyContactPhone,
            BloodType = _request.BloodType,
            Allergies = _request.Allergies,
            ChronicConditions = _request.ChronicConditions,
            CurrentMedications = _request.CurrentMedications,
            CreatedAt = DateTime.UtcNow
        };

        profile = await _repository.AddAsync(profile);

        // Log activity
        await _activityLogRepository.LogActivityAsync(
            userId: _request.UserId,
            action: "CREATE_PATIENT_PROFILE",
            entityType: "patient_profile",
            entityId: profile.Id,
            ipAddress: _ipAddress,
            userAgent: _userAgent,
            details: new Dictionary<string, object>
            {
                { "full_name", _request.FullName ?? "N/A" },
                { "profile_id", profile.Id }
            }
        );

        return new PatientProfileDto(
            profile.Id,
            profile.UserId,
            profile.FullName,
            profile.DateOfBirth,
            profile.Gender,
            profile.Address,
            profile.EmergencyContactName,
            profile.EmergencyContactPhone,
            profile.BloodType,
            profile.Allergies,
            profile.ChronicConditions,
            profile.CurrentMedications,
            profile.CreatedAt,
            profile.UpdatedAt
        );
    }
}
