using AiClinic.Application.DTOs;
using AiClinic.Application.Services;
using AiClinic.Core.Interfaces;

namespace AiClinic.Infrastructure.Services;

public class DoctorService : IDoctorService
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IMessageRepository _messageRepository;

    public DoctorService(
        IDoctorRepository doctorRepository,
        IConversationRepository conversationRepository,
        IMessageRepository messageRepository)
    {
        _doctorRepository = doctorRepository;
        _conversationRepository = conversationRepository;
        _messageRepository = messageRepository;
    }

    public async Task<IEnumerable<DoctorDto>> GetAllDoctorsAsync()
    {
        var doctors = await _doctorRepository.GetAllAsync();
        return doctors.Select(MapToDoctorDto);
    }

    public async Task<IEnumerable<DoctorDto>> GetAvailableDoctorsAsync()
    {
        var doctors = await _doctorRepository.GetAvailableDoctorsAsync();
        return doctors.Select(MapToDoctorDto);
    }

    public async Task<IEnumerable<DoctorDto>> GetDoctorsBySpecializationAsync(string specialization)
    {
        var doctors = await _doctorRepository.GetBySpecializationAsync(specialization);
        return doctors.Select(MapToDoctorDto);
    }

    public async Task<DoctorDto?> GetDoctorByIdAsync(Guid doctorId)
    {
        var doctor = await _doctorRepository.GetByIdAsync(doctorId);
        return doctor != null ? MapToDoctorDto(doctor) : null;
    }

    public async Task<DoctorDto?> GetDoctorByUserIdAsync(Guid userId)
    {
        var doctor = await _doctorRepository.GetByUserIdAsync(userId);
        return doctor != null ? MapToDoctorDto(doctor) : null;
    }

    public async Task UpdateDoctorStatusAsync(Guid doctorId, string status)
    {
        var doctor = await _doctorRepository.GetByIdAsync(doctorId);
        if (doctor == null)
        {
            throw new InvalidOperationException("Doctor not found");
        }

        doctor.AvailabilityStatus = status;
        doctor.UpdatedAt = DateTime.UtcNow;
        await _doctorRepository.UpdateAsync(doctor);
    }

    public async Task<IEnumerable<ConversationDto>> GetDoctorConversationsAsync(Guid doctorUserId)
    {
        var conversations = await _conversationRepository.GetByDoctorIdAsync(doctorUserId);
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
                "Doctor",
                conversation.CreatedAt,
                messageDtos
            ));
        }

        return result;
    }

    private static DoctorDto MapToDoctorDto(Core.Entities.Doctor doctor)
    {
        return new DoctorDto(
            doctor.Id,
            doctor.FullName,
            doctor.PrimarySpecialization,
            doctor.OrganizationId?.ToString() ?? "Independent",
            doctor.AvailabilityStatus,
            doctor.AverageRating,
            doctor.TotalConsultations
        );
    }
}
