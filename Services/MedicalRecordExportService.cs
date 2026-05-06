using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using ai_clinic.Models;
using Document = ai_clinic.Models.Document;

namespace ai_clinic.Services;

/// <summary>
/// Service for exporting medical records to PDF format
/// </summary>
public class MedicalRecordExportService
{
    private readonly MedicalRecordService _medicalRecordService;
    private readonly ConsultationService _consultationService;
    private readonly PrescriptionService _prescriptionService;
    private readonly PatientProfileService _patientProfileService;
    private readonly UserService _userService;

    public MedicalRecordExportService(
        MedicalRecordService medicalRecordService,
        ConsultationService consultationService,
        PrescriptionService prescriptionService,
        PatientProfileService patientProfileService,
        UserService userService)
    {
        _medicalRecordService = medicalRecordService;
        _consultationService = consultationService;
        _prescriptionService = prescriptionService;
        _patientProfileService = patientProfileService;
        _userService = userService;
    }

    /// <summary>
    /// Export patient's medical records to PDF
    /// </summary>
    public async Task<byte[]> ExportMedicalRecordsToPdfAsync(
        Guid patientId, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        // Fetch patient data
        var patient = await _userService.GetByIdAsync(patientId);
        if (patient == null)
            throw new Exception("Patient not found");

        var patientProfile = await _patientProfileService.GetByUserIdAsync(patientId);
        
        // Fetch medical records
        var medicalRecords = await _medicalRecordService.GetByPatientIdAsync(patientId);
        
        // Filter by date range if provided
        if (startDate.HasValue)
        {
            medicalRecords = medicalRecords.Where(r => r.RecordDate >= startDate.Value).ToList();
        }
        
        if (endDate.HasValue)
        {
            medicalRecords = medicalRecords.Where(r => r.RecordDate <= endDate.Value).ToList();
        }

        // Fetch consultation notes
        var consultationNotes = await _consultationService.GetByPatientIdAsync(patientId);
        
        // Fetch prescriptions
        var prescriptions = await _prescriptionService.GetByPatientIdAsync(patientId);

        // Generate PDF
        var document = QuestPDF.Fluent.Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Header()
                    .Height(100)
                    .Background(Colors.Blue.Lighten3)
                    .Padding(20)
                    .Column(column =>
                    {
                        column.Item().Text("AI Clinic - Medical Records")
                            .FontSize(24)
                            .Bold()
                            .FontColor(Colors.Blue.Darken2);
                        
                        column.Item().Text($"Export Date: {DateTime.Now:yyyy-MM-dd HH:mm}")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });

                page.Content()
                    .PaddingVertical(1, Unit.Centimetre)
                    .Column(column =>
                    {
                        // Patient Information Section
                        column.Item().Element(container => RenderPatientInfo(container, patient, patientProfile));
                        
                        column.Item().PaddingTop(20);

                        // Medical Records Section
                        if (medicalRecords.Any())
                        {
                            column.Item().Element(container => RenderMedicalRecords(container, medicalRecords));
                            column.Item().PaddingTop(20);
                        }

                        // Consultation Notes Section
                        if (consultationNotes.Any())
                        {
                            column.Item().Element(container => RenderConsultationNotes(container, consultationNotes));
                            column.Item().PaddingTop(20);
                        }

                        // Prescriptions Section
                        if (prescriptions.Any())
                        {
                            column.Item().Element(container => RenderPrescriptions(container, prescriptions));
                        }
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

        // Update export statistics
        foreach (var record in medicalRecords)
        {
            await _medicalRecordService.UpdateExportStatisticsAsync(record.Id);
        }

        return document.GeneratePdf();
    }

    private void RenderPatientInfo(IContainer container, User patient, PatientProfile? profile)
    {
        container.Column(column =>
        {
            column.Item().Text("Patient Information")
                .FontSize(16)
                .Bold()
                .FontColor(Colors.Blue.Darken1);
            
            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
            
            column.Item().PaddingTop(10).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Name: {profile?.FullName ?? "N/A"}").FontSize(11);
                    col.Item().Text($"Email: {patient.Email}").FontSize(11);
                    col.Item().Text($"Phone: {patient.Phone ?? "N/A"}").FontSize(11);
                });
                
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text($"Date of Birth: {profile?.DateOfBirth?.ToString("yyyy-MM-dd") ?? "N/A"}").FontSize(11);
                    col.Item().Text($"Gender: {profile?.Gender ?? "N/A"}").FontSize(11);
                    col.Item().Text($"Blood Type: {profile?.BloodType ?? "N/A"}").FontSize(11);
                });
            });

            if (profile?.Allergies?.Length > 0)
            {
                column.Item().PaddingTop(10).Text($"Allergies: {string.Join(", ", profile.Allergies)}")
                    .FontSize(11)
                    .FontColor(Colors.Red.Darken1);
            }

            if (profile?.ChronicConditions?.Length > 0)
            {
                column.Item().PaddingTop(5).Text($"Chronic Conditions: {string.Join(", ", profile.ChronicConditions)}")
                    .FontSize(11);
            }
        });
    }

    private void RenderMedicalRecords(IContainer container, List<MedicalRecord> records)
    {
        container.Column(column =>
        {
            column.Item().Text("Medical Records")
                .FontSize(16)
                .Bold()
                .FontColor(Colors.Blue.Darken1);
            
            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            foreach (var record in records.OrderByDescending(r => r.RecordDate))
            {
                column.Item().PaddingTop(15).Column(recordColumn =>
                {
                    recordColumn.Item().Row(row =>
                    {
                        row.RelativeItem().Text(record.Title)
                            .FontSize(13)
                            .Bold();
                        
                        row.AutoItem().Text(record.RecordDate.ToString("yyyy-MM-dd"))
                            .FontSize(11)
                            .FontColor(Colors.Grey.Darken1);
                    });

                    recordColumn.Item().PaddingTop(5).Text($"Type: {record.RecordType}")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);

                    if (!string.IsNullOrEmpty(record.DiagnosisCode))
                    {
                        recordColumn.Item().PaddingTop(5).Text($"Diagnosis Code: {record.DiagnosisCode}")
                            .FontSize(10);
                    }

                    if (!string.IsNullOrEmpty(record.DiagnosisDescription))
                    {
                        recordColumn.Item().PaddingTop(5).Text(record.DiagnosisDescription)
                            .FontSize(11);
                    }

                    recordColumn.Item().PaddingTop(5).Text(record.Content)
                        .FontSize(11);

                    recordColumn.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten3);
                });
            }
        });
    }

    private void RenderConsultationNotes(IContainer container, List<ConsultationNote> notes)
    {
        container.Column(column =>
        {
            column.Item().Text("Consultation Notes")
                .FontSize(16)
                .Bold()
                .FontColor(Colors.Blue.Darken1);
            
            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            foreach (var note in notes.OrderByDescending(n => n.CreatedAt))
            {
                column.Item().PaddingTop(15).Column(noteColumn =>
                {
                    noteColumn.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"Consultation - {note.CreatedAt:yyyy-MM-dd}")
                            .FontSize(13)
                            .Bold();
                        
                        if (note.IsFinalized)
                        {
                            row.AutoItem().Text("Finalized")
                                .FontSize(10)
                                .FontColor(Colors.Green.Darken1);
                        }
                    });

                    if (note.Symptoms?.Length > 0)
                    {
                        noteColumn.Item().PaddingTop(5).Text($"Symptoms: {string.Join(", ", note.Symptoms)}")
                            .FontSize(11);
                    }

                    if (!string.IsNullOrEmpty(note.PhysicalExamination))
                    {
                        noteColumn.Item().PaddingTop(5).Text($"Physical Examination: {note.PhysicalExamination}")
                            .FontSize(11);
                    }

                    noteColumn.Item().PaddingTop(5).Text($"Diagnosis: {note.Diagnosis}")
                        .FontSize(11)
                        .Bold();

                    if (!string.IsNullOrEmpty(note.TreatmentPlan))
                    {
                        noteColumn.Item().PaddingTop(5).Text($"Treatment Plan: {note.TreatmentPlan}")
                            .FontSize(11);
                    }

                    if (!string.IsNullOrEmpty(note.FollowUpInstructions))
                    {
                        noteColumn.Item().PaddingTop(5).Text($"Follow-up: {note.FollowUpInstructions}")
                            .FontSize(11);
                    }

                    noteColumn.Item().PaddingTop(10).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten3);
                });
            }
        });
    }

    private void RenderPrescriptions(IContainer container, List<Prescription> prescriptions)
    {
        container.Column(column =>
        {
            column.Item().Text("Prescriptions")
                .FontSize(16)
                .Bold()
                .FontColor(Colors.Blue.Darken1);
            
            column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

            var activePrescriptions = prescriptions.Where(p => p.IsActive).OrderByDescending(p => p.CreatedAt);

            foreach (var prescription in activePrescriptions)
            {
                column.Item().PaddingTop(10).Row(row =>
                {
                    row.RelativeItem(3).Column(col =>
                    {
                        col.Item().Text(prescription.MedicationName)
                            .FontSize(12)
                            .Bold();
                        
                        col.Item().Text($"Dosage: {prescription.Dosage}")
                            .FontSize(10);
                    });

                    row.RelativeItem(2).Column(col =>
                    {
                        col.Item().Text($"Frequency: {prescription.Frequency}")
                            .FontSize(10);
                        
                        col.Item().Text($"Duration: {prescription.Duration ?? "Ongoing"}")
                            .FontSize(10);
                    });

                    row.RelativeItem(2).Text(prescription.CreatedAt.ToString("yyyy-MM-dd"))
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);
                });

                if (!string.IsNullOrEmpty(prescription.Instructions))
                {
                    column.Item().PaddingTop(5).Text($"Instructions: {prescription.Instructions}")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken2);
                }

                column.Item().PaddingTop(8).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten3);
            }
        });
    }
}
