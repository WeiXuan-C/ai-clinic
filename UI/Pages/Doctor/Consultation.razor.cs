using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ai_clinic.Models;
using ai_clinic.Services;
using ai_clinic.Services.Facades;
using ConversationListItem = ai_clinic.Services.ConversationListItem;

namespace ai_clinic.UI.Pages.Doctor;

/// <summary>
/// Doctor consultation page with real-time SignalR support
/// </summary>
public partial class Consultation : ComponentBase, IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private DoctorFacade DoctorFacade { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private SignalRConsultationService SignalRService { get; set; } = null!;
    [Inject] private IConfiguration Configuration { get; set; } = null!;
    [Inject] private DocumentService DocumentService { get; set; } = null!;

    private List<ConversationListItem> conversationList = [];
    private List<Message> messages = [];
    private Conversation? currentConversation;
    private string newMessage = "";
    private bool isTyping = false;
    private string? doctorPhotoUrl = null;
    private string? patientPhotoUrl = null;
    private Guid currentDoctorId;
    private Guid? lastConsultationNoteId;
    private string noteSymptoms = "";
    private string notePhysicalExam = "";
    private string noteDiagnosis = "";
    private string noteTreatmentPlan = "";
    private string noteFollowUp = "";
    private bool finalizeNote = false;
    private bool isSavingNote = false;
    private string prescriptionMedication = "";
    private string prescriptionDosage = "";
    private string prescriptionFrequency = "";
    private string prescriptionDuration = "";
    private string prescriptionInstructions = "";
    private bool isSavingPrescription = false;
    private string? workflowMessage;
    private string? workflowError;
    private bool _signalRInitialized = false;
    private Dictionary<Guid, List<Document>> messageDocuments = new();

    protected override async Task OnInitializedAsync()
    {
        if (!AuthFacade.IsAuthenticated || AuthFacade.CurrentUser?.Role != UserRole.Doctor)
        {
            Navigation.NavigateTo("/auth/signin");
            return;
        }

        currentDoctorId = AuthFacade.CurrentUser.Id;

        // Load doctor photo
        await LoadDoctorPhoto();

        // Initialize SignalR
        await InitializeSignalR();

        await LoadConversations();
    }

    /// <summary>
    /// Initialize SignalR connection and event handlers
    /// </summary>
    private async Task InitializeSignalR()
    {
        if (_signalRInitialized)
            return;

        try
        {
            var baseUrl = Navigation.BaseUri.TrimEnd('/');
            var hubUrl = $"{baseUrl}/consultationHub";

            Console.WriteLine($"[SignalR] Initializing connection to: {hubUrl}");

            // Initialize connection
            await SignalRService.InitializeAsync(hubUrl);

            // Register user
            await SignalRService.RegisterUserAsync(currentDoctorId);

            // Subscribe to events
            SignalRService.OnMessageReceived += HandleMessageReceived;
            SignalRService.OnUserTyping += HandleUserTyping;
            SignalRService.OnUserStoppedTyping += HandleUserStoppedTyping;
            SignalRService.OnConnected += HandleConnected;
            SignalRService.OnDisconnected += HandleDisconnected;
            SignalRService.OnReconnected += HandleReconnected;

            _signalRInitialized = true;
            Console.WriteLine("[SignalR] Initialization complete");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR] Initialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle incoming real-time messages
    /// </summary>
    private void HandleMessageReceived(MessageReceivedEventArgs args)
    {
        Console.WriteLine($"[SignalR] Message received: {args.MessageId} in conversation {args.ConversationId}");

        // Only process if it's for the current conversation
        if (currentConversation?.Id != args.ConversationId)
            return;

        // Don't add if it's our own message
        if (args.SenderId == currentDoctorId)
            return;

        // Check if message already exists
        if (messages.Any(m => m.Id == args.MessageId))
            return;

        // Add new message
        var message = new Message
        {
            Id = args.MessageId,
            ConversationId = args.ConversationId,
            SenderId = args.SenderId,
            SenderType = Enum.Parse<MessageSenderType>(args.SenderType),
            Content = args.Content,
            CreatedAt = args.CreatedAt,
            IsRead = args.IsRead
        };

        messages.Add(message);
        InvokeAsync(async () =>
        {
            StateHasChanged();
            await ScrollToBottom();
        });
    }

    /// <summary>
    /// Handle user typing notification
    /// </summary>
    private void HandleUserTyping(TypingEventArgs args)
    {
        if (currentConversation?.Id != args.ConversationId)
            return;

        // Don't show typing indicator for doctor's own typing
        if (args.UserRole == "Doctor")
            return;

        isTyping = true;
        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Handle user stopped typing notification
    /// </summary>
    private void HandleUserStoppedTyping(TypingEventArgs args)
    {
        if (currentConversation?.Id != args.ConversationId)
            return;

        isTyping = false;
        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Handle SignalR connected event
    /// </summary>
    private void HandleConnected()
    {
        Console.WriteLine("[SignalR] Connected");
        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Handle SignalR disconnected event
    /// </summary>
    private void HandleDisconnected(Exception? ex)
    {
        Console.WriteLine($"[SignalR] Disconnected: {ex?.Message}");
        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Handle SignalR reconnected event
    /// </summary>
    private void HandleReconnected()
    {
        Console.WriteLine("[SignalR] Reconnected");
        
        // Re-register user and rejoin conversation
        InvokeAsync(async () =>
        {
            await SignalRService.RegisterUserAsync(currentDoctorId);
            if (currentConversation != null)
            {
                await SignalRService.JoinConversationAsync(currentConversation.Id);
            }
            StateHasChanged();
        });
    }

    /// <summary>
    /// Loads doctor profile photo
    /// </summary>
    private async Task LoadDoctorPhoto()
    {
        try
        {
            var doctorProfile = AuthFacade.CurrentDoctorProfile;
            if (doctorProfile?.ProfilePhoto != null && doctorProfile.ProfilePhoto.Length > 0)
            {
                var base64 = Convert.ToBase64String(doctorProfile.ProfilePhoto);
                doctorPhotoUrl = $"data:image/jpeg;base64,{base64}";
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Failed to load doctor photo: {ex.Message}");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await JS.InvokeVoidAsync("initializeLucide");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Lucide initialization warning: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Loads all doctor consultations list
    /// </summary>
    private async Task LoadConversations()
    {
        conversationList = await DoctorFacade.GetDoctorConversationListAsync(currentDoctorId);

        // Load the first conversation if exists
        if (conversationList.Count > 0)
        {
            await LoadConversation(conversationList[0].Id);
        }
    }

    /// <summary>
    /// Loads specific consultation session
    /// </summary>
    private async Task LoadConversation(Guid conversationId)
    {
        try
        {
            // Leave previous conversation
            if (currentConversation != null && _signalRInitialized)
            {
                await SignalRService.LeaveConversationAsync(currentConversation.Id);
            }

            currentConversation = null;
            messages = [];
            isTyping = false;
            patientPhotoUrl = null;
            ClearClinicalForms();

            await InvokeAsync(StateHasChanged);
            await Task.Delay(50);

            var session = await DoctorFacade.GetDoctorConsultationSessionAsync(conversationId, currentDoctorId);

            currentConversation = session.Conversation;
            messages = session.Messages;

            // Join new conversation via SignalR
            if (_signalRInitialized)
            {
                await SignalRService.JoinConversationAsync(conversationId);
                Console.WriteLine($"[SignalR] Joined conversation: {conversationId}");
            }

            // Load patient photo
            await LoadPatientPhoto();

            // Load attachments for all messages
            await LoadMessageAttachments();

            await InvokeAsync(StateHasChanged);

            await Task.Delay(100);
            try
            {
                await JS.InvokeVoidAsync("initializeLucide");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DEBUG] Lucide re-initialization warning: {ex.Message}");
            }

            await ScrollToBottom();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading conversation: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads patient profile photo for current conversation
    /// </summary>
    private async Task LoadPatientPhoto()
    {
        try
        {
            if (currentConversation?.Patient?.PatientProfile?.ProfilePhoto != null &&
                currentConversation.Patient.PatientProfile.ProfilePhoto.Length > 0)
            {
                var base64 = Convert.ToBase64String(currentConversation.Patient.PatientProfile.ProfilePhoto);
                patientPhotoUrl = $"data:image/jpeg;base64,{base64}";
            }

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Failed to load patient photo: {ex.Message}");
        }
    }

    /// <summary>
    /// Sends message from doctor
    /// </summary>
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(newMessage) || currentConversation == null)
            return;

        var messageContent = newMessage;
        newMessage = "";
        StateHasChanged();

        try
        {
            var message = await DoctorFacade.SendDoctorMessageAsync(
                currentConversation.Id,
                currentDoctorId,
                messageContent
            );

            // Add message to local list (SignalR will also broadcast it)
            if (!messages.Any(m => m.Id == message.Id))
            {
                messages.Add(message);
                StateHasChanged();
                await ScrollToBottom();
            }

            // Notify stopped typing
            if (_signalRInitialized)
            {
                await SignalRService.NotifyStoppedTypingAsync(currentConversation.Id, "Doctor");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    private async Task SaveConsultationNote()
    {
        workflowMessage = null;
        workflowError = null;

        if (currentConversation == null)
            return;

        if (string.IsNullOrWhiteSpace(noteDiagnosis))
        {
            workflowError = "Diagnosis is required before saving consultation notes.";
            return;
        }

        isSavingNote = true;

        try
        {
            var note = await DoctorFacade.SaveConsultationNoteAsync(
                currentConversation.Id,
                currentDoctorId,
                currentConversation.PatientId,
                noteDiagnosis,
                noteSymptoms,
                notePhysicalExam,
                noteTreatmentPlan,
                noteFollowUp,
                finalizeNote);

            lastConsultationNoteId = note.Id;
            workflowMessage = finalizeNote
                ? "Consultation note saved and consultation closed."
                : "Consultation note saved.";

            if (finalizeNote)
            {
                await LoadConversations();
            }
        }
        catch (Exception ex)
        {
            workflowError = $"Failed to save consultation note: {ex.Message}";
        }
        finally
        {
            isSavingNote = false;
        }
    }

    private async Task CreatePrescription()
    {
        workflowMessage = null;
        workflowError = null;

        if (currentConversation == null)
            return;

        if (string.IsNullOrWhiteSpace(prescriptionMedication) ||
            string.IsNullOrWhiteSpace(prescriptionDosage) ||
            string.IsNullOrWhiteSpace(prescriptionFrequency))
        {
            workflowError = "Medication, dosage, and frequency are required to create a prescription.";
            return;
        }

        isSavingPrescription = true;

        try
        {
            await DoctorFacade.CreatePrescriptionAsync(
                currentConversation.Id,
                currentDoctorId,
                currentConversation.PatientId,
                lastConsultationNoteId,
                prescriptionMedication,
                prescriptionDosage,
                prescriptionFrequency,
                prescriptionDuration,
                prescriptionInstructions);

            workflowMessage = "Prescription created and stored.";
            prescriptionMedication = "";
            prescriptionDosage = "";
            prescriptionFrequency = "";
            prescriptionDuration = "";
            prescriptionInstructions = "";
        }
        catch (Exception ex)
        {
            workflowError = $"Failed to create prescription: {ex.Message}";
        }
        finally
        {
            isSavingPrescription = false;
        }
    }

    private void ClearClinicalForms()
    {
        lastConsultationNoteId = null;
        noteSymptoms = "";
        notePhysicalExam = "";
        noteDiagnosis = "";
        noteTreatmentPlan = "";
        noteFollowUp = "";
        finalizeNote = false;
        prescriptionMedication = "";
        prescriptionDosage = "";
        prescriptionFrequency = "";
        prescriptionDuration = "";
        prescriptionInstructions = "";
        workflowMessage = null;
        workflowError = null;
    }

    private async Task HandleKeyPress(Microsoft.AspNetCore.Components.Web.KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(newMessage))
        {
            await SendMessage();
        }
    }

    private async Task ScrollToBottom()
    {
        try
        {
            await JS.InvokeVoidAsync("scrollToBottom", "messages-container");
        }
        catch
        {
            // Ignore JS interop errors
        }
    }

    /// <summary>
    /// Loads attachments for all messages in current conversation
    /// </summary>
    private async Task LoadMessageAttachments()
    {
        messageDocuments.Clear();
        
        foreach (var message in messages)
        {
            if (!string.IsNullOrEmpty(message.DocumentReferences))
            {
                try
                {
                    var docIds = message.DocumentReferences
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(id => Guid.Parse(id.Trim()))
                        .ToList();

                    var docs = new List<Document>();
                    foreach (var docId in docIds)
                    {
                        var doc = await DocumentService.GetByIdAsync(docId);
                        if (doc != null)
                        {
                            docs.Add(doc);
                        }
                    }

                    if (docs.Any())
                    {
                        messageDocuments[message.Id] = docs;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] Failed to load attachments for message {message.Id}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Gets documents for a specific message
    /// </summary>
    private List<Document> GetMessageDocuments(Guid messageId)
    {
        return messageDocuments.TryGetValue(messageId, out var docs) ? docs : new List<Document>();
    }

    /// <summary>
    /// Checks if a document is an image
    /// </summary>
    private static bool IsImageDocument(Document doc)
    {
        return doc.MimeType?.StartsWith("image/") == true;
    }

    /// <summary>
    /// Gets base64 data URL for image display
    /// </summary>
    private static string GetImageDataUrl(Document doc)
    {
        if (doc.FileData != null && doc.FileData.Length > 0)
        {
            var base64 = Convert.ToBase64String(doc.FileData);
            return $"data:{doc.MimeType};base64,{base64}";
        }
        return string.Empty;
    }

    /// <summary>
    /// Downloads a document
    /// </summary>
    private async Task DownloadDocument(Document doc)
    {
        try
        {
            if (doc.FileData != null && doc.FileData.Length > 0)
            {
                var base64 = Convert.ToBase64String(doc.FileData);
                var dataUrl = $"data:{doc.MimeType};base64,{base64}";
                
                await JS.InvokeVoidAsync("downloadFile", doc.FileName, dataUrl);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ERROR] Failed to download document: {ex.Message}");
        }
    }

    /// <summary>
    /// Formats file size for display
    /// </summary>
    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string FormatTime(DateTime dateTime)
    {
        var now = DateTime.UtcNow;
        var diff = now - dateTime;

        if (diff.TotalMinutes < 1)
            return "Just now";
        else if (diff.TotalHours < 1)
            return $"{(int)diff.TotalMinutes}m ago";
        else if (diff.TotalDays < 1)
            return $"{(int)diff.TotalHours}h ago";
        else if (diff.TotalDays < 7)
            return $"{(int)diff.TotalDays}d ago";
        else
            return dateTime.ToString("MMM dd");
    }

    public async ValueTask DisposeAsync()
    {
        // Unsubscribe from SignalR events
        if (_signalRInitialized)
        {
            SignalRService.OnMessageReceived -= HandleMessageReceived;
            SignalRService.OnUserTyping -= HandleUserTyping;
            SignalRService.OnUserStoppedTyping -= HandleUserStoppedTyping;
            SignalRService.OnConnected -= HandleConnected;
            SignalRService.OnDisconnected -= HandleDisconnected;
            SignalRService.OnReconnected -= HandleReconnected;

            // Leave current conversation
            if (currentConversation != null)
            {
                await SignalRService.LeaveConversationAsync(currentConversation.Id);
            }
        }
    }
}
