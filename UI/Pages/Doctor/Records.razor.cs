using Microsoft.AspNetCore.Components;
using ai_clinic.Models;
using ai_clinic.Services;

namespace ai_clinic.UI.Pages.Doctor;

public partial class Records : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private ConversationService ConversationService { get; set; } = null!;
    [Inject] private MedicalRecordService MedicalRecordService { get; set; } = null!;
    [Inject] private AuthStateService AuthStateService { get; set; } = null!;

    private class PatientConversationData
    {
        public Guid PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string PatientEmail { get; set; } = string.Empty;
        public byte[]? ProfilePhoto { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public List<Conversation> Conversations { get; set; } = new();
        public List<MedicalRecord> MedicalRecords { get; set; } = new();
        public int ConversationCount => Conversations.Count;
        public int MedicalRecordCount => MedicalRecords.Count;
    }

    private List<PatientConversationData> patientConversations = new();
    private Guid? selectedPatientId = null;
    private string activeTab = "conversations";
    private string searchQuery = string.Empty;
    private bool isLoading = true;
    private string errorMessage = string.Empty;
    private MedicalRecord? selectedRecord = null;
    private bool showRecordModal = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        isLoading = true;
        errorMessage = string.Empty;
        patientConversations.Clear();

        try
        {
            var currentUser = AuthStateService.CurrentUser;
            if (currentUser == null)
            {
                errorMessage = "User not authenticated";
                return;
            }

            // Get all conversations for this doctor
            var conversations = await ConversationService.GetByDoctorIdAsync(currentUser.Id);
            
            // Get all medical records created by this doctor
            var medicalRecords = await MedicalRecordService.GetByDoctorIdAsync(currentUser.Id);

            // Group by patient
            var patientGroups = conversations
                .GroupBy(c => c.PatientId)
                .Select(g => new PatientConversationData
                {
                    PatientId = g.Key,
                    PatientName = g.First().Patient?.PatientProfile?.FullName ?? "Unknown Patient",
                    PatientEmail = g.First().Patient?.Email ?? "N/A",
                    ProfilePhoto = g.First().Patient?.PatientProfile?.ProfilePhoto,
                    DateOfBirth = g.First().Patient?.PatientProfile?.DateOfBirth,
                    Conversations = g.ToList(),
                    MedicalRecords = medicalRecords.Where(mr => mr.PatientId == g.Key).ToList()
                })
                .OrderByDescending(p => p.Conversations.Max(c => c.LastMessageAt))
                .ToList();

            patientConversations = patientGroups;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading data: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private List<PatientConversationData> GetFilteredPatients()
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
            return patientConversations;

        var query = searchQuery.ToLower();
        return patientConversations
            .Where(p => p.PatientName.ToLower().Contains(query) || 
                       p.PatientEmail.ToLower().Contains(query))
            .ToList();
    }

    private void SelectPatient(Guid patientId)
    {
        selectedPatientId = patientId;
        activeTab = "conversations";
    }

    private async Task HandleSearch()
    {
        // Search is handled by GetFilteredPatients()
        await Task.CompletedTask;
    }

    private async Task RefreshData()
    {
        await LoadData();
    }

    private void ViewConversation(Guid conversationId)
    {
        Navigation.NavigateTo($"/doctor/consultation?conversationId={conversationId}");
    }

    private void ViewRecord(MedicalRecord record)
    {
        selectedRecord = record;
        showRecordModal = true;
    }

    private void CloseModals()
    {
        showRecordModal = false;
        selectedRecord = null;
    }

    private int CalculateAge(DateTime dateOfBirth)
    {
        var today = DateTime.Today;
        var age = today.Year - dateOfBirth.Year;
        if (dateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }

    private string GetStatusBadgeClass(ConversationStatus status)
    {
        return status switch
        {
            ConversationStatus.Active => "badge-visit",
            ConversationStatus.Closed => "badge-imaging",
            ConversationStatus.Archived => "badge-lab",
            _ => "badge-prescription"
        };
    }

    private string GetRecordIconClass(string recordType)
    {
        return recordType.ToLower() switch
        {
            "lab result" => "record-icon lab",
            "prescription" => "record-icon prescription",
            "imaging" => "record-icon imaging",
            _ => "record-icon visit"
        };
    }

    private string GetRecordIcon(string recordType)
    {
        return recordType.ToLower() switch
        {
            "lab result" => "flask-conical",
            "prescription" => "pill",
            "imaging" => "scan",
            _ => "file-text"
        };
    }

    private string GetRecordBadgeClass(string recordType)
    {
        return recordType.ToLower() switch
        {
            "lab result" => "badge-lab",
            "prescription" => "badge-prescription",
            "imaging" => "badge-imaging",
            _ => "badge-visit"
        };
    }
}
