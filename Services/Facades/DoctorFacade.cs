using ai_clinic.Models;
using ai_clinic.Services.Hubs;
using Microsoft.AspNetCore.SignalR;

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
    private readonly IHubContext<ConsultationHub> _hubContext;

    public DoctorFacade(
        DoctorProfileService doctorProfileService,
        ConversationService conversationService,
        ConsultationService consultationService,
        PrescriptionService prescriptionService,
        MedicalRecordService medicalRecordService,
        ActivityLogService activityLogService,
        StatisticsService statisticsService,
        IHubContext<ConsultationHub> hubContext)
    {
        _doctorProfileService = doctorProfileService;
        _conversationService = conversationService;
        _consultationService = consultationService;
        _prescriptionService = prescriptionService;
        _medicalRecordService = medicalRecordService;
        _activityLogService = activityLogService;
        _statisticsService = statisticsService;
        _hubContext = hubContext;
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
        var existingProfile = await _doctorProfileService.GetByUserIdAsync(profile.UserId);

        if (existingProfile == null)
        {
            await _doctorProfileService.CreateAsync(profile);
        }
        else
        {
            profile.Id = existingProfile.Id;
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

    /// <summary>
    /// Get doctor profile photo
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<byte[]?> GetDoctorProfilePhotoAsync(Guid userId)
    {
        return await _doctorProfileService.GetProfilePhotoAsync(userId);
    }

    /// <summary>
    /// Get comprehensive doctor dashboard data
    /// Includes stats, schedule, pending consultations, and recent activity
    /// </summary>
    public async Task<DoctorDashboardFullData> GetDoctorDashboardFullDataAsync(Guid userId)
    {
        var profile = await _doctorProfileService.GetByUserIdAsync(userId);
        var conversations = await _conversationService.GetByDoctorIdAsync(userId);
        var performance = await _statisticsService.GetDoctorPerformanceAsync(userId);
        var recentLogs = await _activityLogService.GetRecentLogsByUserAsync(userId, 10);

        var today = DateTime.UtcNow.Date;
        var activeConversations = conversations.Where(c => c.Status == ConversationStatus.Active).ToList();
        var pendingConversations = conversations.Where(c => c.Status == ConversationStatus.Deactive).ToList();

        await _activityLogService.LogActivityAsync(userId, "ViewDoctorDashboard");

        return new DoctorDashboardFullData
        {
            Profile = profile,
            Stats = new DoctorDashboardStatsData
            {
                PatientsToday = activeConversations.Count,
                TotalAppointments = conversations.Count(c => c.CreatedAt.Date == today),
                TotalConsultations = conversations.Count,
                AverageRating = performance.AverageRating
            },
            PendingConsultations = pendingConversations.Take(5).ToList(),
            RecentActivity = recentLogs,
            WeekPerformance = new WeekPerformanceData
            {
                PatientsSeen = performance.CompletedConsultations,
                AverageResponseTimeMinutes = 18, // TODO: Calculate from actual data
                SatisfactionRate = performance.AverageRating / 5.0m * 100,
                RecordsUpdated = recentLogs.Count(l => l.Action.Contains("Update"))
            }
        };
    }

    /// <summary>
    /// Get doctor analytics data
    /// Includes metrics, demographics, top conditions, and AI insights
    /// </summary>
    public async Task<DoctorAnalyticsFullData> GetDoctorAnalyticsAsync(Guid userId, string period = "month")
    {
        var conversations = await _conversationService.GetByDoctorIdAsync(userId);
        var performance = await _statisticsService.GetDoctorPerformanceAsync(userId);
        var medicalRecords = await _medicalRecordService.GetByDoctorIdAsync(userId);

        // Filter by period
        var startDate = period switch
        {
            "week" => DateTime.UtcNow.AddDays(-7),
            "quarter" => DateTime.UtcNow.AddMonths(-3),
            "year" => DateTime.UtcNow.AddYears(-1),
            _ => DateTime.UtcNow.AddMonths(-1)
        };

        var filteredConversations = conversations.Where(c => c.CreatedAt >= startDate).ToList();

        await _activityLogService.LogActivityAsync(userId, "ViewDoctorAnalytics");

        return new DoctorAnalyticsFullData
        {
            Metrics = new DoctorMetricsData
            {
                TotalPatients = filteredConversations.Select(c => c.PatientId).Distinct().Count(),
                TotalConsultations = filteredConversations.Count,
                AverageResponseTimeMinutes = 18, // TODO: Calculate from actual data
                PatientRating = performance.AverageRating
            },
            TopConditions = medicalRecords
                .Where(r => !string.IsNullOrEmpty(r.Title))
                .GroupBy(r => r.Title)
                .Select(g => new TopConditionData
                {
                    ConditionName = g.Key,
                    PatientCount = g.Count()
                })
                .OrderByDescending(c => c.PatientCount)
                .Take(5)
                .ToList()
        };
    }

    /// <summary>
    /// Get doctor's medical records with statistics
    /// </summary>
    public async Task<DoctorRecordsFullData> GetDoctorRecordsAsync(Guid userId, string? filter = null)
    {
        var medicalRecords = await _medicalRecordService.GetByDoctorIdAsync(userId);
        var prescriptions = await _prescriptionService.GetByDoctorIdAsync(userId);

        if (!string.IsNullOrEmpty(filter))
        {
            medicalRecords = medicalRecords.Where(r => 
                r.Title.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                r.Content.Contains(filter, StringComparison.OrdinalIgnoreCase)
            ).ToList();
        }

        await _activityLogService.LogActivityAsync(userId, "ViewDoctorRecords");

        return new DoctorRecordsFullData
        {
            MedicalRecords = medicalRecords.OrderByDescending(r => r.RecordDate).Take(20).ToList(),
            Prescriptions = prescriptions.OrderByDescending(p => p.CreatedAt).Take(20).ToList(),
            Statistics = new RecordStatisticsData
            {
                TotalRecords = medicalRecords.Count + prescriptions.Count,
                LabResults = medicalRecords.Count(r => r.RecordType == "Lab Result"),
                Prescriptions = prescriptions.Count,
                ImagingStudies = medicalRecords.Count(r => r.RecordType == "Imaging")
            }
        };
    }

    /// <summary>
    /// Get doctor's conversation list for UI
    /// Returns simplified conversation list items
    /// </summary>
    public async Task<List<ConversationListItem>> GetDoctorConversationListAsync(Guid doctorId)
    {
        var conversations = await _conversationService.GetByDoctorIdAsync(doctorId);
        
        return conversations.Select(c => new ConversationListItem
        {
            Id = c.Id,
            Title = c.Title ?? $"Consultation with {c.Patient?.Email?.Split('@')[0] ?? "Patient"}",
            LastMessageAt = c.LastMessageAt,
            Status = c.Status,
            UnreadCount = 0, // TODO: Implement unread count logic
            IsAiConversation = c.AiMessagesCount > 0,
            DoctorName = null // Not needed for doctor view
        }).OrderByDescending(c => c.LastMessageAt).ToList();
    }

    /// <summary>
    /// Get doctor's consultation session with messages
    /// Returns complete session data for chat interface
    /// </summary>
    public async Task<ConsultationSession> GetDoctorConsultationSessionAsync(Guid conversationId, Guid doctorId)
    {
        var conversation = await _conversationService.GetByIdAsync(conversationId);
        
        if (conversation == null || conversation.AssignedDoctorId != doctorId)
        {
            throw new UnauthorizedAccessException("Doctor does not have access to this conversation");
        }

        var messages = await _conversationService.GetMessagesAsync(conversationId);

        return new ConsultationSession
        {
            Conversation = conversation,
            Messages = messages.OrderBy(m => m.CreatedAt).ToList(),
            IsAiConsultation = conversation.AssignedDoctorId == null
        };
    }

    /// <summary>
    /// Send message from doctor
    /// Simplified interface for doctor chat
    /// </summary>
    public async Task<Message> SendDoctorMessageAsync(Guid conversationId, Guid doctorId, string content)
    {
        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = doctorId,
            SenderType = MessageSenderType.Doctor,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        message = await _conversationService.AddMessageAsync(message);

        // Update conversation last message time
        await _conversationService.UpdateLastMessageTimeAsync(conversationId);

        // 🔔 REAL-TIME: Send doctor message via SignalR
        await _hubContext.SendMessageToConversation(conversationId, message);

        await _activityLogService.LogActivityAsync(
            doctorId,
            "SendDoctorMessage",
            $"Conversation ID: {conversationId}");

        return message;
    }

    /// <summary>
    /// Record consultation details without closing the conversation.
    /// </summary>
    public async Task<ConsultationNote> SaveConsultationNoteAsync(
        Guid conversationId,
        Guid doctorId,
        Guid patientId,
        string diagnosis,
        string? symptoms,
        string? physicalExamination,
        string? treatmentPlan,
        string? followUpInstructions,
        bool finalize)
    {
        var note = new ConsultationNote
        {
            ConversationId = conversationId,
            DoctorId = doctorId,
            PatientId = patientId,
            Symptoms = symptoms,
            PhysicalExamination = physicalExamination,
            Diagnosis = diagnosis,
            TreatmentPlan = treatmentPlan,
            FollowUpInstructions = followUpInstructions,
            IsFinalized = finalize,
            FinalizedAt = finalize ? DateTime.UtcNow : null
        };

        note = await _consultationService.CreateAsync(note);

        var medicalRecord = new MedicalRecord
        {
            PatientId = patientId,
            ConversationId = conversationId,
            CreatedByDoctorId = doctorId,
            RecordType = "Consultation Note",
            Title = $"Consultation Note - {DateTime.UtcNow:yyyy-MM-dd}",
            Content = $"Diagnosis: {diagnosis}\nSymptoms: {symptoms}\nPhysical Examination: {physicalExamination}\nTreatment Plan: {treatmentPlan}\nFollow-up: {followUpInstructions}",
            DiagnosisDescription = diagnosis,
            RecordDate = DateTime.UtcNow
        };

        await _medicalRecordService.CreateAsync(medicalRecord);

        if (finalize)
        {
            await _conversationService.UpdateStatusAsync(conversationId, ConversationStatus.Closed);
        }

        await _activityLogService.LogActivityAsync(
            doctorId,
            "SaveConsultationNote",
            $"Conversation ID: {conversationId}");

        return note;
    }

    /// <summary>
    /// Generate and store a prescription for a patient.
    /// </summary>
    public async Task<Prescription> CreatePrescriptionAsync(
        Guid conversationId,
        Guid doctorId,
        Guid patientId,
        Guid? consultationNoteId,
        string medicationName,
        string dosage,
        string frequency,
        string? duration,
        string? instructions)
    {
        var prescription = new Prescription
        {
            ConsultationNoteId = consultationNoteId,
            PatientId = patientId,
            DoctorId = doctorId,
            MedicationName = medicationName,
            Dosage = dosage,
            Frequency = frequency,
            Duration = duration,
            Instructions = instructions
        };

        prescription = await _prescriptionService.CreateAsync(prescription);

        var medicalRecord = new MedicalRecord
        {
            PatientId = patientId,
            ConversationId = conversationId,
            CreatedByDoctorId = doctorId,
            RecordType = "Prescription",
            Title = $"Prescription - {medicationName}",
            Content = $"Medication: {medicationName}\nDosage: {dosage}\nFrequency: {frequency}\nDuration: {duration}\nInstructions: {instructions}",
            Medications = medicationName,
            RecordDate = DateTime.UtcNow
        };

        await _medicalRecordService.CreateAsync(medicalRecord);

        await _activityLogService.LogActivityAsync(
            doctorId,
            "CreatePrescription",
            $"Conversation ID: {conversationId}, Medication: {medicationName}");

        return prescription;
    }

    /// <summary>
    /// Get doctor settings
    /// Combines User and DoctorProfile settings
    /// </summary>
    public async Task<DoctorSettingsData> GetDoctorSettingsAsync(Guid userId)
    {
        var profile = await _doctorProfileService.GetByUserIdAsync(userId);
        
        await _activityLogService.LogActivityAsync(userId, "ViewDoctorSettings");

        return new DoctorSettingsData
        {
            Profile = profile,
            Email = profile?.User?.Email ?? string.Empty,
            AvailabilityStatus = profile?.AvailabilityStatus ?? DoctorAvailabilityStatus.Offline,
            AutoAcceptAppointments = profile?.AutoAcceptAppointments ?? false,
            MaxDailyPatients = profile?.MaxDailyPatients ?? 30,
            NotifyUrgentConsultations = profile?.NotifyUrgentConsultations ?? true,
            NotifyNewAppointments = profile?.NotifyNewAppointments ?? true,
            NotifyAiAssessments = profile?.NotifyAiAssessments ?? true,
            NotifyEmailSummaries = profile?.NotifyEmailSummaries ?? false,
            SessionTimeoutMinutes = profile?.SessionTimeoutMinutes ?? 30
        };
    }

    /// <summary>
    /// Save doctor settings
    /// Updates both User and DoctorProfile
    /// </summary>
    public async Task SaveDoctorSettingsAsync(Guid userId, DoctorSettingsData settings)
    {
        var profile = await _doctorProfileService.GetByUserIdAsync(userId);
        
        if (profile != null)
        {
            profile.AvailabilityStatus = settings.AvailabilityStatus;
            profile.AutoAcceptAppointments = settings.AutoAcceptAppointments;
            profile.MaxDailyPatients = settings.MaxDailyPatients;
            profile.NotifyUrgentConsultations = settings.NotifyUrgentConsultations;
            profile.NotifyNewAppointments = settings.NotifyNewAppointments;
            profile.NotifyAiAssessments = settings.NotifyAiAssessments;
            profile.NotifyEmailSummaries = settings.NotifyEmailSummaries;
            profile.SessionTimeoutMinutes = settings.SessionTimeoutMinutes;
            profile.UpdatedAt = DateTime.UtcNow;

            await _doctorProfileService.UpdateAsync(profile);

            await _activityLogService.LogActivityAsync(userId, "UpdateDoctorSettings");
        }
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

// Extended DTOs for Dashboard and Analytics
public class DoctorDashboardFullData
{
    public DoctorProfile? Profile { get; set; }
    public DoctorDashboardStatsData Stats { get; set; } = new();
    public List<Conversation> PendingConsultations { get; set; } = new();
    public List<ActivityLog> RecentActivity { get; set; } = new();
    public WeekPerformanceData WeekPerformance { get; set; } = new();
}

public class DoctorDashboardStatsData
{
    public int PatientsToday { get; set; }
    public int TotalAppointments { get; set; }
    public int TotalConsultations { get; set; }
    public decimal AverageRating { get; set; }
}

public class WeekPerformanceData
{
    public int PatientsSeen { get; set; }
    public int AverageResponseTimeMinutes { get; set; }
    public decimal SatisfactionRate { get; set; }
    public int RecordsUpdated { get; set; }
}

public class DoctorAnalyticsFullData
{
    public DoctorMetricsData Metrics { get; set; } = new();
    public List<TopConditionData> TopConditions { get; set; } = new();
}

public class DoctorMetricsData
{
    public int TotalPatients { get; set; }
    public int TotalConsultations { get; set; }
    public int AverageResponseTimeMinutes { get; set; }
    public decimal PatientRating { get; set; }
}

public class TopConditionData
{
    public string ConditionName { get; set; } = string.Empty;
    public int PatientCount { get; set; }
}

public class DoctorRecordsFullData
{
    public List<MedicalRecord> MedicalRecords { get; set; } = new();
    public List<Prescription> Prescriptions { get; set; } = new();
    public RecordStatisticsData Statistics { get; set; } = new();
}

public class RecordStatisticsData
{
    public int TotalRecords { get; set; }
    public int LabResults { get; set; }
    public int Prescriptions { get; set; }
    public int ImagingStudies { get; set; }
}

public class DoctorSettingsData
{
    public DoctorProfile? Profile { get; set; }
    public string Email { get; set; } = string.Empty;
    public DoctorAvailabilityStatus AvailabilityStatus { get; set; }
    public bool AutoAcceptAppointments { get; set; }
    public int MaxDailyPatients { get; set; }
    public bool NotifyUrgentConsultations { get; set; }
    public bool NotifyNewAppointments { get; set; }
    public bool NotifyAiAssessments { get; set; }
    public bool NotifyEmailSummaries { get; set; }
    public int SessionTimeoutMinutes { get; set; }
}
