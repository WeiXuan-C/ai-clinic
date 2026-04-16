using AiClinic.Application.DTOs;
using AiClinic.Application.Services;
using AiClinic.Core.Interfaces;

namespace AiClinic.Infrastructure.Services;

public class PatientService : IPatientService
{
    private readonly IPatientProfileRepository _patientProfileRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;

    public PatientService(
        IPatientProfileRepository patientProfileRepository,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository)
    {
        _patientProfileRepository = patientProfileRepository;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
    }

    public async Task<PatientProfileDto?> GetProfileAsync(Guid userId)
    {
        var profile = await _patientProfileRepository.GetByUserIdAsync(userId);
        if (profile == null)
            return null;

        return new PatientProfileDto(
            profile.Id,
            profile.UserId,
            profile.FullName,
            profile.DateOfBirth,
            profile.Gender,
            profile.Address,
            profile.BloodType,
            profile.Allergies,
            profile.ChronicConditions,
            profile.CurrentMedications
        );
    }

    public async Task<PatientProfileDto> UpdateProfileAsync(Guid userId, UpdatePatientProfileRequest request)
    {
        var profile = await _patientProfileRepository.GetByUserIdAsync(userId);
        if (profile == null)
        {
            throw new InvalidOperationException("Patient profile not found");
        }

        profile.FullName = request.FullName ?? profile.FullName;
        profile.DateOfBirth = request.DateOfBirth ?? profile.DateOfBirth;
        profile.Gender = request.Gender ?? profile.Gender;
        profile.Address = request.Address ?? profile.Address;
        profile.EmergencyContactName = request.EmergencyContactName ?? profile.EmergencyContactName;
        profile.EmergencyContactPhone = request.EmergencyContactPhone ?? profile.EmergencyContactPhone;
        profile.BloodType = request.BloodType ?? profile.BloodType;
        profile.Allergies = request.Allergies ?? profile.Allergies;
        profile.ChronicConditions = request.ChronicConditions ?? profile.ChronicConditions;
        profile.CurrentMedications = request.CurrentMedications ?? profile.CurrentMedications;
        profile.UpdatedAt = DateTime.UtcNow;

        profile = await _patientProfileRepository.UpdateAsync(profile);

        return new PatientProfileDto(
            profile.Id,
            profile.UserId,
            profile.FullName,
            profile.DateOfBirth,
            profile.Gender,
            profile.Address,
            profile.BloodType,
            profile.Allergies,
            profile.ChronicConditions,
            profile.CurrentMedications
        );
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
                m.SenderType,
                m.CreatedAt,
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
            m.SenderType,
            m.CreatedAt,
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
}
