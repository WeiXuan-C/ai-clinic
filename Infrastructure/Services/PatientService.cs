using AiClinic.Application.DTOs;
using AiClinic.Application.Services;
using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;

namespace AiClinic.Infrastructure.Services;

public class PatientService : IPatientService
{
    private readonly IPatientProfileRepository _patientProfileRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActivityLogRepository _activityLogRepository;

    public PatientService(
        IPatientProfileRepository patientProfileRepository,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository,
        IUserRepository userRepository,
        IActivityLogRepository activityLogRepository)
    {
        _patientProfileRepository = patientProfileRepository;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _activityLogRepository = activityLogRepository;
    }

    public async Task<PatientProfileDto> CreateProfileAsync(CreatePatientProfileRequest request, 
        string? ipAddress = null, string? userAgent = null)
    {
        // Check if profile already exists
        var existingProfile = await _patientProfileRepository.GetByUserIdAsync(request.UserId);
        if (existingProfile != null)
        {
            throw new InvalidOperationException("Patient profile already exists for this user");
        }

        var profile = new PatientProfile
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            FullName = request.FullName,
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender,
            Address = request.Address,
            EmergencyContactName = request.EmergencyContactName,
            EmergencyContactPhone = request.EmergencyContactPhone,
            BloodType = request.BloodType,
            Allergies = request.Allergies,
            ChronicConditions = request.ChronicConditions,
            CurrentMedications = request.CurrentMedications,
            CreatedAt = DateTime.UtcNow
        };

        profile = await _patientProfileRepository.AddAsync(profile);

        // Log activity
        await _activityLogRepository.LogActivityAsync(
            userId: request.UserId,
            action: "CREATE_PATIENT_PROFILE",
            entityType: "patient_profile",
            entityId: profile.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            details: new Dictionary<string, object>
            {
                { "full_name", request.FullName ?? "N/A" },
                { "profile_id", profile.Id }
            }
        );

        return MapToDto(profile);
    }

    public async Task<PatientProfileDto?> GetProfileAsync(Guid userId)
    {
        var profile = await _patientProfileRepository.GetByUserIdAsync(userId);
        if (profile == null)
            return null;

        return MapToDto(profile);
    }

    public async Task<PatientProfileDto?> GetProfileByIdAsync(Guid profileId)
    {
        var profile = await _patientProfileRepository.GetByIdAsync(profileId);
        if (profile == null)
            return null;

        return MapToDto(profile);
    }

    public async Task<PatientProfileDto> UpdateProfileAsync(Guid userId, UpdatePatientProfileRequest request, 
        string? ipAddress = null, string? userAgent = null)
    {
        var profile = await _patientProfileRepository.GetByUserIdAsync(userId);
        if (profile == null)
        {
            throw new InvalidOperationException("Patient profile not found");
        }

        var changedFields = new Dictionary<string, object>();

        if (request.FullName != null && request.FullName != profile.FullName)
        {
            changedFields["full_name"] = new { old = profile.FullName, @new = request.FullName };
            profile.FullName = request.FullName;
        }
        if (request.DateOfBirth != null && request.DateOfBirth != profile.DateOfBirth)
        {
            changedFields["date_of_birth"] = new { old = profile.DateOfBirth, @new = request.DateOfBirth };
            profile.DateOfBirth = request.DateOfBirth;
        }
        if (request.Gender != null && request.Gender != profile.Gender)
        {
            changedFields["gender"] = new { old = profile.Gender, @new = request.Gender };
            profile.Gender = request.Gender;
        }
        if (request.Address != null && request.Address != profile.Address)
        {
            changedFields["address"] = new { old = profile.Address, @new = request.Address };
            profile.Address = request.Address;
        }
        if (request.EmergencyContactName != null && request.EmergencyContactName != profile.EmergencyContactName)
        {
            changedFields["emergency_contact_name"] = new { old = profile.EmergencyContactName, @new = request.EmergencyContactName };
            profile.EmergencyContactName = request.EmergencyContactName;
        }
        if (request.EmergencyContactPhone != null && request.EmergencyContactPhone != profile.EmergencyContactPhone)
        {
            changedFields["emergency_contact_phone"] = new { old = profile.EmergencyContactPhone, @new = request.EmergencyContactPhone };
            profile.EmergencyContactPhone = request.EmergencyContactPhone;
        }
        if (request.BloodType != null && request.BloodType != profile.BloodType)
        {
            changedFields["blood_type"] = new { old = profile.BloodType, @new = request.BloodType };
            profile.BloodType = request.BloodType;
        }
        if (request.Allergies != null)
        {
            changedFields["allergies"] = new { old = profile.Allergies, @new = request.Allergies };
            profile.Allergies = request.Allergies;
        }
        if (request.ChronicConditions != null)
        {
            changedFields["chronic_conditions"] = new { old = profile.ChronicConditions, @new = request.ChronicConditions };
            profile.ChronicConditions = request.ChronicConditions;
        }
        if (request.CurrentMedications != null)
        {
            changedFields["current_medications"] = new { old = profile.CurrentMedications, @new = request.CurrentMedications };
            profile.CurrentMedications = request.CurrentMedications;
        }

        profile.UpdatedAt = DateTime.UtcNow;
        profile = await _patientProfileRepository.UpdateAsync(profile);

        // Log activity
        await _activityLogRepository.LogActivityAsync(
            userId: userId,
            action: "UPDATE_PATIENT_PROFILE",
            entityType: "patient_profile",
            entityId: profile.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            details: new Dictionary<string, object>
            {
                { "profile_id", profile.Id },
                { "changed_fields", changedFields }
            }
        );

        return MapToDto(profile);
    }

    public async Task<bool> DeleteProfileAsync(Guid userId, string? ipAddress = null, string? userAgent = null)
    {
        var profile = await _patientProfileRepository.GetByUserIdAsync(userId);
        if (profile == null)
        {
            throw new InvalidOperationException("Patient profile not found");
        }

        var profileId = profile.Id;
        var result = await _patientProfileRepository.DeleteAsync(profile.Id);

        // Log activity
        await _activityLogRepository.LogActivityAsync(
            userId: userId,
            action: "DELETE_PATIENT_PROFILE",
            entityType: "patient_profile",
            entityId: profileId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            details: new Dictionary<string, object>
            {
                { "profile_id", profileId },
                { "full_name", profile.FullName ?? "N/A" }
            }
        );

        return result;
    }

    public async Task<IEnumerable<ConversationDto>> GetConversationsAsync(Guid patientId)
    {
        var conversations = await _conversationRepository.GetByPatientIdAsync(patientId);
        var result = new List<ConversationDto>();

        foreach (var conversation in conversations)
        {
            var messages = await _messageRepository.GetByConversationIdAsync(conversation.Id);
            var messageDtos = messages.Select(m => new MessageDto(
                m.Id,
                m.Content,
                m.SenderType.ToString().ToLower(),
                m.SentAt,
                m.IsRead
            )).ToList();

            result.Add(new ConversationDto(
                conversation.Id,
                conversation.PatientId,
                conversation.AssignedDoctorId,
                conversation.Status,
                conversation.AssignedDoctorId.HasValue ? "Doctor" : "AI",
                conversation.CreatedAt,
                messageDtos
            ));
        }

        return result;
    }

    public async Task<ConversationDto?> GetActiveConversationAsync(Guid patientId)
    {
        var conversation = await _conversationRepository.GetActiveConversationAsync(patientId);
        if (conversation == null)
            return null;

        var messages = await _messageRepository.GetByConversationIdAsync(conversation.Id);
        var messageDtos = messages.Select(m => new MessageDto(
            m.Id,
            m.Content,
            m.SenderType.ToString().ToLower(),
            m.SentAt,
            m.IsRead
        )).ToList();

        return new ConversationDto(
            conversation.Id,
            conversation.PatientId,
            conversation.AssignedDoctorId,
            conversation.Status,
            conversation.AssignedDoctorId.HasValue ? "Doctor" : "AI",
            conversation.CreatedAt,
            messageDtos
        );
    }
    
    public async Task<bool> CheckEmailExistsAsync(string email)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        return user != null;
    }

    public async Task<IEnumerable<ActivityLogDto>> GetActivityLogsAsync(Guid userId, int limit = 50)
    {
        var logs = await _activityLogRepository.GetByUserIdAsync(userId, limit);
        return logs.Select(log => new ActivityLogDto(
            log.Id,
            log.UserId,
            log.Action,
            log.EntityType,
            log.EntityId,
            log.Details,
            log.CreatedAt
        ));
    }

    private static PatientProfileDto MapToDto(PatientProfile profile)
    {
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
