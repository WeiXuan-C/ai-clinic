using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.Forms;
using ai_clinic.Models;
using ai_clinic.Services;
using ai_clinic.Services.Facades;
using ConversationListItem = ai_clinic.Services.ConversationListItem;
using DoctorListItem = ai_clinic.Services.DoctorListItem;

namespace ai_clinic.UI.Pages.Patient;

/// <summary>
/// Patient consultation page - Uses Facade Pattern to simplify complex interactions
/// </summary>
public partial class Consultation : ComponentBase, IAsyncDisposable
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private ConsultationFacade ConsultationFacade { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;
    [Inject] private DocumentService DocumentService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private SignalRConsultationService SignalRService { get; set; } = null!;

    private List<ConversationListItem> conversationList = [];
    private List<Message> messages = [];
    private Conversation? currentConversation;
    private string newMessage = "";
    private bool isTyping = false;
    private bool isAiMode = true;
    private bool showNewConversationModal = false;
    private bool isLoadingDoctors = false;
    private List<DoctorListItem> availableDoctors = [];
    private List<DoctorListItem> filteredDoctors = [];
    private string _doctorSearchQuery = "";
    private bool showModelSelectorModal = false;
    private List<AiModelInfo> availableModels = [];
    private string currentModelKey = "owl-alpha";
    private Guid? selectedDoctorId = null;
    private List<RecommendedDoctorItem> recommendedDoctors = new();
    private bool showRecommendedDoctors = false;
    private bool isLoadingRecommendations = false;
    private bool showAllRecommendedDoctorsModal = false;
    private Guid? selectedRecommendedDoctorId = null;
    private bool _iconsInitialized = false;
    private string? patientPhotoUrl = null;
    private List<AttachmentFile> attachments = new();
    private bool isUploadingFile = false;
    private Dictionary<Guid, List<Document>> messageDocuments = new();
    private Guid currentPatientId;
    private bool _signalRInitialized = false;
    
    private string DoctorSearchQuery
    {
        get => _doctorSearchQuery;
        set
        {
            _doctorSearchQuery = value;
            FilterDoctors();
        }
    }

    protected override async Task OnInitializedAsync()
    {
        if (!AuthFacade.IsAuthenticated || AuthFacade.CurrentUser?.Role != UserRole.Patient)
        {
            Navigation.NavigateTo("/auth/signin");
            return;
        }

        currentPatientId = AuthFacade.CurrentUser.Id;

        // Load patient photo
        await LoadPatientPhoto();

        // Initialize SignalR
        await InitializeSignalR();

        // Load available AI models
        LoadAvailableModels();
        
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

            Console.WriteLine($"[SignalR Patient] Initializing connection to: {hubUrl}");

            // Initialize connection
            await SignalRService.InitializeAsync(hubUrl);

            // Register user
            await SignalRService.RegisterUserAsync(currentPatientId);

            // Subscribe to events
            SignalRService.OnMessageReceived += HandleMessageReceived;
            SignalRService.OnUserTyping += HandleUserTyping;
            SignalRService.OnUserStoppedTyping += HandleUserStoppedTyping;
            SignalRService.OnConnected += HandleConnected;
            SignalRService.OnDisconnected += HandleDisconnected;
            SignalRService.OnReconnected += HandleReconnected;

            _signalRInitialized = true;
            Console.WriteLine("[SignalR Patient] Initialization complete");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SignalR Patient] Initialization failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Handle incoming real-time messages
    /// </summary>
    private void HandleMessageReceived(MessageReceivedEventArgs args)
    {
        Console.WriteLine($"[SignalR Patient] Message received: {args.MessageId} in conversation {args.ConversationId}");

        // Only process if it's for the current conversation
        if (currentConversation?.Id != args.ConversationId)
            return;

        // Don't add if it's our own message
        if (args.SenderId == currentPatientId)
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

        // Don't show typing indicator for patient's own typing
        if (args.UserRole == "Patient")
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
        Console.WriteLine("[SignalR Patient] Connected");
        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Handle SignalR disconnected event
    /// </summary>
    private void HandleDisconnected(Exception? ex)
    {
        Console.WriteLine($"[SignalR Patient] Disconnected: {ex?.Message}");
        InvokeAsync(StateHasChanged);
    }

    /// <summary>
    /// Handle SignalR reconnected event
    /// </summary>
    private void HandleReconnected()
    {
        Console.WriteLine("[SignalR Patient] Reconnected");
        
        // Re-register user and rejoin conversation
        InvokeAsync(async () =>
        {
            await SignalRService.RegisterUserAsync(currentPatientId);
            if (currentConversation != null)
            {
                await SignalRService.JoinConversationAsync(currentConversation.Id);
            }
            StateHasChanged();
        });
    }

    /// <summary>
    /// Loads patient profile photo
    /// </summary>
    private async Task LoadPatientPhoto()
    {
        try
        {
            var patientProfile = AuthFacade.CurrentPatientProfile;
            if (patientProfile?.ProfilePhoto != null && patientProfile.ProfilePhoto.Length > 0)
            {
                // Convert byte array to base64 data URL
                var base64 = Convert.ToBase64String(patientProfile.ProfilePhoto);
                patientPhotoUrl = $"data:image/jpeg;base64,{base64}";
            }
            
            await Task.CompletedTask; // Make it async for consistency
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DEBUG] Failed to load patient photo: {ex.Message}");
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _iconsInitialized = true;
            try
            {
                // Initialize Lucide icons using the global helper function
                await JS.InvokeVoidAsync("initializeLucide");
            }
            catch (Exception ex)
            {
                // Ignore if lucide is not available
                Console.WriteLine($"[DEBUG] Lucide initialization warning: {ex.Message}");
            }
        }
        // Don't re-initialize icons on every render during streaming
        // This causes DOM manipulation conflicts
    }

    /// <summary>
    /// Loads available AI models list
    /// Uses Facade to get model information
    /// </summary>
    private void LoadAvailableModels()
    {
        availableModels = ConsultationFacade.GetAvailableAiModels();
        currentModelKey = availableModels.Count > 0 ? availableModels[0].Key : "owl-alpha";
    }

    /// <summary>
    /// Gets model description (moved to Facade)
    /// </summary>
    private static string GetModelDescription(string modelKey)
    {
        return modelKey switch
        {
            "owl-alpha" => "High-performance reasoning model, best for complex medical analysis",
            "gemma-4" => "Google's powerful open-source model with strong general capabilities",
            "minimax" => "Excellent multilingual support and natural conversation",
            "nemotron" => "NVIDIA's advanced model for technical tasks",
            "qianfan-ocr" => "Specialized in document analysis and OCR",
            _ => "AI model for medical consultation"
        };
    }

    /// <summary>
    /// Loads all patient consultations list
    /// Uses Facade to simplify the call
    /// </summary>
    private async Task LoadConversations()
    {
        conversationList = await ConsultationFacade.GetPatientConsultationsAsync(AuthFacade.CurrentUser!.Id);
        
        // Load the first conversation if exists
        if (conversationList.Any())
        {
            await LoadConversation(conversationList.First().Id);
        }
    }

    /// <summary>
    /// Loads specific consultation session
    /// Uses Facade to get all needed data at once (conversation, messages, doctor info, mark as read)
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

            // Clear current state first to prevent overlapping
            currentConversation = null;
            messages = [];
            isTyping = false;
            showRecommendedDoctors = false;
            recommendedDoctors = [];
            
            // Force UI update to clear old content
            await InvokeAsync(StateHasChanged);
            
            // Small delay to let DOM fully update before loading new content
            await Task.Delay(50);
            
            var session = await ConsultationFacade.GetConsultationSessionAsync(
                conversationId, 
                AuthFacade.CurrentUser!.Id,
                UserRole.Patient
            );

            currentConversation = session.Conversation;
            messages = session.Messages;
            isAiMode = session.IsAiConsultation;

            // Join new conversation via SignalR
            if (_signalRInitialized)
            {
                await SignalRService.JoinConversationAsync(conversationId);
                Console.WriteLine($"[SignalR Patient] Joined conversation: {conversationId}");
            }
            
            // Load attachments for all messages
            await LoadMessageAttachments();
            
            await InvokeAsync(StateHasChanged);
            
            // Re-initialize Lucide icons after content loads
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
    /// Shows new consultation dialog
    /// </summary>
    private async Task ShowNewConversationDialog()
    {
        isLoadingDoctors = true;
        showNewConversationModal = true;
        DoctorSearchQuery = "";
        StateHasChanged();

        try
        {
            availableDoctors = await ConsultationFacade.GetAvailableDoctorsAsync();
            filteredDoctors = availableDoctors;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading doctors: {ex.Message}");
            availableDoctors = [];
            filteredDoctors = [];
        }
        finally
        {
            isLoadingDoctors = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Creates new consultation session
    /// Uses Facade to simplify creation process (AI or doctor consultation)
    /// </summary>
    private async Task CreateNewConversation(bool withAi)
    {
        try
        {
            ConsultationSession newSession;
            
            if (withAi)
            {
                // 使用 Facade 创建 AI 咨询
                newSession = await ConsultationFacade.StartAiConsultationAsync(AuthFacade.CurrentUser!.Id);
            }
            else
            {
                if (selectedDoctorId == null)
                {
                    return;
                }
                // 使用 Facade 创建医生咨询（不传递 sourceConversationId，因为这不是从AI推荐来的）
                newSession = await ConsultationFacade.StartDoctorConsultationAsync(
                    AuthFacade.CurrentUser!.Id, 
                    selectedDoctorId.Value,
                    initialMessage: null,
                    sourceConversationId: null
                );
            }

            showNewConversationModal = false;
            selectedDoctorId = null;
            await LoadConversations();
            await LoadConversation(newSession.Conversation.Id);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating conversation: {ex.Message}");
        }
    }

    private void CloseModal()
    {
        showNewConversationModal = false;
        selectedDoctorId = null;
        DoctorSearchQuery = "";
        StateHasChanged();
    }

    /// <summary>
    /// Filter doctors based on search query
    /// </summary>
    private void FilterDoctors()
    {
        if (string.IsNullOrWhiteSpace(_doctorSearchQuery))
        {
            filteredDoctors = availableDoctors;
        }
        else
        {
            var query = _doctorSearchQuery.ToLower();
            filteredDoctors = availableDoctors
                .Where(d =>
                    d.FullName.ToLower().Contains(query) ||
                    d.PrimarySpecialization.ToLower().Contains(query))
                .ToList();
        }
        StateHasChanged();
    }

    /// <summary>
    /// Sends message with streaming AI response
    /// Uses Facade to handle message sending and AI response
    /// </summary>
    private async Task SendMessage()
    {
        if ((string.IsNullOrWhiteSpace(newMessage) && attachments.Count == 0) || currentConversation == null)
            return;

        var messageContent = newMessage;
        var messageAttachments = new List<AttachmentFile>(attachments);
        newMessage = "";
        attachments.Clear();
        StateHasChanged(); // Clear input immediately

        Console.WriteLine("=== [DEBUG] SendMessage Started ===");
        Console.WriteLine($"[DEBUG] Message: {messageContent}");
        Console.WriteLine($"[DEBUG] Attachments: {messageAttachments.Count}");
        Console.WriteLine($"[DEBUG] Is AI Conversation: {currentConversation.AssignedDoctorId == null}");

        try
        {
            // 1. Upload attachments first if any
            List<Guid> documentIds = new();
            if (messageAttachments.Count > 0)
            {
                foreach (var attachment in messageAttachments)
                {
                    try
                    {
                        var document = new Document
                        {
                            ConversationId = currentConversation.Id,
                            UploadedByUserId = AuthFacade.CurrentUser!.Id,
                            FileName = attachment.FileName,
                            FileType = DetermineDocumentType(attachment.ContentType),
                            FileSizeBytes = attachment.FileSize,
                            FileUrl = $"/uploads/{Guid.NewGuid()}_{attachment.FileName}",
                            MimeType = attachment.ContentType,
                            FileData = attachment.FileData,
                            PatientId = AuthFacade.CurrentUser!.Id
                        };

                        var savedDoc = await DocumentService.CreateAsync(document);
                        documentIds.Add(savedDoc.Id);
                        Console.WriteLine($"[DEBUG] Uploaded document: {savedDoc.FileName} (ID: {savedDoc.Id})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Failed to upload attachment {attachment.FileName}: {ex.Message}");
                    }
                }
            }

            // 2. If is AI conversation, create empty AI message for streaming
            Message? aiMessage = null;
            Message? userMessage = null;
            
            if (currentConversation.AssignedDoctorId == null)
            {
                // Show typing indicator
                isTyping = true;
                await InvokeAsync(() =>
                {
                    StateHasChanged();
                });
                Console.WriteLine("[DEBUG] Showing typing indicator, waiting for stream...");
            }

            // 3. Send message with attachments
            Console.WriteLine("[DEBUG] Calling ConsultationFacade.SendPatientMessageWithStreamingAsync...");
            
            var chunkCount = 0;
            await foreach (var chunk in ConsultationFacade.SendPatientMessageWithAttachmentsAsync(
                currentConversation.Id,
                AuthFacade.CurrentUser!.Id,
                messageContent,
                documentIds))
            {
                chunkCount++;
                Console.WriteLine($"[DEBUG] Received chunk #{chunkCount}: IsUserMessage={chunk.IsUserMessage}, IsAiChunk={chunk.IsAiChunk}, IsComplete={chunk.IsComplete}");
                
                if (chunk.IsUserMessage)
                {
                    Console.WriteLine($"[DEBUG] Processing user message chunk");
                    if (userMessage == null && chunk.Message != null)
                    {
                        userMessage = chunk.Message;
                        messages.Add(userMessage);
                        await InvokeAsync(() =>
                        {
                            StateHasChanged();
                        });
                        _ = ScrollToBottom();
                        Console.WriteLine($"[DEBUG] Added user message with ID: {chunk.Message.Id}");
                    }
                }
                else if (chunk.IsAiChunk)
                {
                    // First AI chunk - create AI message box
                    if (aiMessage == null)
                    {
                        aiMessage = new Message
                        {
                            Id = Guid.NewGuid(),
                            ConversationId = currentConversation.Id,
                            SenderId = null,
                            SenderType = MessageSenderType.AI,
                            Content = chunk.Content,
                            CreatedAt = DateTime.UtcNow
                        };
                        
                        messages.Add(aiMessage);
                        isTyping = false;
                        Console.WriteLine("[DEBUG] First AI chunk received, created AI message box");
                    }
                    else
                    {
                        // Append content
                        aiMessage.Content += chunk.Content;
                        Console.WriteLine($"[DEBUG] AI content updated, length: {aiMessage.Content.Length}");
                    }
                    
                    // Critical: Use InvokeAsync to marshal UI updates to the component's sync context
                    await InvokeAsync(() =>
                    {
                        StateHasChanged();
                    });
                    
                    // Scroll to bottom without blocking
                    _ = ScrollToBottom();
                }
                else if (chunk.IsComplete && chunk.Message != null)
                {
                    Console.WriteLine($"[DEBUG] AI message complete, final length: {chunk.Message.Content.Length}");
                    if (aiMessage != null)
                    {
                        messages.Remove(aiMessage);
                        messages.Add(chunk.Message);
                        Console.WriteLine($"[DEBUG] Replaced temporary AI message with final version, ID: {chunk.Message.Id}");
                        await InvokeAsync(() =>
                        {
                            StateHasChanged();
                        });
                    }
                }
            }

            Console.WriteLine($"[DEBUG] Streaming completed, total chunks: {chunkCount}");
            
            isTyping = false;
            await InvokeAsync(() =>
            {
                StateHasChanged();
            });
            await ScrollToBottom();

            Console.WriteLine("=== [DEBUG] SendMessage Completed Successfully ===\n");
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== [DEBUG] SendMessage ERROR ===");
            Console.WriteLine($"[ERROR] Exception Type: {ex.GetType().Name}");
            Console.WriteLine($"[ERROR] Message: {ex.Message}");
            Console.WriteLine($"[ERROR] Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[ERROR] Inner Exception: {ex.InnerException.Message}");
            }
            
            Console.WriteLine("[ERROR] Message failed to send");
            
            isTyping = false;
            await InvokeAsync(() =>
            {
                StateHasChanged();
            });
        }
    }

    /// <summary>
    /// Determines document type from MIME type
    /// </summary>
    private static DocumentType DetermineDocumentType(string mimeType)
    {
        if (mimeType.StartsWith("image/"))
            return DocumentType.Image;
        else if (mimeType == "application/pdf")
            return DocumentType.MedicalRecord;
        else if (mimeType.Contains("word") || mimeType.Contains("document"))
            return DocumentType.MedicalRecord;
        else
            return DocumentType.Other;
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

    private void ToggleMode()
    {
        isAiMode = !isAiMode;
        StateHasChanged();
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

    private static string GetStatusBadgeClass(ConversationStatus status)
    {
        return status switch
        {
            ConversationStatus.Active => "status-active",
            ConversationStatus.Closed => "status-closed",
            ConversationStatus.Archived => "status-archived",
            _ => ""
        };
    }

    private static string GetAvailabilityStatusText(DoctorAvailabilityStatus status)
    {
        return status switch
        {
            DoctorAvailabilityStatus.Available => "🟢 Available",
            DoctorAvailabilityStatus.Busy => "🟡 Busy",
            DoctorAvailabilityStatus.Offline => "🔴 Offline",
            _ => "Unknown"
        };
    }

    private static string GetAvailabilityStatusStyle(DoctorAvailabilityStatus status)
    {
        return status switch
        {
            DoctorAvailabilityStatus.Available => "color: #10b981; font-weight: 500;",
            DoctorAvailabilityStatus.Busy => "color: #f59e0b; font-weight: 500;",
            DoctorAvailabilityStatus.Offline => "color: #ef4444; font-weight: 500;",
            _ => "color: #6b7280;"
        };
    }
    /// <summary>
    /// Shows model selector
    /// </summary>
    private void ShowModelSelector()
    {
        showModelSelectorModal = true;
        StateHasChanged();
    }

    /// <summary>
    /// Switches AI model
    /// Uses Facade to control model selection
    /// </summary>
    private void SwitchModel(string modelKey)
    {
        Console.WriteLine($"[UI] Switching AI model to: {modelKey}");
        currentModelKey = modelKey;
        ConsultationFacade.SwitchAiModel(modelKey);
        showModelSelectorModal = false;
        Console.WriteLine($"[UI] Model switched successfully to: {ConsultationFacade.GetCurrentAiModelName()}");
        StateHasChanged();
    }

    /// <summary>
    /// Closes model selector
    /// </summary>
    private void CloseModelSelector()
    {
        showModelSelectorModal = false;
        StateHasChanged();
    }

    /// <summary>
    /// Loads AI-recommended doctors based on conversation context
    /// </summary>
    private async Task LoadRecommendedDoctors()
    {
        if (currentConversation == null)
            return;

        isLoadingRecommendations = true;
        showRecommendedDoctors = true;
        StateHasChanged();

        try
        {
            Console.WriteLine($"[UI] Loading recommended doctors for conversation {currentConversation.Id}");
            
            recommendedDoctors = await ConsultationFacade.GetAiRecommendedDoctorsAsync(
                currentConversation.Id,
                maxResults: 5
            );

            Console.WriteLine($"[UI] Loaded {recommendedDoctors.Count} recommended doctors");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UI] Error loading recommended doctors: {ex.Message}");
            recommendedDoctors = new();
        }
        finally
        {
            isLoadingRecommendations = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Shows all recommended doctors in a modal
    /// </summary>
    private void ShowAllRecommendedDoctors()
    {
        showAllRecommendedDoctorsModal = true;
        selectedRecommendedDoctorId = null;
        StateHasChanged();
    }

    /// <summary>
    /// Closes all recommended doctors modal
    /// </summary>
    private void CloseAllRecommendedDoctors()
    {
        showAllRecommendedDoctorsModal = false;
        selectedRecommendedDoctorId = null;
        StateHasChanged();
    }

    /// <summary>
    /// Shows doctor details (placeholder for future implementation)
    /// </summary>
    private void ShowDoctorDetails(RecommendedDoctorItem doctor)
    {
        Console.WriteLine($"[UI] Showing details for Dr. {doctor.FullName}");
        // Future: Show detailed doctor profile modal
    }

    /// <summary>
    /// Starts consultation with a recommended doctor
    /// </summary>
    private async Task StartConsultationWithDoctor(Guid? doctorId)
    {
        if (doctorId == null)
            return;

        try
        {
            Console.WriteLine($"[UI] Starting consultation with doctor {doctorId}");
            
            // Create new doctor consultation with source conversation ID for summary
            var newSession = await ConsultationFacade.StartDoctorConsultationAsync(
                AuthFacade.CurrentUser!.Id,
                doctorId.Value,
                initialMessage: null,
                sourceConversationId: currentConversation?.Id // Pass current AI conversation ID
            );

            // Close modals
            showAllRecommendedDoctorsModal = false;
            selectedRecommendedDoctorId = null;

            // Reload conversations and switch to new one
            await LoadConversations();
            await LoadConversation(newSession.Conversation.Id);

            Console.WriteLine($"[UI] Successfully started consultation with doctor");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[UI] Error starting consultation with doctor: {ex.Message}");
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
    /// Handles file selection for attachment
    /// </summary>
    private async Task HandleFileSelected(InputFileChangeEventArgs e)
    {
        const long maxFileSize = 10 * 1024 * 1024; // 10MB
        const int maxAllowedFiles = 5;

        if (attachments.Count >= maxAllowedFiles)
        {
            Console.WriteLine($"[UI] Maximum {maxAllowedFiles} files allowed");
            return;
        }

        foreach (var file in e.GetMultipleFiles(maxAllowedFiles - attachments.Count))
        {
            if (file.Size > maxFileSize)
            {
                Console.WriteLine($"[UI] File {file.Name} exceeds maximum size of 10MB");
                continue;
            }

            try
            {
                isUploadingFile = true;
                StateHasChanged();

                // Read file content
                using var stream = file.OpenReadStream(maxFileSize);
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var fileData = memoryStream.ToArray();

                // Create attachment object
                var attachment = new AttachmentFile
                {
                    FileName = file.Name,
                    ContentType = file.ContentType,
                    FileSize = file.Size,
                    FileData = fileData,
                    PreviewUrl = await GeneratePreviewUrl(file, fileData)
                };

                attachments.Add(attachment);
                Console.WriteLine($"[UI] File {file.Name} added to attachments");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UI] Error uploading file {file.Name}: {ex.Message}");
            }
            finally
            {
                isUploadingFile = false;
                StateHasChanged();
            }
        }
    }

    /// <summary>
    /// Generates preview URL for image files
    /// </summary>
    private async Task<string?> GeneratePreviewUrl(IBrowserFile file, byte[] fileData)
    {
        if (file.ContentType.StartsWith("image/"))
        {
            var base64 = Convert.ToBase64String(fileData);
            return $"data:{file.ContentType};base64,{base64}";
        }
        return null;
    }

    /// <summary>
    /// Removes an attachment from the list
    /// </summary>
    private void RemoveAttachment(AttachmentFile attachment)
    {
        attachments.Remove(attachment);
        StateHasChanged();
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

    /// <summary>
    /// Cleanup SignalR connections
    /// </summary>
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

    /// <summary>
    /// Helper class for attachment files
    /// </summary>
    private class AttachmentFile
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public byte[] FileData { get; set; } = Array.Empty<byte>();
        public string? PreviewUrl { get; set; }
    }

}

