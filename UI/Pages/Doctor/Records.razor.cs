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
    private string errorMessage = string.Empty;
    private MedicalRecord? selectedRecord = null;
    private bool showRecordModal = false;

    private bool _hasInitialized = false;

    private string GetDisplayName(string? fullName, string? email)
    {
        // If full name exists and is not empty, use it
        if (!string.IsNullOrWhiteSpace(fullName))
            return fullName;
        
        // If email exists, extract the part before @ symbol
        if (!string.IsNullOrWhiteSpace(email))
        {
            var atIndex = email.IndexOf('@');
            if (atIndex > 0)
            {
                var username = email.Substring(0, atIndex);
                // Capitalize first letter
                return char.ToUpper(username[0]) + username.Substring(1);
            }
        }
        
        // Fallback
        return "Patient";
    }

    protected override async Task OnInitializedAsync()
    {
        if (_hasInitialized)
        {
            Console.WriteLine("[Doctor Records] OnInitializedAsync skipped - already initialized");
            return;
        }
        
        _hasInitialized = true;
        Console.WriteLine("[Doctor Records] OnInitializedAsync started");
        Console.WriteLine($"[Doctor Records] Thread ID: {Environment.CurrentManagedThreadId}");
        
        try
        {
            Console.WriteLine("[Doctor Records] About to call LoadData");
            await LoadData();
            Console.WriteLine("[Doctor Records] LoadData completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Doctor Records] OnInitializedAsync error: {ex.Message}");
            Console.WriteLine($"[Doctor Records] Stack trace: {ex.StackTrace}");
            errorMessage = $"Initialization error: {ex.Message}";
            StateHasChanged();
        }
        
        Console.WriteLine("[Doctor Records] OnInitializedAsync completed");
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            Console.WriteLine("[Doctor Records] OnAfterRenderAsync - first render");
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task LoadData()
    {
        Console.WriteLine("[Doctor Records] LoadData started");
        errorMessage = string.Empty;
        patientConversations.Clear();
        
        // Force UI update to show loading state
        StateHasChanged();

        try
        {
            var currentUser = AuthStateService.CurrentUser;
            Console.WriteLine($"[Doctor Records] Current user: {currentUser?.Email ?? "null"}");
            
            if (currentUser == null)
            {
                errorMessage = "User not authenticated";
                Console.WriteLine("[Doctor Records] User not authenticated");
                StateHasChanged();
                return;
            }

            Console.WriteLine($"[Doctor Records] Fetching conversations for doctor ID: {currentUser.Id}");
            
            // Use Task.Run to prevent blocking the UI thread
            var conversations = await Task.Run(async () => 
            {
                try
                {
                    return await ConversationService.GetByDoctorIdAsync(currentUser.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Doctor Records] Error fetching conversations: {ex.Message}");
                    throw;
                }
            });
            
            Console.WriteLine($"[Doctor Records] Fetched {conversations?.Count ?? 0} conversations");
            
            Console.WriteLine($"[Doctor Records] Fetching medical records for doctor ID: {currentUser.Id}");
            
            var medicalRecords = await Task.Run(async () =>
            {
                try
                {
                    return await MedicalRecordService.GetByDoctorIdAsync(currentUser.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Doctor Records] Error fetching medical records: {ex.Message}");
                    throw;
                }
            });
            
            Console.WriteLine($"[Doctor Records] Fetched {medicalRecords?.Count ?? 0} medical records");

            if (conversations == null || !conversations.Any())
            {
                Console.WriteLine("[Doctor Records] No conversations found");
                patientConversations = new List<PatientConversationData>();
                StateHasChanged();
                return;
            }

            Console.WriteLine("[Doctor Records] Grouping conversations by patient");
            // Group by patient with null safety
            var patientGroups = conversations
                .Where(c => c.Patient != null) // Filter out conversations without patient data
                .GroupBy(c => c.PatientId)
                .Select(g => {
                    var patient = g.First().Patient;
                    var fullName = patient?.PatientProfile?.FullName;
                    var email = patient?.Email ?? "N/A";
                    
                    return new PatientConversationData
                    {
                        PatientId = g.Key,
                        PatientName = GetDisplayName(fullName, email),
                        PatientEmail = email,
                        ProfilePhoto = patient?.PatientProfile?.ProfilePhoto,
                        DateOfBirth = patient?.PatientProfile?.DateOfBirth,
                        Conversations = g.ToList(),
                        MedicalRecords = medicalRecords?.Where(mr => mr.PatientId == g.Key).ToList() ?? new List<MedicalRecord>()
                    };
                })
                .OrderByDescending(p => p.Conversations.Any() ? p.Conversations.Max(c => c.LastMessageAt) : DateTime.MinValue)
                .ToList();

            patientConversations = patientGroups;
            Console.WriteLine($"[Doctor Records] Grouped into {patientConversations.Count} patient groups");
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading data: {ex.Message}";
            Console.WriteLine($"[Doctor Records] Error in LoadData: {ex.Message}");
            Console.WriteLine($"[Doctor Records] Stack trace: {ex.StackTrace}");
        }
        finally
        {
            Console.WriteLine("[Doctor Records] LoadData completed (finally block)");
            StateHasChanged();
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

    private string GetRecordIconEmoji(string recordType)
    {
        return recordType.ToLower() switch
        {
            "lab result" => "🧪",
            "prescription" => "💊",
            "imaging" => "🔬",
            _ => "📄"
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
