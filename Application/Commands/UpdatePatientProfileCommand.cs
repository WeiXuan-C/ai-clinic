using AiClinic.Application.DTOs;
using AiClinic.Core.Interfaces;

namespace AiClinic.Application.Commands;

/// <summary>
/// Command Pattern - Encapsulates patient profile update request
/// </summary>
public class UpdatePatientProfileCommand : ICommand<PatientProfileDto>
{
    private readonly IPatientProfileRepository _repository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly Guid _userId;
    private readonly UpdatePatientProfileRequest _request;
    private readonly string? _ipAddress;
    private readonly string? _userAgent;

    public UpdatePatientProfileCommand(
        IPatientProfileRepository repository,
        IActivityLogRepository activityLogRepository,
        Guid userId,
        UpdatePatientProfileRequest request,
        string? ipAddress = null,
        string? userAgent = null)
    {
        _repository = repository;
        _activityLogRepository = activityLogRepository;
        _userId = userId;
        _request = request;
        _ipAddress = ipAddress;
        _userAgent = userAgent;
    }

    public async Task<PatientProfileDto> ExecuteAsync()
    {
        var profile = await _repository.GetByUserIdAsync(_userId);
        if (profile == null)
        {
            throw new InvalidOperationException("Patient profile not found");
        }

        var changedFields = new Dictionary<string, object>();

        if (_request.FullName != null && _request.FullName != profile.FullName)
        {
            changedFields["full_name"] = new { old = profile.FullName, @new = _request.FullName };
            profile.FullName = _request.FullName;
        }
        if (_request.DateOfBirth != null && _request.DateOfBirth != profile.DateOfBirth)
        {
            changedFields["date_of_birth"] = new { old = profile.DateOfBirth, @new = _request.DateOfBirth };
            profile.DateOfBirth = _request.DateOfBirth;
        }
        if (_request.Gender != null && _request.Gender != profile.Gender)
        {
            changedFields["gender"] = new { old = profile.Gender, @new = _request.Gender };
            profile.Gender = _request.Gender;
        }
        if (_request.Address != null && _request.Address != profile.Address)
        {
            changedFields["address"] = new { old = profile.Address, @new = _request.Address };
            profile.Address = _request.Address;
        }
        if (_request.EmergencyContactName != null && _request.EmergencyContactName != profile.EmergencyContactName)
        {
            changedFields["emergency_contact_name"] = new { old = profile.EmergencyContactName, @new = _request.EmergencyContactName };
            profile.EmergencyContactName = _request.EmergencyContactName;
        }
        if (_request.EmergencyContactPhone != null && _request.EmergencyContactPhone != profile.EmergencyContactPhone)
        {
            changedFields["emergency_contact_phone"] = new { old = profile.EmergencyContactPhone, @new = _request.EmergencyContactPhone };
            profile.EmergencyContactPhone = _request.EmergencyContactPhone;
        }
        if (_request.BloodType != null && _request.BloodType != profile.BloodType)
        {
            changedFields["blood_type"] = new { old = profile.BloodType, @new = _request.BloodType };
            profile.BloodType = _request.BloodType;
        }
        if (_request.Allergies != null)
        {
            changedFields["allergies"] = new { old = profile.Allergies, @new = _request.Allergies };
            profile.Allergies = _request.Allergies;
        }
        if (_request.ChronicConditions != null)
        {
            changedFields["chronic_conditions"] = new { old = profile.ChronicConditions, @new = _request.ChronicConditions };
            profile.ChronicConditions = _request.ChronicConditions;
        }
        if (_request.CurrentMedications != null)
        {
            changedFields["current_medications"] = new { old = profile.CurrentMedications, @new = _request.CurrentMedications };
            profile.CurrentMedications = _request.CurrentMedications;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        profile = await _repository.UpdateAsync(profile);

        // Log activity
        await _activityLogRepository.LogActivityAsync(
            userId: _userId,
            action: "UPDATE_PATIENT_PROFILE",
            entityType: "patient_profile",
            entityId: profile.Id,
            ipAddress: _ipAddress,
            userAgent: _userAgent,
            details: new Dictionary<string, object>
            {
                { "profile_id", profile.Id },
                { "changed_fields", changedFields }
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
