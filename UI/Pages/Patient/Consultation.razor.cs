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

        // Load available AI models
        LoadAvailableModels();
        
        await LoadConversations();
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
            var session = await ConsultationFacade.GetConsultationSessionAsync(
                conversationId, 
                AuthFacade.CurrentUser!.Id,
                UserRole.Patient
            );

            currentConversation = session.Conversation;
            messages = session.Messages;
            isAiMode = session.IsAiConsultation;
            
            // Reset recommendations when switching conversations
            showRecommendedDoctors = false;
            recommendedDoctors = new();
            
            StateHasChanged();
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
    /// Sends message
    /// Uses Facade to handle message sending and AI response
    /// </summary>
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(newMessage) || currentConversation == null)
            return;

        var messageContent = newMessage;
        newMessage = "";
        StateHasChanged();

        Console.WriteLine("=== [DEBUG] SendMessage Started ===");
        Console.WriteLine($"[DEBUG] Conversation ID: {currentConversation.Id}");
        Console.WriteLine($"[DEBUG] Patient ID: {AuthFacade.CurrentUser!.Id}");
        Console.WriteLine($"[DEBUG] Message Content: {messageContent}");
        Console.WriteLine($"[DEBUG] Is AI Mode: {isAiMode}");

        try
        {
            // 使用 Facade 发送消息（自动处理 AI 响应）
            isTyping = true;
            Console.WriteLine("[DEBUG] Calling ConsultationFacade.SendPatientMessageAsync...");
            
            var result = await ConsultationFacade.SendPatientMessageAsync(
                currentConversation.Id,
                AuthFacade.CurrentUser!.Id,
                messageContent
            );

            Console.WriteLine("[DEBUG] Message sent successfully");
            Console.WriteLine($"[DEBUG] Patient Message ID: {result.PatientMessage.Id}");
            Console.WriteLine($"[DEBUG] Is AI Conversation: {result.IsAiConversation}");
            Console.WriteLine($"[DEBUG] AI Response Received: {result.AiResponse != null}");

            // 添加患者消息到界面
            messages.Add(result.PatientMessage);
            StateHasChanged();
            await ScrollToBottom();

            // 如果有 AI 响应，添加到界面
            if (result.AiResponse != null)
            {
                Console.WriteLine($"[DEBUG] AI Response ID: {result.AiResponse.Id}");
                Console.WriteLine($"[DEBUG] AI Response Content Length: {result.AiResponse.Content.Length} chars");
                Console.WriteLine($"[DEBUG] AI Response Preview: {result.AiResponse.Content[..Math.Min(100, result.AiResponse.Content.Length)]}...");

                await Task.Delay(500); // 短暂延迟，让用户看到 typing indicator
                messages.Add(result.AiResponse);
                isTyping = false;
                StateHasChanged();
                await ScrollToBottom();

                Console.WriteLine("[DEBUG] AI response added to UI");
            }
            else
            {
                Console.WriteLine("[DEBUG] No AI response (likely doctor conversation)");
                isTyping = false;
            }

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
            Console.WriteLine("=== [DEBUG] SendMessage Failed ===\n");
            
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

