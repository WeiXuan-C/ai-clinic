using System.Text;
using System.Text.Json;
using ai_clinic.Models;

namespace ai_clinic.Services;

/// <summary>
/// Export Service for Doctor Medical Records
/// Strategy Pattern: Different export strategies for different formats (PDF, CSV, JSON)
/// Single Responsibility: Handles all export-related operations for doctors
/// </summary>
public class DoctorRecordExportService
{
    private readonly MedicalRecordService _medicalRecordService;
    private readonly PrescriptionService _prescriptionService;
    private readonly MedicalRecordExportService _medicalRecordExportService;

    public DoctorRecordExportService(
        MedicalRecordService medicalRecordService,
        PrescriptionService prescriptionService,
        MedicalRecordExportService medicalRecordExportService)
    {
        _medicalRecordService = medicalRecordService;
        _prescriptionService = prescriptionService;
        _medicalRecordExportService = medicalRecordExportService;
    }

    /// <summary>
    /// Export doctor's records to PDF
    /// Delegates to MedicalRecordExportService (Delegation Pattern)
    /// </summary>
    public async Task<byte[]> ExportToPdfAsync(
        Guid doctorId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        // Use existing PDF export service
        return await _medicalRecordExportService.ExportMedicalRecordsToPdfAsync(
            doctorId,
            startDate,
            endDate);
    }

    /// <summary>
    /// Export doctor's records to CSV
    /// Strategy Pattern: CSV export strategy
    /// </summary>
    public async Task<string> ExportToCsvAsync(
        Guid doctorId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeMedicalRecords = true,
        bool includePrescriptions = true)
    {
        var sb = new StringBuilder();

        // CSV Header
        sb.AppendLine("Type,Date,Patient,Title,Description,Details,Status");

        if (includeMedicalRecords)
        {
            var medicalRecords = await _medicalRecordService.GetByDoctorIdAsync(doctorId);
            
            foreach (var record in medicalRecords)
            {
                if (IsWithinDateRange(record.RecordDate, startDate, endDate))
                {
                    var type = "Medical Record";
                    var date = record.RecordDate.ToString("yyyy-MM-dd");
                    var patient = CsvEscape(record.Patient?.Email ?? "N/A");
                    var title = CsvEscape(record.Title);
                    var diagnosis = CsvEscape(record.DiagnosisDescription ?? "");
                    var content = CsvEscape(TruncateText(record.Content, 100));
                    var status = record.IsExported ? "Exported" : "New";

                    sb.AppendLine($"{type},{date},{patient},{title},{diagnosis},{content},{status}");
                }
            }
        }

        if (includePrescriptions)
        {
            // Get all prescriptions for this doctor
            var medicalRecords = await _medicalRecordService.GetByDoctorIdAsync(doctorId);
            var patientIds = medicalRecords.Select(r => r.PatientId).Distinct();
            
            foreach (var patientId in patientIds)
            {
                var prescriptions = await _prescriptionService.GetByPatientIdAsync(patientId);
                
                foreach (var prescription in prescriptions.Where(p => p.DoctorId == doctorId))
                {
                    if (IsWithinDateRange(prescription.CreatedAt, startDate, endDate))
                    {
                        var type = "Prescription";
                        var date = prescription.CreatedAt.ToString("yyyy-MM-dd");
                        var patient = CsvEscape(prescription.Patient?.Email ?? "N/A");
                        var medication = CsvEscape(prescription.MedicationName);
                        var dosage = CsvEscape(prescription.Dosage);
                        var frequency = CsvEscape(prescription.Frequency);
                        var status = prescription.IsActive ? "Active" : "Inactive";

                        sb.AppendLine($"{type},{date},{patient},{medication},{dosage},{frequency},{status}");
                    }
                }
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Export doctor's records to JSON
    /// Strategy Pattern: JSON export strategy
    /// </summary>
    public async Task<string> ExportToJsonAsync(
        Guid doctorId,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeMedicalRecords = true,
        bool includePrescriptions = true)
    {
        var exportData = new
        {
            ExportDate = DateTime.UtcNow,
            DoctorId = doctorId,
            DateRange = new
            {
                Start = startDate?.ToString("yyyy-MM-dd"),
                End = endDate?.ToString("yyyy-MM-dd")
            },
            MedicalRecords = includeMedicalRecords 
                ? await GetMedicalRecordsForExport(doctorId, startDate, endDate)
                : new List<object>(),
            Prescriptions = includePrescriptions
                ? await GetPrescriptionsForExport(doctorId, startDate, endDate)
                : new List<object>(),
            Summary = await GetExportSummary(doctorId, startDate, endDate)
        };

        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    /// <summary>
    /// Get medical records formatted for export
    /// Single Responsibility: Data transformation for medical records
    /// </summary>
    private async Task<List<object>> GetMedicalRecordsForExport(
        Guid doctorId,
        DateTime? startDate,
        DateTime? endDate)
    {
        var medicalRecords = await _medicalRecordService.GetByDoctorIdAsync(doctorId);
        
        return medicalRecords
            .Where(r => IsWithinDateRange(r.RecordDate, startDate, endDate))
            .OrderByDescending(r => r.RecordDate)
            .Select(r => new
            {
                r.Id,
                r.Title,
                r.RecordType,
                RecordDate = r.RecordDate.ToString("yyyy-MM-dd"),
                r.DiagnosisDescription,
                r.DiagnosisCode,
                r.Content,
                r.Medications,
                Patient = new
                {
                    r.Patient?.Email,
                    r.Patient?.Phone
                },
                CreatedAt = r.CreatedAt.ToString("o"),
                UpdatedAt = r.UpdatedAt.ToString("o"),
                IsExported = r.IsExported,
                ExportCount = r.ExportCount,
                LastExportedAt = r.LastExportedAt?.ToString("o")
            })
            .Cast<object>()
            .ToList();
    }

    /// <summary>
    /// Get prescriptions formatted for export
    /// Single Responsibility: Data transformation for prescriptions
    /// </summary>
    private async Task<List<object>> GetPrescriptionsForExport(
        Guid doctorId,
        DateTime? startDate,
        DateTime? endDate)
    {
        var prescriptions = new List<Prescription>();
        
        // Get all medical records for this doctor to find patient IDs
        var medicalRecords = await _medicalRecordService.GetByDoctorIdAsync(doctorId);
        var patientIds = medicalRecords.Select(r => r.PatientId).Distinct();
        
        // Get all prescriptions for these patients created by this doctor
        foreach (var patientId in patientIds)
        {
            var patientPrescriptions = await _prescriptionService.GetByPatientIdAsync(patientId);
            prescriptions.AddRange(patientPrescriptions.Where(p => p.DoctorId == doctorId));
        }
        
        return prescriptions
            .Where(p => IsWithinDateRange(p.CreatedAt, startDate, endDate))
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.MedicationName,
                p.Dosage,
                p.Frequency,
                p.Duration,
                p.Instructions,
                p.IsActive,
                Patient = new
                {
                    p.Patient?.Email,
                    p.Patient?.Phone
                },
                CreatedAt = p.CreatedAt.ToString("o"),
                UpdatedAt = p.UpdatedAt.ToString("o")
            })
            .Cast<object>()
            .ToList();
    }

    /// <summary>
    /// Get export summary statistics
    /// Single Responsibility: Calculate summary statistics
    /// </summary>
    private async Task<object> GetExportSummary(
        Guid doctorId,
        DateTime? startDate,
        DateTime? endDate)
    {
        var medicalRecords = await _medicalRecordService.GetByDoctorIdAsync(doctorId);
        var filteredRecords = medicalRecords
            .Where(r => IsWithinDateRange(r.RecordDate, startDate, endDate))
            .ToList();

        var prescriptions = new List<Prescription>();
        var patientIds = medicalRecords.Select(r => r.PatientId).Distinct();
        
        foreach (var patientId in patientIds)
        {
            var patientPrescriptions = await _prescriptionService.GetByPatientIdAsync(patientId);
            prescriptions.AddRange(patientPrescriptions.Where(p => p.DoctorId == doctorId));
        }

        var filteredPrescriptions = prescriptions
            .Where(p => IsWithinDateRange(p.CreatedAt, startDate, endDate))
            .ToList();

        return new
        {
            TotalRecords = filteredRecords.Count + filteredPrescriptions.Count,
            MedicalRecords = filteredRecords.Count,
            Prescriptions = filteredPrescriptions.Count,
            ActivePrescriptions = filteredPrescriptions.Count(p => p.IsActive),
            UniquePatients = filteredRecords.Select(r => r.PatientId).Distinct().Count(),
            RecordTypes = filteredRecords
                .GroupBy(r => r.RecordType)
                .Select(g => new { Type = g.Key, Count = g.Count() })
                .ToList()
        };
    }

    /// <summary>
    /// Check if date is within export range
    /// Utility method following Single Responsibility
    /// </summary>
    private bool IsWithinDateRange(DateTime date, DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && date < startDate.Value)
            return false;
        
        if (endDate.HasValue && date > endDate.Value)
            return false;
        
        return true;
    }

    /// <summary>
    /// Escape CSV special characters
    /// Utility method following Single Responsibility
    /// </summary>
    private string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        
        // Escape quotes and wrap in quotes if contains comma, quote, or newline
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        
        return value;
    }

    /// <summary>
    /// Truncate text to specified length
    /// Utility method following Single Responsibility
    /// </summary>
    private string? TruncateText(string? text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return text;
        
        if (text.Length <= maxLength)
            return text;
        
        return text.Substring(0, maxLength) + "...";
    }
}
