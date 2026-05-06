using Microsoft.AspNetCore.Components;
using ai_clinic.Models;
using ai_clinic.Services.Facades;

namespace ai_clinic.UI.Pages.Doctor;

public partial class Records : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private DoctorFacade DoctorFacade { get; set; } = null!;

    private bool isLoading = true;
    private string? errorMessage;
    private string currentFilter = "all";
    private string searchQuery = "";

    private List<MedicalRecord> medicalRecords = new();
    private List<Prescription> prescriptions = new();
    private RecordStatisticsData statistics = new();

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
}
