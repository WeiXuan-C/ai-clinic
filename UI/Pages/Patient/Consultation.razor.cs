using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ai_clinic.Models;
using ai_clinic.Services;
using ai_clinic.Services.Facades;
using ConversationListItem = ai_clinic.Services.ConversationListItem;
using DoctorListItem = ai_clinic.Services.DoctorListItem;
using static ai_clinic.Services.Facades.ConsultationFacade;

namespace ai_clinic.UI.Pages.Patient;

/// <summary>
/// Patient consultation page - Uses Facade Pattern to simplify complex interactions
/// </summary>
public partial class Consultation : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private ConsultationFacade ConsultationFacade { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

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

        // Load patient photo
        await LoadPatientPhoto();

        // Load available AI models
        LoadAvailableModels();
        
        await LoadConversations();
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
                // 使用 Facade 创建医生咨询
                newSession = await ConsultationFacade.StartDoctorConsultationAsync(
                    AuthFacade.CurrentUser!.Id, 
                    selectedDoctorId.Value
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
        if (string.IsNullOrWhiteSpace(newMessage) || currentConversation == null)
            return;

        var messageContent = newMessage;
        newMessage = "";
        StateHasChanged(); // Clear input immediately

        Console.WriteLine("=== [DEBUG] SendMessage Started ===");
        Console.WriteLine($"[DEBUG] Message: {messageContent}");
        Console.WriteLine($"[DEBUG] Is AI Conversation: {currentConversation.AssignedDoctorId == null}");

        try
        {
            // 1. 如果是AI对话，创建空的AI消息用于流式输出（先不显示用户消息）
            Message? aiMessage = null;
            Message? userMessage = null;
            
            if (currentConversation.AssignedDoctorId == null)
            {
                // 显示typing indicator，但不显示空的AI消息框
                isTyping = true;
                StateHasChanged();
                Console.WriteLine("[DEBUG] Showing typing indicator, waiting for stream...");
            }

            // 2. 发送消息到后端并获取流式响应
            Console.WriteLine("[DEBUG] Calling ConsultationFacade.SendPatientMessageWithStreamingAsync...");
            
            var chunkCount = 0;
            await foreach (var chunk in ConsultationFacade.SendPatientMessageWithStreamingAsync(
                currentConversation.Id,
                AuthFacade.CurrentUser!.Id,
                messageContent))
            {
                chunkCount++;
                Console.WriteLine($"[DEBUG] Received chunk #{chunkCount}: IsUserMessage={chunk.IsUserMessage}, IsAiChunk={chunk.IsAiChunk}, IsComplete={chunk.IsComplete}");
                
                if (chunk.IsUserMessage)
                {
                    Console.WriteLine($"[DEBUG] Processing user message chunk");
                    // 第一次收到用户消息时才显示
                    if (userMessage == null && chunk.Message != null)
                    {
                        userMessage = chunk.Message;
                        messages.Add(userMessage);
                        StateHasChanged();
                        await ScrollToBottom();
                        Console.WriteLine($"[DEBUG] Added user message with ID: {chunk.Message.Id}");
                    }
                }
                else if (chunk.IsAiChunk)
                {
                    // 第一个AI chunk到达时，创建AI消息框
                    if (aiMessage == null)
                    {
                        aiMessage = new Message
                        {
                            Id = Guid.NewGuid(), // Temporary ID
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
                        // 后续chunk追加内容
                        aiMessage.Content += chunk.Content;
                        Console.WriteLine($"[DEBUG] AI content updated, length: {aiMessage.Content.Length}");
                    }
                    
                    // Force immediate UI update for streaming effect
                    await InvokeAsync(StateHasChanged);
                    await Task.Delay(1); // Tiny delay to ensure UI renders
                    await ScrollToBottom();
                }
                else if (chunk.IsComplete && chunk.Message != null)
                {
                    Console.WriteLine($"[DEBUG] AI message complete, final length: {chunk.Message.Content.Length}");
                    // AI消息完成，更新为最终版本
                    if (aiMessage != null)
                    {
                        messages.Remove(aiMessage);
                        messages.Add(chunk.Message);
                        Console.WriteLine($"[DEBUG] Replaced temporary AI message with final version, ID: {chunk.Message.Id}");
                        StateHasChanged();
                    }
                }
            }

            Console.WriteLine($"[DEBUG] Streaming completed, total chunks: {chunkCount}");
            
            isTyping = false;
            StateHasChanged();
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
            StateHasChanged();
        }
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
            
            // Create new doctor consultation
            var newSession = await ConsultationFacade.StartDoctorConsultationAsync(
                AuthFacade.CurrentUser!.Id,
                doctorId.Value
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

}

