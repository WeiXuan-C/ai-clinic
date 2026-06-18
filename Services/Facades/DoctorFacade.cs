using ai_clinic.Models;
using ai_clinic.Services.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services.Facades;

/// <summary>
/// Facade Pattern: Provides a unified interface for doctor-related operations
/// Coordinates multiple subsystems: DoctorProfile, Conversation, Consultation, Prescription, Export Services
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
    private readonly DoctorRecordExportService _doctorRecordExportService;
    private readonly DoctorReportExportService _doctorReportExportService;
    private readonly UserService _userService;
    private readonly DoctorSettingsService _doctorSettingsService;

    public DoctorFacade(
        DoctorProfileService doctorProfileService,
        ConversationService conversationService,
        ConsultationService consultationService,
        PrescriptionService prescriptionService,
        MedicalRecordService medicalRecordService,
        ActivityLogService activityLogService,
        StatisticsService statisticsService,
        IHubContext<ConsultationHub> hubContext,
        DoctorRecordExportService doctorRecordExportService,
        DoctorReportExportService doctorReportExportService,
        UserService userService,
        DoctorSettingsService doctorSettingsService)
    {
        _doctorProfileService = doctorProfileService;
        _conversationService = conversationService;
        _consultationService = consultationService;
        _prescriptionService = prescriptionService;
        _medicalRecordService = medicalRecordService;
        _activityLogService = activityLogService;
        _statisticsService = statisticsService;
        _hubContext = hubContext;
        _doctorRecordExportService = doctorRecordExportService;
        _doctorReportExportService = doctorReportExportService;
        _userService = userService;
        _doctorSettingsService = doctorSettingsService;
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
        // Update statistics before returning profile
        await _doctorProfileService.UpdateDoctorStatisticsAsync(userId);
        return await _doctorProfileService.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Save doctor profile (create or update)
    /// Simplified interface for UI layer
    /// </summary>
    public async Task SaveDoctorProfileAsync(DoctorProfile profile)
    {
        try
        {
            // Clear User navigation property to prevent EF from tracking/updating the User entity
            // This prevents "UNIQUE constraint failed: users.email" errors
            profile.User = null!;
            
            var existingProfile = await _doctorProfileService.GetByUserIdAsync(profile.UserId);

            if (existingProfile == null)
            {
                Console.WriteLine($"[DoctorFacade] No existing profile found, creating new one for user: {profile.UserId}");
                await _doctorProfileService.CreateAsync(profile);
            }
            else
            {
                Console.WriteLine($"[DoctorFacade] Existing profile found (ID: {existingProfile.Id}), updating for user: {profile.UserId}");
                profile.Id = existingProfile.Id;
                profile.CreatedAt = existingProfile.CreatedAt; // Preserve creation date
                await _doctorProfileService.UpdateAsync(profile);
            }

            // Update statistics after saving
            await _doctorProfileService.UpdateDoctorStatisticsAsync(profile.UserId);

            await _activityLogService.LogActivityAsync(
                profile.UserId,
                "UpdateDoctorProfile",
                $"Profile ID: {profile.Id}");
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("already exists"))
        {
            // Profile exists but GetByUserIdAsync returned null (possibly due to error)
            // Try to update instead
            Console.WriteLine($"[DoctorFacade] Profile creation failed (already exists), attempting update for user: {profile.UserId}");
            
            // Clear User navigation property again before retry
            profile.User = null!;
            
            // Fetch the existing profile again with a fresh db context
            var existingProfile = await _doctorProfileService.GetByUserIdAsync(profile.UserId);
            if (existingProfile != null)
            {
                profile.Id = existingProfile.Id;
                profile.CreatedAt = existingProfile.CreatedAt;
                await _doctorProfileService.UpdateAsync(profile);
                
                await _doctorProfileService.UpdateDoctorStatisticsAsync(profile.UserId);
                
                await _activityLogService.LogActivityAsync(
                    profile.UserId,
                    "UpdateDoctorProfile",
                    $"Profile ID: {profile.Id}");
            }
            else
            {
                // Still can't find it, throw the original exception
                throw;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorFacade] Error in SaveDoctorProfileAsync: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Manually update doctor statistics (for maintenance or refresh)
    /// </summary>
    public async Task UpdateDoctorStatisticsAsync(Guid userId)
    {
        await _doctorProfileService.UpdateDoctorStatisticsAsync(userId);
    }

    /// <summary>
    /// Update doctor profile photo
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<bool> UpdateDoctorProfilePhotoAsync(Guid userId, byte[]? photoData)
    {
        if (photoData == null)
        {
            return false;
        }

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
    /// Updates the conversation status (e.g., Active, Closed)
    /// </summary>
    public async Task UpdateConversationStatusAsync(Guid conversationId, ConversationStatus status)
    {
        var conversation = await _conversationService.GetByIdAsync(conversationId);
        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation {conversationId} not found");
        }

        // Use the existing UpdateStatusAsync method
        await _conversationService.UpdateStatusAsync(conversationId, status);

        await _activityLogService.LogActivityAsync(
            conversation.AssignedDoctorId ?? Guid.Empty,
            "UpdateConversationStatus",
            $"Conversation ID: {conversationId}, Status: {status}");
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
        try
        {
            Console.WriteLine($"[DoctorFacade] GetDoctorSettingsAsync started for user: {userId}");
            
            var profile = await _doctorProfileService.GetByUserIdAsync(userId);
            Console.WriteLine($"[DoctorFacade] Profile loaded: {profile != null}");
            
            // If profile doesn't have user loaded, create a new profile with defaults
            if (profile == null)
            {
                Console.WriteLine("[DoctorFacade] Profile is null, loading user email separately");
                
                // Get user email separately
                var user = await GetUserByIdAsync(userId);
                Console.WriteLine($"[DoctorFacade] User loaded: {user != null}, Email: {user?.Email}");
                
                await _activityLogService.LogActivityAsync(userId, "ViewDoctorSettings");

                return new DoctorSettingsData
                {
                    Profile = null,
                    Email = user?.Email ?? string.Empty,
                    AvailabilityStatus = DoctorAvailabilityStatus.Offline,
                    AutoAcceptAppointments = false,
                    MaxDailyPatients = 30,
                    NotifyUrgentConsultations = true,
                    NotifyNewAppointments = true,
                    NotifyAiAssessments = true,
                    NotifyEmailSummaries = false,
                    SessionTimeoutMinutes = 30
                };
            }
            
            // Get user email from profile's User or separately
            var emailToUse = profile.User?.Email ?? string.Empty;
            if (string.IsNullOrEmpty(emailToUse))
            {
                Console.WriteLine("[DoctorFacade] Email not in profile, loading user separately");
                var user = await GetUserByIdAsync(userId);
                emailToUse = user?.Email ?? string.Empty;
            }
            
            Console.WriteLine($"[DoctorFacade] ✓ Settings prepared successfully. Email: {emailToUse}");
            
            await _activityLogService.LogActivityAsync(userId, "ViewDoctorSettings");

            return new DoctorSettingsData
            {
                Profile = profile,
                Email = emailToUse,
                AvailabilityStatus = profile.AvailabilityStatus,
                AutoAcceptAppointments = profile.AutoAcceptAppointments,
                MaxDailyPatients = profile.MaxDailyPatients,
                NotifyUrgentConsultations = profile.NotifyUrgentConsultations,
                NotifyNewAppointments = profile.NotifyNewAppointments,
                NotifyAiAssessments = profile.NotifyAiAssessments,
                NotifyEmailSummaries = profile.NotifyEmailSummaries,
                SessionTimeoutMinutes = profile.SessionTimeoutMinutes
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorFacade] ERROR in GetDoctorSettingsAsync");
            Console.WriteLine($"  Type: {ex.GetType().Name}");
            Console.WriteLine($"  Message: {ex.Message}");
            Console.WriteLine($"  Stack Trace: {ex.StackTrace}");
            throw; // Re-throw to be handled by caller
        }
    }

    /// <summary>
    /// Helper method to get user by ID
    /// </summary>
    private async Task<User?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            using var db = Data.DbClient.Instance.GetDb();
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
            
            if (user == null)
            {
                Console.WriteLine($"[DoctorFacade] WARNING: User not found for ID: {userId}");
            }
            
            return user;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorFacade] ERROR in GetUserByIdAsync: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Save doctor settings
    /// Updates both User and DoctorProfile
    /// </summary>
    public async Task SaveDoctorSettingsAsync(Guid userId, DoctorSettingsData settings)
    {
        try
        {
            Console.WriteLine($"[DoctorFacade] SaveDoctorSettingsAsync started for user: {userId}");
            
            var profile = await _doctorProfileService.GetByUserIdAsync(userId);
            
            if (profile == null)
            {
                Console.WriteLine($"[DoctorFacade] WARNING: Profile not found for user: {userId}, creating new profile");
                
                // Create a new profile if it doesn't exist
                profile = new DoctorProfile
                {
                    UserId = userId,
                    AvailabilityStatus = settings.AvailabilityStatus,
                    AutoAcceptAppointments = settings.AutoAcceptAppointments,
                    MaxDailyPatients = settings.MaxDailyPatients,
                    NotifyUrgentConsultations = settings.NotifyUrgentConsultations,
                    NotifyNewAppointments = settings.NotifyNewAppointments,
                    NotifyAiAssessments = settings.NotifyAiAssessments,
                    NotifyEmailSummaries = settings.NotifyEmailSummaries,
                    SessionTimeoutMinutes = settings.SessionTimeoutMinutes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                await _doctorProfileService.CreateAsync(profile);
                Console.WriteLine("[DoctorFacade] ✓ New profile created");
            }
            else
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
                Console.WriteLine("[DoctorFacade] ✓ Profile updated");
            }

            await _activityLogService.LogActivityAsync(userId, "UpdateDoctorSettings");
            Console.WriteLine("[DoctorFacade] ✓ SaveDoctorSettingsAsync completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorFacade] ERROR in SaveDoctorSettingsAsync");
            Console.WriteLine($"  Type: {ex.GetType().Name}");
            Console.WriteLine($"  Message: {ex.Message}");
            Console.WriteLine($"  Stack Trace: {ex.StackTrace}");
            throw; // Re-throw to be handled by caller
        }
    }

    /// <summary>
    /// Update medical record - Facade Pattern
    /// Coordinates record update and activity logging
    /// </summary>
    public async Task UpdateMedicalRecordAsync(MedicalRecord medicalRecord, Guid doctorId)
    {
        await _medicalRecordService.UpdateAsync(medicalRecord);
        
        await _activityLogService.LogActivityAsync(
            doctorId,
            "UpdateMedicalRecord",
            $"Medical Record ID: {medicalRecord.Id}");
    }

    /// <summary>
    /// Update prescription - Facade Pattern
    /// Coordinates prescription update and activity logging
    /// </summary>
    public async Task UpdatePrescriptionAsync(Prescription prescription, Guid doctorId)
    {
        await _prescriptionService.UpdateAsync(prescription);
        
        await _activityLogService.LogActivityAsync(
            doctorId,
            "UpdatePrescription",
            $"Prescription ID: {prescription.Id}");
    }

    /// <summary>
    /// Create medical record - Facade Pattern
    /// Coordinates record creation and activity logging
    /// </summary>
    public async Task<MedicalRecord> CreateMedicalRecordAsync(MedicalRecord medicalRecord, Guid doctorId)
    {
        medicalRecord = await _medicalRecordService.CreateAsync(medicalRecord);
        
        await _activityLogService.LogActivityAsync(
            doctorId,
            "CreateMedicalRecord",
            $"Medical Record ID: {medicalRecord.Id}");

        return medicalRecord;
    }

    /// <summary>
    /// Create prescription - Facade Pattern
    /// Coordinates prescription creation and activity logging (simplified version)
    /// </summary>
    public async Task<Prescription> CreatePrescriptionSimpleAsync(Prescription prescription, Guid doctorId)
    {
        prescription = await _prescriptionService.CreateAsync(prescription);
        
        await _activityLogService.LogActivityAsync(
            doctorId,
            "CreatePrescription",
            $"Prescription ID: {prescription.Id}");

        return prescription;
    }

    /// <summary>
    /// Export doctor records to PDF format - Facade Pattern
    /// Coordinates export service and activity logging
    /// </summary>
    public async Task<byte[]> ExportDoctorRecordsToPdfAsync(
        Guid doctorId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var pdfBytes = await _doctorReportExportService.GenerateDoctorAnalyticsReportAsync(
            doctorId,
            startDate,
            endDate);

        await _activityLogService.LogActivityAsync(
            doctorId,
            "ExportDoctorRecordsPDF",
            $"{{\"start_date\": \"{startDate?.ToString("yyyy-MM-dd") ?? "all"}\", " +
            $"\"end_date\": \"{endDate?.ToString("yyyy-MM-dd") ?? "all"}\", " +
            $"\"size_bytes\": {pdfBytes.Length}}}");

        return pdfBytes;
    }

    /// <summary>
    /// Export doctor records to CSV format - Facade Pattern
    /// Coordinates export service and activity logging
    /// </summary>
    public async Task<string> ExportDoctorRecordsToCsvAsync(
        Guid doctorId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeMedicalRecords = true,
        bool includePrescriptions = true)
    {
        var csvContent = await _doctorRecordExportService.ExportToCsvAsync(
            doctorId,
            startDate,
            endDate,
            includeMedicalRecords,
            includePrescriptions);

        await _activityLogService.LogActivityAsync(
            doctorId,
            "ExportDoctorRecordsCSV",
            $"{{\"start_date\": \"{startDate?.ToString("yyyy-MM-dd") ?? "all"}\", " +
            $"\"end_date\": \"{endDate?.ToString("yyyy-MM-dd") ?? "all"}\", " +
            $"\"size_bytes\": {csvContent.Length}}}");

        return csvContent;
    }

    /// <summary>
    /// Export doctor records to JSON format - Facade Pattern
    /// Coordinates export service and activity logging
    /// </summary>
    public async Task<string> ExportDoctorRecordsToJsonAsync(
        Guid doctorId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeMedicalRecords = true,
        bool includePrescriptions = true)
    {
        var jsonContent = await _doctorRecordExportService.ExportToJsonAsync(
            doctorId,
            startDate,
            endDate,
            includeMedicalRecords,
            includePrescriptions);

        await _activityLogService.LogActivityAsync(
            doctorId,
            "ExportDoctorRecordsJSON",
            $"{{\"start_date\": \"{startDate?.ToString("yyyy-MM-dd") ?? "all"}\", " +
            $"\"end_date\": \"{endDate?.ToString("yyyy-MM-dd") ?? "all"}\", " +
            $"\"size_bytes\": {jsonContent.Length}}}");

        return jsonContent;
    }

    // ====================================================================
    // ACCOUNT SECURITY OPERATIONS - Facade Pattern
    // Coordinates UserService, DoctorSettingsService, and ActivityLogService
    // ====================================================================

    /// <summary>
    /// Change doctor's email address - Facade Pattern
    /// Coordinates email validation, user service, and activity logging
    /// </summary>
    public async Task<(bool Success, string Message)> ChangeEmailAsync(
        Guid userId,
        string currentPassword,
        string newEmail)
    {
        try
        {
            // Delegate to DoctorSettingsService for email change logic
            var result = await _doctorSettingsService.ChangeEmailAsync(userId, currentPassword, newEmail);

            // Log activity if successful
            if (result.Success)
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    "ChangeEmail",
                    $"{{\"new_email\": \"{newEmail}\"}}");
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorFacade] Error in ChangeEmailAsync: {ex.Message}");
            return (false, "An error occurred while changing email");
        }
    }

    /// <summary>
    /// Change doctor's password - Facade Pattern
    /// Coordinates password validation, user service, and activity logging
    /// </summary>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword)
    {
        try
        {
            // Delegate to DoctorSettingsService for password change logic
            var result = await _doctorSettingsService.ChangePasswordAsync(userId, currentPassword, newPassword);

            // Log activity if successful
            if (result.Success)
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    "ChangePassword",
                    null);
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorFacade] Error in ChangePasswordAsync: {ex.Message}");
            return (false, "An error occurred while changing password");
        }
    }

    /// <summary>
    /// Deactivate doctor account - Facade Pattern
    /// Coordinates account deactivation, profile update, and activity logging
    /// </summary>
    public async Task<(bool Success, string Message)> DeactivateAccountAsync(
        Guid userId,
        string password)
    {
        try
        {
            // Delegate to DoctorSettingsService for deactivation logic
            var result = await _doctorSettingsService.DeactivateAccountAsync(userId, password);

            // Log activity if successful
            if (result.Success)
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    "DeactivateAccount",
                    null);

                // Update doctor profile availability status
                var profile = await _doctorProfileService.GetByUserIdAsync(userId);
                if (profile != null)
                {
                    profile.AvailabilityStatus = DoctorAvailabilityStatus.Offline;
                    profile.IsAcceptingPatients = false;
                    await _doctorProfileService.UpdateAsync(profile);
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorFacade] Error in DeactivateAccountAsync: {ex.Message}");
            return (false, "An error occurred while deactivating account");
        }
    }

    /// <summary>
    /// Delete doctor account permanently - Facade Pattern
    /// Coordinates account deletion, related data cleanup, and activity logging
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteAccountAsync(
        Guid userId,
        string password)
    {
        try
        {
            // Log activity BEFORE deletion
            await _activityLogService.LogActivityAsync(
                userId,
                "DeleteAccount",
                null);

            // Delegate to DoctorSettingsService for deletion logic
            var result = await _doctorSettingsService.DeleteAccountAsync(userId, password);

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorFacade] Error in DeleteAccountAsync: {ex.Message}");
            return (false, "An error occurred while deleting account");
        }
    }

    /// <summary>
    /// Download doctor's data in JSON format - Facade Pattern
    /// Coordinates data retrieval from multiple sources and activity logging
    /// </summary>
    public async Task<string?> DownloadMyDataAsync(Guid userId)
    {
        try
        {
            // Delegate to DoctorSettingsService for data export logic
            var jsonData = await _doctorSettingsService.DownloadMyDataAsync(userId);

            // Log activity if successful
            if (jsonData != null)
            {
                await _activityLogService.LogActivityAsync(
                    userId,
                    "DownloadMyData",
                    $"{{\"size_bytes\": {jsonData.Length}}}");
            }

            return jsonData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorFacade] Error in DownloadMyDataAsync: {ex.Message}");
            return null;
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
