using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ai_clinic.Models;
using ai_clinic.Services;
using ai_clinic.Services.Facades;
using System.Text;

namespace ai_clinic.UI.Pages.Doctor;

public partial class Records : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private DoctorFacade DoctorFacade { get; set; } = null!;
    [Inject] private MedicalRecordService MedicalRecordService { get; set; } = null!;
    [Inject] private PrescriptionService PrescriptionService { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private bool isLoading = true;
    private string? errorMessage;
    private string currentFilter = "all";
    private string searchQuery = "";

    private List<MedicalRecord> medicalRecords = new();
    private List<Prescription> prescriptions = new();
    private RecordStatisticsData statistics = new();

    // Modal state - Single Responsibility Principle: Each state manages one concern
    private bool showViewModal = false;
    private bool showEditModal = false;
    private bool isSaving = false;
    private string? editErrorMessage;
    private object? selectedRecord;

    // Edit state - Encapsulation: Separate edit entities from display entities
    private MedicalRecord editMedicalRecord = new();
    private Prescription editPrescription = new();
    private DateTime editMedicalRecordDate = DateTime.UtcNow;

    protected override async Task OnInitializedAsync()
    {
        if (!AuthFacade.IsAuthenticated || AuthFacade.CurrentUser?.Role != UserRole.Doctor)
        {
            Navigation.NavigateTo("/auth/signin");
            return;
        }

        await LoadRecords();
    }

    private async Task LoadRecords()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            var userId = AuthFacade.CurrentUser!.Id;
            var recordsData = await DoctorFacade.GetDoctorRecordsAsync(userId, string.IsNullOrEmpty(searchQuery) ? null : searchQuery);

            medicalRecords = recordsData.MedicalRecords;
            prescriptions = recordsData.Prescriptions;
            statistics = recordsData.Statistics;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load records: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SetFilter(string filter)
    {
        currentFilter = filter;
        await LoadRecords();
    }

    private async Task HandleSearch()
    {
        await LoadRecords();
    }

    private List<object> GetFilteredRecords()
    {
        var allRecords = new List<object>();

        if (currentFilter == "all" || currentFilter == "records")
        {
            allRecords.AddRange(medicalRecords.Cast<object>());
        }

        if (currentFilter == "all" || currentFilter == "prescriptions")
        {
            allRecords.AddRange(prescriptions.Cast<object>());
        }

        return allRecords;
    }

    private string GetRecordType(object record)
    {
        return record switch
        {
            MedicalRecord mr => mr.RecordType ?? "VISIT NOTE",
            Prescription => "PRESCRIPTION",
            _ => "UNKNOWN"
        };
    }

    private string GetRecordTitle(object record)
    {
        return record switch
        {
            MedicalRecord mr => mr.Title,
            Prescription p => $"{p.MedicationName} Prescription",
            _ => "Unknown Record"
        };
    }

    private string GetRecordDate(object record)
    {
        var date = record switch
        {
            MedicalRecord mr => mr.RecordDate,
            Prescription p => p.CreatedAt,
            _ => DateTime.UtcNow
        };

        return date.ToString("MMM dd, yyyy");
    }

    private string GetRecordDetails(object record)
    {
        return record switch
        {
            MedicalRecord mr => $"Type: {mr.RecordType ?? "General"}",
            Prescription p => $"Dosage: {p.Dosage} • {p.Frequency}",
            _ => ""
        };
    }

    private string GetBadgeClass(string recordType)
    {
        return recordType.ToLower() switch
        {
            "lab result" => "badge-lab",
            "prescription" => "badge-prescription",
            "imaging" => "badge-imaging",
            _ => "badge-visit"
        };
    }

    private string GetIconClass(string recordType)
    {
        return recordType.ToLower() switch
        {
            "lab result" => "lab",
            "prescription" => "prescription",
            "imaging" => "imaging",
            _ => "visit"
        };
    }

    private string GetIconName(string recordType)
    {
        return recordType.ToLower() switch
        {
            "lab result" => "flask-conical",
            "prescription" => "pill",
            "imaging" => "scan",
            _ => "file-text"
        };
    }

    // ============================================
    // VIEW FUNCTIONALITY
    // Single Responsibility: Handle viewing records
    // ============================================
    
    /// <summary>
    /// View record details in modal
    /// Follows Open/Closed Principle: extensible for new record types
    /// </summary>
    private void ViewRecord(object record)
    {
        selectedRecord = record;
        showViewModal = true;
        StateHasChanged();
    }

    // ============================================
    // EDIT FUNCTIONALITY
    // Single Responsibility: Handle editing records
    // ============================================
    
    /// <summary>
    /// Open edit modal for a record
    /// Follows Abstraction: Works with any record type
    /// </summary>
    private void EditRecord(object record)
    {
        selectedRecord = record;
        editErrorMessage = null;

        // Encapsulation: Create separate edit copies to avoid direct mutation
        if (record is MedicalRecord mr)
        {
            editMedicalRecord = new MedicalRecord
            {
                Id = mr.Id,
                PatientId = mr.PatientId,
                ConversationId = mr.ConversationId,
                CreatedByDoctorId = mr.CreatedByDoctorId,
                RecordType = mr.RecordType,
                Title = mr.Title,
                Content = mr.Content,
                DiagnosisDescription = mr.DiagnosisDescription,
                Medications = mr.Medications,
                RecordDate = mr.RecordDate,
                DiagnosisCode = mr.DiagnosisCode
            };
            editMedicalRecordDate = mr.RecordDate.Date;
        }
        else if (record is Prescription p)
        {
            editPrescription = new Prescription
            {
                Id = p.Id,
                ConsultationNoteId = p.ConsultationNoteId,
                PatientId = p.PatientId,
                DoctorId = p.DoctorId,
                MedicationName = p.MedicationName,
                Dosage = p.Dosage,
                Frequency = p.Frequency,
                Duration = p.Duration,
                Instructions = p.Instructions,
                IsActive = p.IsActive
            };
        }

        showEditModal = true;
        StateHasChanged();
    }

    /// <summary>
    /// Save edited record
    /// Follows Dependency Inversion: Depends on service abstractions
    /// </summary>
    private async Task SaveRecord()
    {
        if (selectedRecord == null) return;

        isSaving = true;
        editErrorMessage = null;
        StateHasChanged();

        try
        {
            // Strategy Pattern: Different save strategies for different record types
            if (selectedRecord is MedicalRecord)
            {
                await SaveMedicalRecord();
            }
            else if (selectedRecord is Prescription)
            {
                await SavePrescription();
            }

            // Reload data and close modal
            await LoadRecords();
            CloseModals();
        }
        catch (Exception ex)
        {
            editErrorMessage = $"Failed to save: {ex.Message}";
        }
        finally
        {
            isSaving = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Save medical record - Single Responsibility
    /// </summary>
    private async Task SaveMedicalRecord()
    {
        // Validation - Guard Clauses
        if (string.IsNullOrWhiteSpace(editMedicalRecord.Title))
            throw new InvalidOperationException("Title is required");

        if (string.IsNullOrWhiteSpace(editMedicalRecord.Content))
            throw new InvalidOperationException("Content is required");

        // Update record date from date picker
        editMedicalRecord.RecordDate = editMedicalRecordDate;

        // Update via service (Facade Pattern)
        await MedicalRecordService.UpdateAsync(editMedicalRecord);
    }

    /// <summary>
    /// Save prescription - Single Responsibility
    /// </summary>
    private async Task SavePrescription()
    {
        // Validation - Guard Clauses
        if (string.IsNullOrWhiteSpace(editPrescription.MedicationName))
            throw new InvalidOperationException("Medication name is required");

        if (string.IsNullOrWhiteSpace(editPrescription.Dosage))
            throw new InvalidOperationException("Dosage is required");

        if (string.IsNullOrWhiteSpace(editPrescription.Frequency))
            throw new InvalidOperationException("Frequency is required");

        // Update via service (Facade Pattern)
        await PrescriptionService.UpdateAsync(editPrescription);
    }

    // ============================================
    // DOWNLOAD FUNCTIONALITY
    // Single Responsibility: Handle downloading records
    // ============================================
    
    /// <summary>
    /// Download record as text file
    /// Follows Open/Closed Principle: Extensible for new formats (PDF, CSV, etc.)
    /// </summary>
    private async Task DownloadRecord(object record)
    {
        try
        {
            string content;
            string filename;

            // Strategy Pattern: Different export strategies for different record types
            if (record is MedicalRecord mr)
            {
                content = GenerateMedicalRecordText(mr);
                filename = $"MedicalRecord_{mr.RecordDate:yyyyMMdd}_{SanitizeFilename(mr.Title)}.txt";
            }
            else if (record is Prescription p)
            {
                content = GeneratePrescriptionText(p);
                filename = $"Prescription_{p.CreatedAt:yyyyMMdd}_{SanitizeFilename(p.MedicationName)}.txt";
            }
            else
            {
                return;
            }

            // Use JSInterop to trigger browser download
            await JS.InvokeVoidAsync("downloadFile", filename, content);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to download: {ex.Message}";
            StateHasChanged();
        }
    }

    /// <summary>
    /// Generate formatted text for medical record
    /// Single Responsibility: Text generation for medical records
    /// </summary>
    private string GenerateMedicalRecordText(MedicalRecord record)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("===========================================");
        sb.AppendLine("MEDICAL RECORD");
        sb.AppendLine("===========================================");
        sb.AppendLine();
        sb.AppendLine($"Title: {record.Title}");
        sb.AppendLine($"Record Type: {record.RecordType}");
        sb.AppendLine($"Date: {record.RecordDate:MMMM dd, yyyy}");
        sb.AppendLine($"Patient: {record.Patient?.Email ?? "N/A"}");
        
        if (!string.IsNullOrEmpty(record.DiagnosisDescription))
        {
            sb.AppendLine($"Diagnosis: {record.DiagnosisDescription}");
        }
        
        if (!string.IsNullOrEmpty(record.DiagnosisCode))
        {
            sb.AppendLine($"Diagnosis Code: {record.DiagnosisCode}");
        }

        if (!string.IsNullOrEmpty(record.Medications))
        {
            sb.AppendLine($"Medications: {record.Medications}");
        }
        
        sb.AppendLine();
        sb.AppendLine("CONTENT:");
        sb.AppendLine("-------------------------------------------");
        sb.AppendLine(record.Content);
        sb.AppendLine("-------------------------------------------");
        sb.AppendLine();
        sb.AppendLine($"Created: {record.CreatedAt:yyyy-MM-dd HH:mm}");
        sb.AppendLine($"Last Updated: {record.UpdatedAt:yyyy-MM-dd HH:mm}");
        
        if (record.CreatedByDoctor != null)
        {
            sb.AppendLine($"Doctor: {record.CreatedByDoctor.Email}");
        }
        
        return sb.ToString();
    }

    /// <summary>
    /// Generate formatted text for prescription
    /// Single Responsibility: Text generation for prescriptions
    /// </summary>
    private string GeneratePrescriptionText(Prescription prescription)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("===========================================");
        sb.AppendLine("PRESCRIPTION");
        sb.AppendLine("===========================================");
        sb.AppendLine();
        sb.AppendLine($"Patient: {prescription.Patient?.Email ?? "N/A"}");
        sb.AppendLine($"Doctor: {prescription.Doctor?.Email ?? "N/A"}");
        sb.AppendLine($"Date Issued: {prescription.CreatedAt:MMMM dd, yyyy}");
        sb.AppendLine($"Status: {(prescription.IsActive ? "Active" : "Inactive")}");
        sb.AppendLine();
        sb.AppendLine("MEDICATION DETAILS:");
        sb.AppendLine("-------------------------------------------");
        sb.AppendLine($"Medication: {prescription.MedicationName}");
        sb.AppendLine($"Dosage: {prescription.Dosage}");
        sb.AppendLine($"Frequency: {prescription.Frequency}");
        
        if (!string.IsNullOrEmpty(prescription.Duration))
        {
            sb.AppendLine($"Duration: {prescription.Duration}");
        }
        
        sb.AppendLine();
        
        if (!string.IsNullOrEmpty(prescription.Instructions))
        {
            sb.AppendLine("INSTRUCTIONS:");
            sb.AppendLine("-------------------------------------------");
            sb.AppendLine(prescription.Instructions);
            sb.AppendLine("-------------------------------------------");
            sb.AppendLine();
        }
        
        sb.AppendLine($"Last Updated: {prescription.UpdatedAt:yyyy-MM-dd HH:mm}");
        
        return sb.ToString();
    }

    /// <summary>
    /// Sanitize filename for safe file system operations
    /// Utility method following Single Responsibility
    /// </summary>
    private string SanitizeFilename(string filename)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = string.Concat(filename.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        return sanitized.Length > 50 ? sanitized.Substring(0, 50) : sanitized;
    }

    // ============================================
    // MODAL MANAGEMENT
    // Single Responsibility: Handle modal state
    // ============================================
    
    /// <summary>
    /// Close all modals
    /// Encapsulation: Centralized modal state management
    /// </summary>
    private void CloseModals()
    {
        showViewModal = false;
        showEditModal = false;
        selectedRecord = null;
        editErrorMessage = null;
        StateHasChanged();
    }
}
