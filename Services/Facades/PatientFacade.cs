using ai_clinic.Models;
using ai_clinic.Services.DoctorRecommendation;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using PdfDocument = QuestPDF.Fluent.Document;

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
    private readonly DocumentService _documentService;
    private readonly UserService _userService;
    private readonly PatientSettingsService _patientSettingsService;
    // private readonly PatientConsultationWorkflowService _workflowService;
    private readonly MedicalRecordExportService _exportService;

    public PatientFacade(
        PatientProfileService patientProfileService,
        ConversationService conversationService,
        MedicalRecordService medicalRecordService,
        PrescriptionService prescriptionService,
        ConsultationService consultationService,
        ActivityLogService activityLogService,
        DocumentService documentService,
        UserService userService,
        PatientSettingsService patientSettingsService,
        // PatientConsultationWorkflowService workflowService,
        MedicalRecordExportService exportService)
    {
        _patientProfileService = patientProfileService;
        _conversationService = conversationService;
        _medicalRecordService = medicalRecordService;
        _prescriptionService = prescriptionService;
        _consultationService = consultationService;
        _activityLogService = activityLogService;
        _documentService = documentService;
        _userService = userService;
        _patientSettingsService = patientSettingsService;
        // _workflowService = workflowService;
        _exportService = exportService;
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

        var conversations = await conversationsTask;
        var medicalRecords = await medicalRecordsTask;

        // Log activity
        await _activityLogService.LogActivityAsync(userId, "ViewDashboard");

        return new PatientDashboardData
        {
            Profile = await profileTask,
            RecentConversations = conversations.OrderByDescending(c => c.UpdatedAt).Take(3).ToList(),
            MedicalRecords = medicalRecords,
            ActivePrescriptions = (await prescriptionsTask)
                .Where(p => p.IsActive)
                .ToList(),
            UpcomingAppointment = conversations
                .Where(c => c.Status == ConversationStatus.Active && c.AssignedDoctorId.HasValue)
                .OrderBy(c => c.CreatedAt)
                .FirstOrDefault(),
            RecentHealthMetric = medicalRecords
                .Where(r => r.RecordType == "Lab Result")
                .OrderByDescending(r => r.RecordDate)
                .FirstOrDefault()
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
        var existingProfile = await _patientProfileService.GetByUserIdAsync(profile.UserId);

        if (existingProfile == null)
        {
            await _patientProfileService.CreateAsync(profile);
        }
        else
        {
            profile.Id = existingProfile.Id;
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
        if (photoData == null)
        {
            return false;
        }
        
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
    /// Get patient profile photo
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<byte[]?> GetPatientProfilePhotoAsync(Guid userId)
    {
        return await _patientProfileService.GetProfilePhotoAsync(userId);
    }

    #region Medical Records Management

    /// <summary>
    /// Get all medical records for a patient with statistics
    /// Facade method that coordinates multiple services
    /// </summary>
    public async Task<PatientRecordsData> GetPatientRecordsAsync(Guid userId)
    {
        // Get all records in parallel
        var medicalRecordsTask = _medicalRecordService.GetByPatientIdAsync(userId);
        var prescriptionsTask = _prescriptionService.GetByPatientIdAsync(userId);
        var documentsTask = _documentService.GetByPatientIdAsync(userId);

        await Task.WhenAll(medicalRecordsTask, prescriptionsTask, documentsTask);

        var medicalRecords = await medicalRecordsTask;
        var prescriptions = await prescriptionsTask;
        var documents = await documentsTask;

        // Debug logging
        Console.WriteLine($"[PATIENT FACADE] Medical Records Count: {medicalRecords.Count}");
        Console.WriteLine($"[PATIENT FACADE] Prescriptions Count: {prescriptions.Count}");
        foreach (var p in prescriptions)
        {
            Console.WriteLine($"[PATIENT FACADE]   - Prescription ID: {p.Id}, Medication: {p.MedicationName}, IsActive: {p.IsActive}");
        }
        Console.WriteLine($"[PATIENT FACADE] Documents Count: {documents.Count}");

        // Calculate statistics
        var stats = new RecordStatistics
        {
            TotalRecords = medicalRecords.Count + prescriptions.Count + documents.Count,
            LabResults = medicalRecords.Count(r => r.RecordType?.Equals("Lab Result", StringComparison.OrdinalIgnoreCase) == true) +
                        documents.Count(d => d.DocumentTypeString?.Equals("Lab Result", StringComparison.OrdinalIgnoreCase) == true),
            Prescriptions = prescriptions.Count + 
                           medicalRecords.Count(r => r.RecordType?.Equals("Prescription", StringComparison.OrdinalIgnoreCase) == true),
            ImagingStudies = medicalRecords.Count(r => r.RecordType?.Equals("Imaging", StringComparison.OrdinalIgnoreCase) == true) +
                            documents.Count(d => d.DocumentTypeString?.Equals("Imaging", StringComparison.OrdinalIgnoreCase) == true),
            VisitNotes = medicalRecords.Count(r => 
                r.RecordType?.Equals("Visit Note", StringComparison.OrdinalIgnoreCase) == true ||
                r.RecordType?.Equals("Consultation", StringComparison.OrdinalIgnoreCase) == true ||
                r.RecordType?.Equals("Consultation Note", StringComparison.OrdinalIgnoreCase) == true ||
                r.RecordType?.Equals("AI Consultation", StringComparison.OrdinalIgnoreCase) == true) +
                        documents.Count(d => d.DocumentTypeString?.Equals("Visit Note", StringComparison.OrdinalIgnoreCase) == true),
            Immunizations = medicalRecords.Count(r => r.RecordType?.Equals("Immunization", StringComparison.OrdinalIgnoreCase) == true) +
                           documents.Count(d => d.DocumentTypeString?.Equals("Immunization", StringComparison.OrdinalIgnoreCase) == true)
        };

        Console.WriteLine($"[PATIENT FACADE] Statistics - Total: {stats.TotalRecords}, Lab: {stats.LabResults}, Rx: {stats.Prescriptions}, Visit: {stats.VisitNotes}");

        // Log activity
        await _activityLogService.LogActivityAsync(userId, "ViewMedicalRecords");

        return new PatientRecordsData
        {
            MedicalRecords = medicalRecords,
            Prescriptions = prescriptions,
            Documents = documents,
            Statistics = stats
        };
    }

    /// <summary>
    /// Upload a new medical document
    /// Coordinates document creation and activity logging
    /// </summary>
    public async Task<Models.Document> UploadMedicalDocumentAsync(
        Guid userId,
        string title,
        string documentType,
        byte[] fileData,
        string fileName,
        string? description = null)
    {
        var document = new Models.Document
        {
            PatientId = userId,
            Title = title,
            DocumentTypeString = documentType,
            FileName = fileName,
            FileData = fileData,
            FileSizeBytes = fileData.Length,
            Description = description,
            UploadedByUserId = userId,
            CreatedAt = DateTime.UtcNow,
            FileUrl = "", // Empty for binary storage
            FileType = DocumentType.Other // Default enum value
        };

        document = await _documentService.CreateAsync(document);

        await _activityLogService.LogActivityAsync(
            userId,
            "UploadDocument",
            $"{{\"document_id\": \"{document.Id}\", \"type\": \"{documentType}\", \"title\": \"{title}\"}}");

        return document;
    }

    /// <summary>
    /// Download a medical document
    /// Coordinates document retrieval and activity logging
    /// </summary>
    public async Task<byte[]?> DownloadMedicalDocumentAsync(Guid userId, Guid documentId)
    {
        var document = await _documentService.GetByIdAsync(documentId);
        
        if (document == null || document.PatientId != userId)
        {
            return null;
        }

        await _activityLogService.LogActivityAsync(
            userId,
            "DownloadDocument",
            $"{{\"document_id\": \"{documentId}\", \"title\": \"{document.Title}\"}}");

        return document.FileData;
    }

    /// <summary>
    /// Get a medical document by ID (returns full document object)
    /// Simplified interface for UI layer to access document metadata
    /// </summary>
    public async Task<Models.Document?> GetMedicalDocumentAsync(Guid userId, Guid documentId)
    {
        var document = await _documentService.GetByIdAsync(documentId);
        
        if (document == null || document.PatientId != userId)
        {
            return null;
        }

        return document;
    }

    /// <summary>
    /// Get consultation notes for a patient
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<List<ConsultationNote>> GetConsultationNotesAsync(Guid userId)
    {
        return await _consultationService.GetByPatientIdAsync(userId);
    }

    /// <summary>
    /// Get prescriptions for a patient
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<List<Prescription>> GetPrescriptionsAsync(Guid userId)
    {
        return await _prescriptionService.GetByPatientIdAsync(userId);
    }

    /// <summary>
    /// Delete a medical record or document
    /// Coordinates deletion and activity logging
    /// </summary>
    public async Task<bool> DeleteMedicalRecordAsync(Guid userId, Guid recordId, string recordType)
    {
        bool success = false;

        switch (recordType.ToLower())
        {
            case "medical_record":
                success = await _medicalRecordService.DeleteAsync(recordId);
                break;
            case "prescription":
                success = await _prescriptionService.DeleteAsync(recordId);
                break;
            case "document":
                success = await _documentService.DeleteAsync(recordId);
                break;
        }

        if (success)
        {
            await _activityLogService.LogActivityAsync(
                userId,
                "DeleteMedicalRecord",
                $"{{\"record_id\": \"{recordId}\", \"type\": \"{recordType}\"}}");
        }

        return success;
    }

    /// <summary>
    /// Get medical records timeline
    /// Returns chronologically ordered records for timeline view
    /// </summary>
    public async Task<List<TimelineItem>> GetMedicalTimelineAsync(Guid userId)
    {
        var recordsData = await GetPatientRecordsAsync(userId);
        var timeline = new List<TimelineItem>();

        // Add medical records
        foreach (var record in recordsData.MedicalRecords)
        {
            timeline.Add(new TimelineItem
            {
                Date = record.RecordDate,
                Title = record.Title,
                Type = record.RecordType,
                Description = record.Content // Use Content field
            });
        }

        // Add prescriptions
        foreach (var prescription in recordsData.Prescriptions)
        {
            timeline.Add(new TimelineItem
            {
                Date = prescription.CreatedAt, // Use CreatedAt field
                Title = $"Prescription - {prescription.MedicationName}",
                Type = "Prescription",
                Description = prescription.Dosage
            });
        }

        // Add documents
        foreach (var document in recordsData.Documents)
        {
            timeline.Add(new TimelineItem
            {
                Date = document.CreatedAt,
                Title = document.Title ?? document.FileName,
                Type = document.DocumentTypeString ?? document.FileType.ToString(),
                Description = document.Description
            });
        }

        // Sort by date descending
        return timeline.OrderByDescending(t => t.Date).ToList();
    }

    #endregion
    
    /*
    // TODO: Implement PatientConsultationWorkflowService and related types
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
    */

    /// <summary>
    /// Get patient settings
    /// Combines User and PatientProfile settings
    /// </summary>
    public async Task<PatientSettingsData> GetPatientSettingsAsync(Guid userId)
    {
        var profile = await _patientProfileService.GetByUserIdAsync(userId);
        
        await _activityLogService.LogActivityAsync(userId, "ViewPatientSettings");

        return new PatientSettingsData
        {
            Profile = profile,
            Email = profile?.User?.Email ?? string.Empty,
            DataSharingEnabled = profile?.User?.DataSharingEnabled ?? false,
            AiAnalysisEnabled = profile?.User?.AiAnalysisEnabled ?? true,
            ActivityTrackingEnabled = profile?.User?.ActivityTrackingEnabled ?? true
        };
    }

    /// <summary>
    /// Save patient settings
    /// Updates User settings
    /// </summary>
    public async Task SavePatientSettingsAsync(Guid userId, PatientSettingsData settings)
    {
        var profile = await _patientProfileService.GetByUserIdAsync(userId);
        
        if (profile?.User != null)
        {
            profile.User.DataSharingEnabled = settings.DataSharingEnabled;
            profile.User.AiAnalysisEnabled = settings.AiAnalysisEnabled;
            profile.User.ActivityTrackingEnabled = settings.ActivityTrackingEnabled;
            profile.User.UpdatedAt = DateTime.UtcNow;

            // Update user through UserService
            // Note: You may need to add an UpdateUser method to UserService
            await _activityLogService.LogActivityAsync(userId, "UpdatePatientSettings");
        }
    }

    /// <summary>
    /// Change patient's email address
    /// Validates that the new email is not already in use
    /// </summary>
    public async Task<(bool Success, string Message)> ChangeEmailAsync(Guid userId, string newEmail)
    {
        return await _patientSettingsService.ChangeEmailAsync(userId, newEmail);
    }

    /// <summary>
    /// Change patient's password
    /// Validates password complexity and prevents using the same password
    /// </summary>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string confirmPassword)
    {
        return await _patientSettingsService.ChangePasswordAsync(userId, currentPassword, newPassword, confirmPassword);
    }

    /// <summary>
    /// Export all patient data including profile, medical records, prescriptions, documents, and conversations
    /// Returns a comprehensive PDF report
    /// </summary>
    public async Task<byte[]> ExportAllPatientDataAsync(Guid userId)
    {
        // Get all patient data in parallel
        var profileTask = _patientProfileService.GetByUserIdAsync(userId);
        var userTask = _userService.GetByIdAsync(userId);
        var medicalRecordsTask = _medicalRecordService.GetByPatientIdAsync(userId);
        var prescriptionsTask = _prescriptionService.GetByPatientIdAsync(userId);
        var documentsTask = _documentService.GetByPatientIdAsync(userId);
        var conversationsTask = _conversationService.GetByPatientIdAsync(userId);

        await Task.WhenAll(profileTask, userTask, medicalRecordsTask, prescriptionsTask, documentsTask, conversationsTask);

        var profile = await profileTask;
        var user = await userTask;
        var medicalRecords = await medicalRecordsTask;
        var prescriptions = await prescriptionsTask;
        var documents = await documentsTask;
        var conversations = await conversationsTask;

        // Generate comprehensive PDF
        var document = PdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                page.Header()
                    .Height(100)
                    .Background(Colors.Blue.Lighten3)
                    .Padding(20)
                    .Column(column =>
                    {
                        column.Item().Text("AI Clinic - Complete Patient Data Export")
                            .FontSize(18)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                        
                        column.Item().Text($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);
                        
                        column.Item().Text("This document contains all your personal and medical information")
                            .FontSize(9)
                            .FontColor(Colors.Grey.Darken1);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Personal Information Section
                        column.Item().Text("PERSONAL INFORMATION")
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        column.Item().PaddingTop(10).Text($"Email: {user?.Email ?? "N/A"}");
                        column.Item().Text($"Full Name: {profile?.FullName ?? "N/A"}");
                        column.Item().Text($"Date of Birth: {profile?.DateOfBirth?.ToString("yyyy-MM-dd") ?? "N/A"}");
                        column.Item().Text($"Gender: {profile?.Gender ?? "N/A"}");
                        column.Item().Text($"Phone: {user?.Phone ?? "N/A"}");
                        column.Item().Text($"Address: {profile?.Address ?? "N/A"}");
                        column.Item().Text($"Blood Type: {profile?.BloodType ?? "N/A"}");
                        column.Item().Text($"Allergies: {profile?.Allergies ?? "None"}");
                        column.Item().Text($"Emergency Contact: {profile?.EmergencyContactName ?? "N/A"} - {profile?.EmergencyContactPhone ?? "N/A"}");
                        column.Item().Text($"Account Created: {user?.CreatedAt.ToString("yyyy-MM-dd") ?? "N/A"}");
                        
                        column.Item().PaddingTop(15);

                        // Privacy Settings Section
                        column.Item().Text("PRIVACY SETTINGS")
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        column.Item().PaddingTop(10).Text($"Data Sharing: {(user?.DataSharingEnabled == true ? "Enabled" : "Disabled")}");
                        column.Item().Text($"AI Analysis: {(user?.AiAnalysisEnabled == true ? "Enabled" : "Disabled")}");
                        column.Item().Text($"Activity Tracking: {(user?.ActivityTrackingEnabled == true ? "Enabled" : "Disabled")}");
                        
                        column.Item().PaddingTop(15);

                        // Medical Records Section
                        column.Item().Text($"MEDICAL RECORDS ({medicalRecords.Count})")
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        if (medicalRecords.Any())
                        {
                            foreach (var record in medicalRecords.OrderByDescending(r => r.RecordDate).Take(20))
                            {
                                column.Item().PaddingTop(10).Text($"Date: {record.RecordDate:yyyy-MM-dd}")
                                    .FontSize(10).Bold();
                                column.Item().Text($"Type: {record.RecordType}");
                                column.Item().Text($"Title: {record.Title}");
                                if (!string.IsNullOrEmpty(record.DiagnosisDescription))
                                    column.Item().Text($"Diagnosis: {record.DiagnosisDescription}");
                                column.Item().Text($"Content: {record.Content}");
                                if (!string.IsNullOrEmpty(record.Medications))
                                    column.Item().Text($"Medications: {record.Medications}");
                            }
                        }
                        else
                        {
                            column.Item().PaddingTop(10).Text("No medical records found");
                        }

                        column.Item().PageBreak();

                        // Prescriptions Section
                        column.Item().Text($"PRESCRIPTIONS ({prescriptions.Count})")
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        if (prescriptions.Any())
                        {
                            foreach (var prescription in prescriptions.OrderByDescending(p => p.CreatedAt))
                            {
                                column.Item().PaddingTop(10).Text($"Date: {prescription.CreatedAt:yyyy-MM-dd}")
                                    .FontSize(10).Bold();
                                column.Item().Text($"Medication: {prescription.MedicationName}");
                                column.Item().Text($"Dosage: {prescription.Dosage}");
                                column.Item().Text($"Frequency: {prescription.Frequency}");
                                column.Item().Text($"Duration: {prescription.Duration ?? "Ongoing"}");
                                column.Item().Text($"Status: {(prescription.IsActive ? "Active" : "Inactive")}");
                                if (!string.IsNullOrEmpty(prescription.Instructions))
                                    column.Item().Text($"Instructions: {prescription.Instructions}");
                            }
                        }
                        else
                        {
                            column.Item().PaddingTop(10).Text("No prescriptions found");
                        }

                        column.Item().PaddingTop(15);

                        // Documents Section
                        column.Item().Text($"DOCUMENTS ({documents.Count})")
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        if (documents.Any())
                        {
                            foreach (var doc in documents.OrderByDescending(d => d.CreatedAt))
                            {
                                column.Item().PaddingTop(10).Text($"Date: {doc.CreatedAt:yyyy-MM-dd}")
                                    .FontSize(10).Bold();
                                column.Item().Text($"Title: {doc.Title ?? doc.FileName}");
                                column.Item().Text($"Type: {doc.DocumentTypeString ?? doc.FileType.ToString()}");
                                column.Item().Text($"File: {doc.FileName} ({doc.FileSizeBytes} bytes)");
                                if (!string.IsNullOrEmpty(doc.Description))
                                    column.Item().Text($"Description: {doc.Description}");
                            }
                        }
                        else
                        {
                            column.Item().PaddingTop(10).Text("No documents found");
                        }

                        column.Item().PaddingTop(15);

                        // Consultations Section
                        column.Item().Text($"CONSULTATIONS ({conversations.Count})")
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        if (conversations.Any())
                        {
                            foreach (var conversation in conversations.OrderByDescending(c => c.CreatedAt).Take(10))
                            {
                                column.Item().PaddingTop(10).Text($"Date: {conversation.CreatedAt:yyyy-MM-dd}")
                                    .FontSize(10).Bold();
                                column.Item().Text($"Title: {conversation.Title}");
                                column.Item().Text($"Status: {conversation.Status}");
                                if (!string.IsNullOrEmpty(conversation.InitialSymptoms))
                                    column.Item().Text($"Symptoms: {conversation.InitialSymptoms}");
                                if (conversation.AssignedDoctorId.HasValue)
                                    column.Item().Text($"Assigned Doctor: Yes");
                            }
                        }
                        else
                        {
                            column.Item().PaddingTop(10).Text("No consultations found");
                        }

                        column.Item().PaddingTop(15);

                        // Summary
                        column.Item().Text("DATA EXPORT SUMMARY")
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(5).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        column.Item().PaddingTop(10).Text($"Total Medical Records: {medicalRecords.Count}");
                        column.Item().Text($"Total Prescriptions: {prescriptions.Count}");
                        column.Item().Text($"Total Documents: {documents.Count}");
                        column.Item().Text($"Total Consultations: {conversations.Count}");
                        column.Item().Text($"Export Date: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                        
                        column.Item().PaddingTop(15);
                        column.Item().Text("This is a complete export of your data as of the date above. For the most current information, please log in to your AI Clinic account.")
                            .FontSize(9).Italic().FontColor(Colors.Grey.Darken1);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
            });
        });

        var pdfBytes = document.GeneratePdf();

        await _activityLogService.LogActivityAsync(
            userId,
            "ExportAllData",
            "Complete patient data exported to PDF");

        return pdfBytes;
    }

    /// <summary>
    /// Export patient's medical records to PDF
    /// Coordinates export service and activity logging
    /// </summary>
    public async Task<byte[]> ExportMedicalRecordsToPdfAsync(
        Guid patientId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        var pdfBytes = await _exportService.ExportMedicalRecordsToPdfAsync(patientId, startDate, endDate);

        await _activityLogService.LogActivityAsync(
            patientId,
            "ExportMedicalRecords",
            $"{{\"start_date\": \"{startDate?.ToString("yyyy-MM-dd") ?? "all"}\", \"end_date\": \"{endDate?.ToString("yyyy-MM-dd") ?? "all"}\", \"size_bytes\": {pdfBytes.Length}}}");

        return pdfBytes;
    }

    /// <summary>
    /// Export a single prescription as PDF
    /// </summary>
    public async Task<byte[]> ExportSinglePrescriptionToPdfAsync(Guid patientId, Guid prescriptionId)
    {
        var prescription = await _prescriptionService.GetByIdAsync(prescriptionId);
        if (prescription == null || prescription.PatientId != patientId)
        {
            throw new Exception("Prescription not found or access denied");
        }

        var patient = await _userService.GetByIdAsync(patientId);
        var patientProfile = await _patientProfileService.GetByUserIdAsync(patientId);

        var document = PdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header()
                    .Height(80)
                    .Background(Colors.Blue.Lighten3)
                    .Padding(20)
                    .Column(column =>
                    {
                        column.Item().Text("AI Clinic - Prescription")
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                        
                        column.Item().Text($"Date: {prescription.CreatedAt:yyyy-MM-dd}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Patient Info
                        column.Item().Text("Patient Information")
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        column.Item().PaddingTop(10).Text($"Name: {patientProfile?.FullName ?? "N/A"}");
                        column.Item().Text($"Date of Birth: {patientProfile?.DateOfBirth?.ToString("yyyy-MM-dd") ?? "N/A"}");
                        
                        column.Item().PaddingTop(20);

                        // Prescription Details
                        column.Item().Text("Prescription Details")
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        column.Item().PaddingTop(15).Text($"Medication: {prescription.MedicationName}")
                            .FontSize(13).Bold();
                        column.Item().PaddingTop(10).Text($"Dosage: {prescription.Dosage}");
                        column.Item().Text($"Frequency: {prescription.Frequency}");
                        column.Item().Text($"Duration: {prescription.Duration ?? "Ongoing"}");
                        
                        if (!string.IsNullOrEmpty(prescription.Instructions))
                        {
                            column.Item().PaddingTop(10).Text("Instructions:")
                                .FontSize(12).Bold();
                            column.Item().Text(prescription.Instructions);
                        }

                        column.Item().PaddingTop(15).Text($"Status: {(prescription.IsActive ? "Active" : "Inactive")}")
                            .FontColor(prescription.IsActive ? Colors.Green.Darken1 : Colors.Red.Darken1);
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
            });
        });

        await _activityLogService.LogActivityAsync(
            patientId,
            "ExportPrescription",
            $"{{\"prescription_id\": \"{prescriptionId}\"}}");

        return document.GeneratePdf();
    }

    /// <summary>
    /// Export a single medical record as PDF
    /// </summary>
    public async Task<byte[]> ExportSingleMedicalRecordToPdfAsync(Guid patientId, Guid recordId)
    {
        var record = await _medicalRecordService.GetByIdAsync(recordId);
        if (record == null || record.PatientId != patientId)
        {
            throw new Exception("Medical record not found or access denied");
        }

        var patient = await _userService.GetByIdAsync(patientId);
        var patientProfile = await _patientProfileService.GetByUserIdAsync(patientId);

        var document = PdfDocument.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header()
                    .Height(80)
                    .Background(Colors.Blue.Lighten3)
                    .Padding(20)
                    .Column(column =>
                    {
                        column.Item().Text("AI Clinic - Medical Record")
                            .FontSize(20)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                        
                        column.Item().Text($"Date: {record.RecordDate:yyyy-MM-dd}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Patient Info
                        column.Item().Text("Patient Information")
                            .FontSize(14).Bold().FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        column.Item().PaddingTop(10).Text($"Name: {patientProfile?.FullName ?? "N/A"}");
                        column.Item().Text($"Date of Birth: {patientProfile?.DateOfBirth?.ToString("yyyy-MM-dd") ?? "N/A"}");
                        
                        column.Item().PaddingTop(20);

                        // Record Details
                        column.Item().Text(record.Title)
                            .FontSize(16).Bold().FontColor(Colors.Blue.Darken1);
                        column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        
                        column.Item().PaddingTop(15).Text($"Record Type: {record.RecordType}")
                            .FontSize(11).FontColor(Colors.Grey.Darken1);
                        
                        if (!string.IsNullOrEmpty(record.DiagnosisCode))
                        {
                            column.Item().PaddingTop(10).Text($"Diagnosis Code: {record.DiagnosisCode}");
                        }
                        
                        if (!string.IsNullOrEmpty(record.DiagnosisDescription))
                        {
                            column.Item().PaddingTop(10).Text("Diagnosis:")
                                .FontSize(12).Bold();
                            column.Item().Text(record.DiagnosisDescription);
                        }

                        column.Item().PaddingTop(15).Text("Content:")
                            .FontSize(12).Bold();
                        column.Item().Text(record.Content);

                        if (!string.IsNullOrEmpty(record.Medications))
                        {
                            column.Item().PaddingTop(15).Text("Medications:")
                                .FontSize(12).Bold();
                            column.Item().Text(record.Medications);
                        }
                    });

                page.Footer()
                    .AlignCenter()
                    .Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                    });
            });
        });

        await _medicalRecordService.UpdateExportStatisticsAsync(recordId);
        
        await _activityLogService.LogActivityAsync(
            patientId,
            "ExportMedicalRecord",
            $"{{\"record_id\": \"{recordId}\"}}");

        return document.GeneratePdf();
    }
}

// DTOs for Facade responses
public class PatientDashboardData
{
    public PatientProfile? Profile { get; set; }
    public List<Conversation> RecentConversations { get; set; } = new();
    public List<MedicalRecord> MedicalRecords { get; set; } = new();
    public List<Prescription> ActivePrescriptions { get; set; } = new();
    public Conversation? UpcomingAppointment { get; set; }
    public MedicalRecord? RecentHealthMetric { get; set; }
}

public class PatientMedicalHistory
{
    public PatientProfile? Profile { get; set; }
    public List<MedicalRecord> MedicalRecords { get; set; } = new();
    public List<Prescription> Prescriptions { get; set; } = new();
    public List<ConsultationNote> ConsultationNotes { get; set; } = new();
}

public class PatientRecordsData
{
    public List<MedicalRecord> MedicalRecords { get; set; } = new();
    public List<Prescription> Prescriptions { get; set; } = new();
    public List<Models.Document> Documents { get; set; } = new();
    public RecordStatistics Statistics { get; set; } = new();
}

public class RecordStatistics
{
    public int TotalRecords { get; set; }
    public int LabResults { get; set; }
    public int Prescriptions { get; set; }
    public int ImagingStudies { get; set; }
    public int VisitNotes { get; set; }
    public int Immunizations { get; set; }
}

public class TimelineItem
{
    public DateTime Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Description { get; set; }
}
    public class DoctorConsultationResult
{
    public Conversation Conversation { get; set; } = null!;
    public Guid DoctorId { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class PatientSettingsData
{
    public PatientProfile? Profile { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool DataSharingEnabled { get; set; }
    public bool AiAnalysisEnabled { get; set; }
    public bool ActivityTrackingEnabled { get; set; }
}
