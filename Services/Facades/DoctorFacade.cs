using ai_clinic.Models;

namespace ai_clinic.Services.Facades;

/// <summary>
/// Facade Pattern: Provides a unified interface for doctor-related operations
/// Coordinates multiple subsystems: DoctorProfile, Conversation, Consultation, Prescription
/// </summary>
public class DoctorFacade
{
    private readonly DoctorProfileService _doctorProfileService;
    private readonly ConversationService _conversationService;
    private readonly ConsultationService _consultationService;
    private readonly PrescriptionService _prescriptionService;
    private readonly MedicalRecordService _medicalRecordService;
    private readonly ActivityLogService _activityLogService;
    private readonly StatisticsService _statisticsService;

    public DoctorFacade(
        DoctorProfileService doctorProfileService,
        ConversationService conversationService,
        ConsultationService consultationService,
        PrescriptionService prescriptionService,
        MedicalRecordService medicalRecordService,
        ActivityLogService activityLogService,
        StatisticsService statisticsService)
    {
        _doctorProfileService = doctorProfileService;
        _conversationService = conversationService;
        _consultationService = consultationService;
        _prescriptionService = prescriptionService;
        _medicalRecordService = medicalRecordService;
        _activityLogService = activityLogService;
        _statisticsService = statisticsService;
    }

    /// <summary>
    /// Get complete doctor dashboard data
    /// Coordinates multiple services to gather all doctor information
    /// </summary>
    public async Task<DoctorDashboardData> GetDashboardDataAsync(Guid userId)
    {
        var profileTask = _doctorProfileService.GetByUserIdAsync(userId);
        var conversationsTask = _conversationService.GetByDoctorIdAsync(userId);
        var performanceTask = _statisticsService.GetDoctorPerformanceAsync(userId);

        await Task.WhenAll(profileTask, conversationsTask, performanceTask);

        await _activityLogService.LogActivityAsync(userId, "ViewDoctorDashboard");

        return new DoctorDashboardData
        {
            Profile = await profileTask,
            ActiveConversations = (await conversationsTask)
                .Where(c => c.Status == ConversationStatus.Active)
                .ToList(),
            PerformanceStats = await performanceTask
        };
    }

    /// <summary>
    /// Complete a consultation with all related records
    /// Coordinates consultation note, prescription, and medical record creation
    /// </summary>
    public async Task<ConsultationResult> CompleteConsultationAsync(
        Guid conversationId,
        Guid doctorId,
        Guid patientId,
        string diagnosis,
        string treatmentPlan,
        string? prescriptionDetails = null)
    {
        // Create consultation note
        var consultationNote = new ConsultationNote
        {
            ConversationId = conversationId,
            DoctorId = doctorId,
            PatientId = patientId,
            Diagnosis = diagnosis,
            TreatmentPlan = treatmentPlan,
            FollowUpInstructions = !string.IsNullOrEmpty(prescriptionDetails) ? "Follow prescription instructions" : null
        };

        consultationNote = await _consultationService.CreateAsync(consultationNote);

        // Create prescription if provided
        Prescription? prescription = null;
        if (!string.IsNullOrEmpty(prescriptionDetails))
        {
            prescription = new Prescription
            {
                ConsultationNoteId = consultationNote.Id,
                PatientId = patientId,
                DoctorId = doctorId,
                MedicationName = prescriptionDetails,
                Dosage = "As prescribed",
                Frequency = "As prescribed",
                Instructions = prescriptionDetails
            };

            prescription = await _prescriptionService.CreateAsync(prescription);
        }

        // Create medical record
        var medicalRecord = new MedicalRecord
        {
            PatientId = patientId,
            ConversationId = conversationId,
            CreatedByDoctorId = doctorId,
            RecordType = "Consultation",
            Title = $"Consultation - {diagnosis}",
            Content = $"Diagnosis: {diagnosis}\nTreatment Plan: {treatmentPlan}"
        };

        medicalRecord = await _medicalRecordService.CreateAsync(medicalRecord);

        // Update conversation status
        await _conversationService.UpdateStatusAsync(conversationId, ConversationStatus.Closed);

        // Log activity
        await _activityLogService.LogActivityAsync(
            doctorId,
            "CompleteConsultation",
            $"Conversation ID: {conversationId}");

        return new ConsultationResult
        {
            ConsultationNote = consultationNote,
            Prescription = prescription,
            MedicalRecord = medicalRecord
        };
    }

    /// <summary>
    /// Accept a patient consultation
    /// Updates conversation and doctor availability
    /// </summary>
    public async Task AcceptConsultationAsync(Guid conversationId, Guid doctorId)
    {
        await _conversationService.AssignDoctorAsync(conversationId, doctorId);
        
        await _activityLogService.LogActivityAsync(
            doctorId,
            "AcceptConsultation",
            $"Conversation ID: {conversationId}");
    }

    /// <summary>
    /// Get doctor's workload summary
    /// </summary>
    public async Task<DoctorWorkloadSummary> GetWorkloadSummaryAsync(Guid userId)
    {
        var conversations = await _conversationService.GetByDoctorIdAsync(userId);
        var performance = await _statisticsService.GetDoctorPerformanceAsync(userId);

        return new DoctorWorkloadSummary
        {
            ActiveConsultations = conversations.Count(c => c.Status == ConversationStatus.Active),
            PendingConsultations = conversations.Count(c => c.Status == ConversationStatus.Deactive),
            CompletedToday = conversations.Count(c => 
                c.Status == ConversationStatus.Closed && 
                c.UpdatedAt.Date == DateTime.UtcNow.Date),
            TotalCompleted = performance.CompletedConsultations,
            AverageRating = performance.AverageRating
        };
    }

    /// <summary>
    /// Get all active doctors
    /// For public doctor directory
    /// </summary>
    public async Task<List<DoctorProfile>> GetAllActiveDoctorsAsync()
    {
        return await _doctorProfileService.GetAllAsync();
    }

    /// <summary>
    /// Get doctor's conversations list
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<List<Conversation>> GetDoctorConversationsAsync(Guid doctorId)
    {
        return await _conversationService.GetByDoctorIdAsync(doctorId);
    }

    /// <summary>
    /// Get doctor profile by user ID
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<DoctorProfile?> GetDoctorProfileAsync(Guid userId)
    {
        return await _doctorProfileService.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Save doctor profile (create or update)
    /// Simplified interface for UI layer
    /// </summary>
    public async Task SaveDoctorProfileAsync(DoctorProfile profile)
    {
        if (profile.Id == Guid.Empty)
        {
            await _doctorProfileService.CreateAsync(profile);
        }
        else
        {
            await _doctorProfileService.UpdateAsync(profile);
        }

        await _activityLogService.LogActivityAsync(
            profile.UserId,
            "UpdateDoctorProfile",
            $"Profile ID: {profile.Id}");
    }

    /// <summary>
    /// Update doctor profile photo
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<bool> UpdateDoctorProfilePhotoAsync(Guid userId, byte[]? photoData)
    {
        var success = await _doctorProfileService.UpdateProfilePhotoAsync(userId, photoData);
        
        if (success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                photoData != null ? "UploadProfilePhoto" : "DeleteProfilePhoto",
                $"User ID: {userId}");
        }

        return success;
    }
}

// DTOs for Facade responses
public class DoctorDashboardData
{
    public DoctorProfile? Profile { get; set; }
    public List<Conversation> ActiveConversations { get; set; } = new();
    public DoctorPerformanceStats PerformanceStats { get; set; } = new();
}

public class ConsultationResult
{
    public ConsultationNote ConsultationNote { get; set; } = null!;
    public Prescription? Prescription { get; set; }
    public MedicalRecord MedicalRecord { get; set; } = null!;
}

public class DoctorWorkloadSummary
{
    public int ActiveConsultations { get; set; }
    public int PendingConsultations { get; set; }
    public int CompletedToday { get; set; }
    public int TotalCompleted { get; set; }
    public decimal AverageRating { get; set; }
}
