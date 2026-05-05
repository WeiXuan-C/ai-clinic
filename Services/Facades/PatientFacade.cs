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
    private readonly DocumentService _documentService;

    public PatientFacade(
        PatientProfileService patientProfileService,
        ConversationService conversationService,
        MedicalRecordService medicalRecordService,
        PrescriptionService prescriptionService,
        ConsultationService consultationService,
        ActivityLogService activityLogService,
        DocumentService documentService)
    {
        _patientProfileService = patientProfileService;
        _conversationService = conversationService;
        _medicalRecordService = medicalRecordService;
        _prescriptionService = prescriptionService;
        _consultationService = consultationService;
        _activityLogService = activityLogService;
        _documentService = documentService;
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

        // Calculate statistics
        var stats = new RecordStatistics
        {
            TotalRecords = medicalRecords.Count + prescriptions.Count + documents.Count,
            LabResults = medicalRecords.Count(r => r.RecordType == "Lab Result"),
            Prescriptions = prescriptions.Count,
            ImagingStudies = medicalRecords.Count(r => r.RecordType == "Imaging"),
            VisitNotes = medicalRecords.Count(r => r.RecordType == "Visit Note"),
            Immunizations = medicalRecords.Count(r => r.RecordType == "Immunization")
        };

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
    public async Task<Document> UploadMedicalDocumentAsync(
        Guid userId,
        string title,
        string documentType,
        byte[] fileData,
        string fileName,
        string? description = null)
    {
        var document = new Document
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

public class PatientRecordsData
{
    public List<MedicalRecord> MedicalRecords { get; set; } = new();
    public List<Prescription> Prescriptions { get; set; } = new();
    public List<Document> Documents { get; set; } = new();
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
