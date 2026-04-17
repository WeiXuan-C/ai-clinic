using AiClinic.Core.Interfaces;

namespace AiClinic.Application.Commands;

/// <summary>
/// Command Pattern - Encapsulates patient profile deletion request
/// </summary>
public class DeletePatientProfileCommand : ICommand<bool>
{
    private readonly IPatientProfileRepository _repository;
    private readonly IActivityLogRepository _activityLogRepository;
    private readonly Guid _userId;
    private readonly string? _ipAddress;
    private readonly string? _userAgent;

    public DeletePatientProfileCommand(
        IPatientProfileRepository repository,
        IActivityLogRepository activityLogRepository,
        Guid userId,
        string? ipAddress = null,
        string? userAgent = null)
    {
        _repository = repository;
        _activityLogRepository = activityLogRepository;
        _userId = userId;
        _ipAddress = ipAddress;
        _userAgent = userAgent;
    }

    public async Task<bool> ExecuteAsync()
    {
        var profile = await _repository.GetByUserIdAsync(_userId);
        if (profile == null)
        {
            throw new InvalidOperationException("Patient profile not found");
        }

        var profileId = profile.Id;
        var fullName = profile.FullName;
        
        var result = await _repository.DeleteAsync(profile.Id);

        // Log activity
        await _activityLogRepository.LogActivityAsync(
            userId: _userId,
            action: "DELETE_PATIENT_PROFILE",
            entityType: "patient_profile",
            entityId: profileId,
            ipAddress: _ipAddress,
            userAgent: _userAgent,
            details: new Dictionary<string, object>
            {
                { "profile_id", profileId },
                { "full_name", fullName ?? "N/A" }
            }
        );

        return result;
    }
}
