using AiClinic.Application.Services;
using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;

namespace AiClinic.Infrastructure.Services;

public class DoctorAssignmentService : IDoctorAssignmentService
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IConversationRepository _conversationRepository;

    public DoctorAssignmentService(
        IDoctorRepository doctorRepository,
        IConversationRepository conversationRepository)
    {
        _doctorRepository = doctorRepository;
        _conversationRepository = conversationRepository;
    }

    public async Task<Doctor?> FindBestAvailableDoctorAsync(string? specialization = null)
    {
        // TODO: Implement when Doctor entity has Specialization property
        var availableDoctors = await _doctorRepository.GetAvailableDoctorsAsync();
        return availableDoctors.FirstOrDefault();
        
        /* Original implementation - requires additional Doctor properties
        if (!availableDoctors.Any())
            return null;

        // Filter by specialization if provided
        if (!string.IsNullOrEmpty(specialization))
        {
            availableDoctors = availableDoctors
                .Where(d => d.Specialization.Equals(specialization, StringComparison.OrdinalIgnoreCase));
        }

        // Score and rank doctors
        var scoredDoctors = availableDoctors.Select(doctor => new
        {
            Doctor = doctor,
            Score = CalculateDoctorScore(doctor)
        })
        .OrderByDescending(x => x.Score)
        .ToList();

        return scoredDoctors.FirstOrDefault()?.Doctor;
        */
    }

    public async Task<bool> AssignDoctorToConversationAsync(Guid conversationId, Guid doctorId)
    {
        var conversation = await _conversationRepository.GetByIdAsync(conversationId);
        if (conversation == null)
            return false;

        conversation.AssignedDoctorId = doctorId;
        // conversation.Type = ConversationType.Doctor; // TODO: Add Type property to Conversation
        await _conversationRepository.UpdateAsync(conversation);

        // TODO: Implement when Doctor entity has ActiveConversations property
        /*
        var doctor = await _doctorRepository.GetByIdAsync(doctorId);
        if (doctor != null)
        {
            doctor.ActiveConversations++;
            await _doctorRepository.UpdateAsync(doctor);
        }
        */

        return true;
    }

    public async Task<bool> UpdateDoctorAvailabilityAsync(Guid doctorId, DoctorStatus status)
    {
        // TODO: Implement when Doctor entity has Status property
        /*
        var doctor = await _doctorRepository.GetByIdAsync(doctorId);
        if (doctor == null)
            return false;

        doctor.Status = status;
        await _doctorRepository.UpdateAsync(doctor);
        */
        return await Task.FromResult(true);
    }

    private double CalculateDoctorScore(Doctor doctor)
    {
        // TODO: Implement when Doctor entity has Rating, ActiveConversations, TotalConsultations properties
        return 0;
        
        /* Original implementation
        // Scoring algorithm based on multiple factors
        double score = 0;

        // Rating weight (40%)
        score += (double)doctor.Rating * 8;

        // Workload weight (30%) - prefer doctors with fewer active conversations
        score += Math.Max(0, 30 - (doctor.ActiveConversations * 5));

        // Experience weight (30%) - based on total consultations
        score += Math.Min(30, doctor.TotalConsultations * 0.1);

        return score;
        */
    }
}
