using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ai_clinic.Models;

namespace ai_clinic.Services;

/// <summary>
/// Service for generating comprehensive doctor performance and analytics reports
/// Strategy Pattern: Different report generation strategies for doctor analytics
/// </summary>
public class DoctorReportExportService
{
    private readonly MedicalRecordService _medicalRecordService;
    private readonly PrescriptionService _prescriptionService;
    private readonly DoctorProfileService _doctorProfileService;
    private readonly PatientProfileService _patientProfileService;
    private readonly UserService _userService;

    public DoctorReportExportService(
        MedicalRecordService medicalRecordService,
        PrescriptionService prescriptionService,
        DoctorProfileService doctorProfileService,
        PatientProfileService patientProfileService,
        UserService userService)
    {
        _medicalRecordService = medicalRecordService;
        _prescriptionService = prescriptionService;
        _doctorProfileService = doctorProfileService;
        _patientProfileService = patientProfileService;
        _userService = userService;
    }

    /// <summary>
    /// Generate comprehensive doctor analytics report as PDF
    /// Includes statistics, trends, and detailed analysis
    /// </summary>
    public async Task<byte[]> GenerateDoctorAnalyticsReportAsync(
        Guid doctorId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        // Fetch doctor information
        var doctor = await _userService.GetByIdAsync(doctorId);
        if (doctor == null)
            throw new Exception("Doctor not found");

        var doctorProfile = await _doctorProfileService.GetByUserIdAsync(doctorId);
        
        // Fetch all records created by this doctor
        var allMedicalRecords = await _medicalRecordService.GetByDoctorIdAsync(doctorId);
        
        // Apply date filter
        var medicalRecords = allMedicalRecords
            .Where(r => IsWithinDateRange(r.RecordDate, startDate, endDate))
            .OrderByDescending(r => r.RecordDate)
            .ToList();

        // Fetch prescriptions
        var allPrescriptions = await GetDoctorPrescriptions(doctorId);
        var prescriptions = allPrescriptions
            .Where(p => IsWithinDateRange(p.CreatedAt, startDate, endDate))
            .ToList();

        // Calculate analytics
        var analytics = CalculateAnalytics(medicalRecords, prescriptions);

        // Generate PDF with simple layout
        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(10));

                // Header
                page.Header().Height(100).Background(Colors.Blue.Darken2).Padding(15).Column(column =>
                {
                    column.Item().Text("Doctor Analytics Report").FontSize(22).Bold().FontColor(Colors.White);
                    column.Item().Text($"Dr. {doctorProfile?.FullName ?? doctor.Email}").FontSize(14).FontColor(Colors.White);
                    column.Item().Text($"Specialization: {doctorProfile?.PrimarySpecialization ?? "General"}").FontSize(10).FontColor(Colors.White);
                    
                    var dateRangeText = startDate.HasValue || endDate.HasValue
                        ? $"{startDate?.ToString("MMM dd, yyyy") ?? "All"} - {endDate?.ToString("MMM dd, yyyy") ?? "Present"}"
                        : "All Time";
                    column.Item().Text($"Period: {dateRangeText}").FontSize(9).FontColor(Colors.Grey.Lighten2);
                });

                // Content
                page.Content().Column(column =>
                {
                    column.Spacing(15);

                    // Summary Stats in Simple Table
                    column.Item().PaddingTop(10).Element(c => RenderSimpleStats(c, analytics, medicalRecords, prescriptions));

                    // Record Types
                    column.Item().Element(c => RenderSimpleRecordTypes(c, analytics));

                    // Top Diagnoses
                    if (medicalRecords.Any(r => !string.IsNullOrEmpty(r.DiagnosisDescription)))
                    {
                        column.Item().Element(c => RenderSimpleDiagnoses(c, medicalRecords));
                    }

                    // Top Medications
                    if (prescriptions.Any())
                    {
                        column.Item().Element(c => RenderSimpleMedications(c, prescriptions));
                    }
                });

                // Footer
                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span("Page ");
                    x.CurrentPageNumber();
                    x.Span(" of ");
                    x.TotalPages();
                });
            });
        });

        return document.GeneratePdf();
    }

    private void RenderSimpleStats(IContainer container, DoctorAnalytics analytics, List<MedicalRecord> records, List<Prescription> prescriptions)
    {
        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("Performance Summary").FontSize(14).Bold();
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                // Row 1
                table.Cell().Padding(5).Text("Total Records:").FontSize(10);
                table.Cell().Padding(5).Text(analytics.TotalRecords.ToString()).FontSize(10).Bold();

                // Row 2
                table.Cell().Padding(5).Text("Unique Patients:").FontSize(10);
                table.Cell().Padding(5).Text(analytics.TotalPatients.ToString()).FontSize(10).Bold();

                // Row 3
                table.Cell().Padding(5).Text("Total Prescriptions:").FontSize(10);
                table.Cell().Padding(5).Text(analytics.TotalPrescriptions.ToString()).FontSize(10).Bold();

                // Row 4
                table.Cell().Padding(5).Text("Avg Records/Patient:").FontSize(10);
                table.Cell().Padding(5).Text(analytics.AverageRecordsPerPatient.ToString("F1")).FontSize(10).Bold();

                // Row 5
                table.Cell().Padding(5).Text("Active Prescriptions:").FontSize(10);
                table.Cell().Padding(5).Text(prescriptions.Count(p => p.IsActive).ToString()).FontSize(10).Bold();

                // Row 6
                table.Cell().Padding(5).Text("Exported Records:").FontSize(10);
                table.Cell().Padding(5).Text(records.Count(r => r.IsExported).ToString()).FontSize(10).Bold();
            });
        });
    }

    private void RenderSimpleRecordTypes(IContainer container, DoctorAnalytics analytics)
    {
        if (!analytics.RecordTypeDistribution.Any())
            return;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("Record Distribution").FontSize(14).Bold();
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(3);
                    columns.RelativeColumn(1);
                    columns.RelativeColumn(1);
                });

                // Header
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Type").FontSize(9).Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Count").FontSize(9).Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("%").FontSize(9).Bold();

                // Data
                foreach (var item in analytics.RecordTypeDistribution.OrderByDescending(x => x.Value))
                {
                    var percentage = (item.Value / (double)analytics.TotalRecords) * 100;
                    
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Key).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Value.ToString()).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text($"{percentage:F1}%").FontSize(9);
                }
            });
        });
    }

    private void RenderSimpleDiagnoses(IContainer container, List<MedicalRecord> records)
    {
        var diagnoses = records
            .Where(r => !string.IsNullOrEmpty(r.DiagnosisDescription))
            .GroupBy(r => r.DiagnosisDescription)
            .Select(g => new { Diagnosis = g.Key!, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToList();

        if (!diagnoses.Any())
            return;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("Top 10 Diagnoses").FontSize(14).Bold();
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn();
                    columns.ConstantColumn(50);
                });

                // Header
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("#").FontSize(9).Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Diagnosis").FontSize(9).Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Cases").FontSize(9).Bold();

                // Data
                int rank = 1;
                foreach (var item in diagnoses)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(rank.ToString()).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Diagnosis).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Count.ToString()).FontSize(9).Bold();
                    rank++;
                }
            });
        });
    }

    private void RenderSimpleMedications(IContainer container, List<Prescription> prescriptions)
    {
        var medications = prescriptions
            .GroupBy(p => p.MedicationName)
            .Select(g => new { Medication = g.Key, Total = g.Count(), Active = g.Count(p => p.IsActive) })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToList();

        if (!medications.Any())
            return;

        container.Border(1).BorderColor(Colors.Grey.Lighten2).Padding(10).Column(column =>
        {
            column.Item().Text("Top 10 Medications").FontSize(14).Bold();
            column.Item().PaddingTop(10).Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);
                    columns.RelativeColumn();
                    columns.ConstantColumn(50);
                    columns.ConstantColumn(50);
                });

                // Header
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("#").FontSize(9).Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Medication").FontSize(9).Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Total").FontSize(9).Bold();
                table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("Active").FontSize(9).Bold();

                // Data
                int rank = 1;
                foreach (var item in medications)
                {
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(rank.ToString()).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Medication).FontSize(9);
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Total.ToString()).FontSize(9).Bold();
                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(item.Active.ToString()).FontSize(9);
                    rank++;
                }
            });
        });
    }

    private DoctorAnalytics CalculateAnalytics(List<MedicalRecord> records, List<Prescription> prescriptions)
    {
        var uniquePatients = records.Select(r => r.PatientId).Distinct().ToList();
        
        var recordTypeDistribution = records
            .GroupBy(r => r.RecordType ?? "Unspecified")
            .ToDictionary(g => g.Key, g => g.Count());

        var mostCommonType = recordTypeDistribution.Any() 
            ? recordTypeDistribution.OrderByDescending(x => x.Value).First()
            : new KeyValuePair<string, int>("N/A", 0);

        return new DoctorAnalytics
        {
            TotalRecords = records.Count,
            TotalPatients = uniquePatients.Count,
            TotalPrescriptions = prescriptions.Count,
            AverageRecordsPerPatient = uniquePatients.Count > 0 ? records.Count / (double)uniquePatients.Count : 0,
            RecordTypeDistribution = recordTypeDistribution,
            MostCommonRecordType = mostCommonType.Key,
            MostCommonRecordTypePercentage = records.Count > 0 ? (mostCommonType.Value / (double)records.Count) * 100 : 0,
            AveragePatientAge = 0 // Would need patient profiles to calculate
        };
    }

    private async Task<List<Prescription>> GetDoctorPrescriptions(Guid doctorId)
    {
        var medicalRecords = await _medicalRecordService.GetByDoctorIdAsync(doctorId);
        var patientIds = medicalRecords.Select(r => r.PatientId).Distinct();
        
        var prescriptions = new List<Prescription>();
        foreach (var patientId in patientIds)
        {
            var patientPrescriptions = await _prescriptionService.GetByPatientIdAsync(patientId);
            prescriptions.AddRange(patientPrescriptions.Where(p => p.DoctorId == doctorId));
        }
        
        return prescriptions;
    }

    private bool IsWithinDateRange(DateTime date, DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && date < startDate.Value)
            return false;
        
        if (endDate.HasValue && date > endDate.Value)
            return false;
        
        return true;
    }
}

public class DoctorAnalytics
{
    public int TotalRecords { get; set; }
    public int TotalPatients { get; set; }
    public int TotalPrescriptions { get; set; }
    public double AverageRecordsPerPatient { get; set; }
    public Dictionary<string, int> RecordTypeDistribution { get; set; } = new();
    public string MostCommonRecordType { get; set; } = "";
    public double MostCommonRecordTypePercentage { get; set; }
    public double AveragePatientAge { get; set; }
}
