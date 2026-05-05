using ai_clinic.Models;
using ai_clinic.Services.DoctorRecommendation;

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
    private readonly PatientConsultationWorkflowService _workflowService;

    public PatientFacade(
        PatientProfileService patientProfileService,
        ConversationService conversationService,
        MedicalRecordService medicalRecordService,
        PrescriptionService prescriptionService,
        ConsultationService consultationService,
        ActivityLogService activityLogService,
        PatientConsultationWorkflowService workflowService)
    {
        _patientProfileService = patientProfileService;
        _conversationService = conversationService;
        _medicalRecordService = medicalRecordService;
        _prescriptionService = prescriptionService;
        _consultationService = consultationService;
        _activityLogService = activityLogService;
        _workflowService = workflowService;
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

    /// <summary>
    /// 发送患者消息并获取AI分析和医生推荐
    /// 完整的工作流：消息 -> AI分析 -> 症状提取 -> 医疗记录 -> 医生推荐
    /// </summary>
    public async Task<PatientConsultationWorkflowResult> SendMessageAndGetRecommendationsAsync(
        Guid conversationId,
        Guid patientId,
        string message)
    {
        Console.WriteLine("[PATIENT FACADE] SendMessageAndGetRecommendationsAsync called");
        
        // 执行完整工作流
        var result = await _workflowService.ExecuteFullWorkflowAsync(
            conversationId,
            patientId,
            message);

        // 记录活动
        await _activityLogService.LogActivityAsync(
            patientId,
            "AiConsultation",
            $"Conversation: {conversationId}, Symptoms: {string.Join(", ", result.Analysis.Symptoms)}");

        return result;
    }

    /// <summary>
    /// 选择医生并创建咨询
    /// 用户从推荐列表中选择医生后调用此方法
    /// </summary>
    public async Task<DoctorConsultationResult> SelectDoctorAndCreateConsultationAsync(
        Guid patientId,
        Guid doctorId,
        Guid aiConversationId,
        AiSymptomAnalysis analysis)
    {
        Console.WriteLine("[PATIENT FACADE] SelectDoctorAndCreateConsultationAsync called");

        // 创建与医生的咨询
        var doctorConversation = await _workflowService.CreateDoctorConsultationAsync(
            patientId,
            doctorId,
            aiConversationId,
            analysis);

        // 记录活动
        await _activityLogService.LogActivityAsync(
            patientId,
            "SelectDoctor",
            $"Doctor: {doctorId}, Conversation: {doctorConversation.Id}");

        return new DoctorConsultationResult
        {
            Conversation = doctorConversation,
            DoctorId = doctorId,
            Success = true
        };
    }

    /// <summary>
    /// 获取推荐医生（不创建咨询）
    /// 用于显示医生列表供用户选择
    /// </summary>
    public async Task<List<DoctorMatchResult>> GetRecommendedDoctorsAsync(AiSymptomAnalysis analysis)
    {
        return await _workflowService.GetRecommendedDoctorsAsync(analysis);
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

public class DoctorConsultationResult
{
    public Conversation Conversation { get; set; } = null!;
    public Guid DoctorId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
