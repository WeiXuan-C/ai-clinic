using ai_clinic.Models;

namespace ai_clinic.Services.Facades;

/// <summary>
/// Facade Pattern: Provides a unified interface for patient-related operations
/// Coordinates multiple subsystems: PatientProfile, Conversation, MedicalRecord, Prescription
/// </summary>
public class PatientFacade
{
    private readonly PatientProfileService _patientProfileService;
    private readonly ConversationService _conversationService;
    private readonly MedicalRecordService _medicalRecordService;
    private readonly PrescriptionService _prescriptionService;
    private readonly ConsultationService _consultationService;
    private readonly ActivityLogService _activityLogService;

    public PatientFacade(
        PatientProfileService patientProfileService,
        ConversationService conversationService,
        MedicalRecordService medicalRecordService,
        PrescriptionService prescriptionService,
        ConsultationService consultationService,
        ActivityLogService activityLogService)
    {
        _patientProfileService = patientProfileService;
        _conversationService = conversationService;
        _medicalRecordService = medicalRecordService;
        _prescriptionService = prescriptionService;
        _consultationService = consultationService;
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Get complete patient dashboard data
    /// Coordinates multiple services to gather all patient information
    /// </summary>
    public async Task<PatientDashboardData> GetDashboardDataAsync(Guid userId)
    {
        // Parallel execution for better performance
        var profileTask = _patientProfileService.GetByUserIdAsync(userId);
        var conversationsTask = _conversationService.GetByPatientIdAsync(userId);
        var medicalRecordsTask = _medicalRecordService.GetByPatientIdAsync(userId);
        var prescriptionsTask = _prescriptionService.GetByPatientIdAsync(userId);

        await Task.WhenAll(profileTask, conversationsTask, medicalRecordsTask, prescriptionsTask);

        // Log activity
        await _activityLogService.LogActivityAsync(userId, "ViewDashboard");

        return new PatientDashboardData
        {
            Profile = await profileTask,
            RecentConversations = (await conversationsTask).Take(5).ToList(),
            MedicalRecords = await medicalRecordsTask,
            ActivePrescriptions = (await prescriptionsTask)
                .Where(p => p.IsActive)
                .ToList()
        };
    }

    /// <summary>
    /// Start a new consultation
    /// Coordinates conversation creation and initial message
    /// </summary>
    public async Task<Conversation> StartConsultationAsync(
        Guid patientId, 
        string title, 
        string initialSymptoms,
        string initialMessage)
    {
        // Create conversation
        var conversation = new Conversation
        {
            PatientId = patientId,
            Title = title,
            Status = ConversationStatus.Active,
            InitialSymptoms = initialSymptoms,
            ConsultationStatus = "pending_doctor_assignment"
        };

        conversation = await _conversationService.CreateAsync(conversation);

        // Log activity
        await _activityLogService.LogActivityAsync(
            patientId, 
            "StartConsultation", 
            $"Conversation ID: {conversation.Id}");

        return conversation;
    }

    /// <summary>
    /// Get patient's complete medical history
    /// Combines profile, records, prescriptions, and consultations
    /// </summary>
    public async Task<PatientMedicalHistory> GetMedicalHistoryAsync(Guid userId)
    {
        var profileTask = _patientProfileService.GetByUserIdAsync(userId);
        var medicalRecordsTask = _medicalRecordService.GetByPatientIdAsync(userId);
        var prescriptionsTask = _prescriptionService.GetByPatientIdAsync(userId);
        var consultationsTask = _consultationService.GetByPatientIdAsync(userId);

        await Task.WhenAll(profileTask, medicalRecordsTask, prescriptionsTask, consultationsTask);

        await _activityLogService.LogActivityAsync(userId, "ViewMedicalHistory");

        return new PatientMedicalHistory
        {
            Profile = await profileTask,
            MedicalRecords = await medicalRecordsTask,
            Prescriptions = await prescriptionsTask,
            ConsultationNotes = await consultationsTask
        };
    }

    /// <summary>
    /// Get patient profile by user ID
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<PatientProfile?> GetPatientProfileAsync(Guid userId)
    {
        return await _patientProfileService.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Save patient profile (create or update)
    /// Simplified interface for UI layer
    /// </summary>
    public async Task SavePatientProfileAsync(PatientProfile profile)
    {
        if (profile.Id == Guid.Empty)
        {
            await _patientProfileService.CreateAsync(profile);
        }
        else
        {
            await _patientProfileService.UpdateAsync(profile);
        }

        await _activityLogService.LogActivityAsync(
            profile.UserId,
            "UpdatePatientProfile",
            $"Profile ID: {profile.Id}");
    }

    /// <summary>
    /// Update patient profile photo
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<bool> UpdatePatientProfilePhotoAsync(Guid userId, byte[]? photoData)
    {
        var success = await _patientProfileService.UpdateProfilePhotoAsync(userId, photoData);
        
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
public class PatientDashboardData
{
    public PatientProfile? Profile { get; set; }
    public List<Conversation> RecentConversations { get; set; } = new();
    public List<MedicalRecord> MedicalRecords { get; set; } = new();
    public List<Prescription> ActivePrescriptions { get; set; } = new();
}

public class PatientMedicalHistory
{
    public PatientProfile? Profile { get; set; }
    public List<MedicalRecord> MedicalRecords { get; set; } = new();
    public List<Prescription> Prescriptions { get; set; } = new();
    public List<ConsultationNote> ConsultationNotes { get; set; } = new();
}
