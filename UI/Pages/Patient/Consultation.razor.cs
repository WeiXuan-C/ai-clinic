using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ai_clinic.Models;
using ai_clinic.Services;
using ai_clinic.Services.Facades;

namespace ai_clinic.UI.Pages.Patient;

/// <summary>
/// 患者咨询页面 - 使用 Facade Pattern 简化复杂交互
/// </summary>
public partial class Consultation : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthStateService AuthState { get; set; } = null!;
    [Inject] private ConsultationFacade ConsultationFacade { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private List<ConversationListItem> conversationList = new();
    private List<Message> messages = new();
    private Conversation? currentConversation;
    private string newMessage = "";
    private bool isTyping = false;
    private bool isAiMode = true;
    private bool showNewConversationModal = false;
    private bool isLoadingDoctors = false;
    private List<DoctorListItem> availableDoctors = new();
    private List<DoctorListItem> filteredDoctors = new();
    private string _doctorSearchQuery = "";
    private string doctorSearchQuery
    {
        get => _doctorSearchQuery;
        set
        {
            _doctorSearchQuery = value;
            FilterDoctors();
        }
    }
    private Guid? selectedDoctorId = null;

    protected override async Task OnInitializedAsync()
    {
        if (!AuthState.IsAuthenticated || AuthState.CurrentUser?.Role != UserRole.Patient)
        {
            Navigation.NavigateTo("/auth/signin");
            return;
        }

        await LoadConversations();
    }

    /// <summary>
    /// 加载患者的所有咨询列表
    /// 使用 Facade 简化调用
    /// </summary>
    private async Task LoadConversations()
    {
        conversationList = await ConsultationFacade.GetPatientConsultationsAsync(AuthState.CurrentUser!.Id);
        
        // Load the first conversation if exists
        if (conversationList.Any())
        {
            await LoadConversation(conversationList.First().Id);
        }
    }

    /// <summary>
    /// 加载特定咨询会话
    /// 使用 Facade 一次性获取所有需要的数据（对话、消息、医生信息、标记已读）
    /// </summary>
    private async Task LoadConversation(Guid conversationId)
    {
        try
        {
            var session = await ConsultationFacade.GetConsultationSessionAsync(
                conversationId, 
                AuthState.CurrentUser!.Id,
                UserRole.Patient
            );

            currentConversation = session.Conversation;
            messages = session.Messages;
            isAiMode = session.IsAiConsultation;
            
            StateHasChanged();
            await ScrollToBottom();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading conversation: {ex.Message}");
        }
    }

    /// <summary>
    /// 显示新建咨询对话框
    /// </summary>
    private async Task ShowNewConversationDialog()
    {
        isLoadingDoctors = true;
        showNewConversationModal = true;
        doctorSearchQuery = "";
        StateHasChanged();
        
        try
        {
            availableDoctors = await ConsultationFacade.GetAvailableDoctorsAsync();
            filteredDoctors = availableDoctors;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading doctors: {ex.Message}");
            availableDoctors = new List<DoctorListItem>();
            filteredDoctors = new List<DoctorListItem>();
        }
        finally
        {
            isLoadingDoctors = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// 创建新的咨询会话
    /// 使用 Facade 简化创建流程（AI 或医生咨询）
    /// </summary>
    private async Task CreateNewConversation(bool withAi)
    {
        try
        {
            ConsultationSession newSession;
            
            if (withAi)
            {
                // 使用 Facade 创建 AI 咨询
                newSession = await ConsultationFacade.StartAiConsultationAsync(AuthState.CurrentUser!.Id);
            }
            else
            {
                if (selectedDoctorId == null)
                {
                    return;
                }
                // 使用 Facade 创建医生咨询
                newSession = await ConsultationFacade.StartDoctorConsultationAsync(
                    AuthState.CurrentUser!.Id, 
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
        doctorSearchQuery = "";
        StateHasChanged();
    }

    /// <summary>
    /// Filter doctors based on search query
    /// </summary>
    private void FilterDoctors()
    {
        if (string.IsNullOrWhiteSpace(doctorSearchQuery))
        {
            filteredDoctors = availableDoctors;
        }
        else
        {
            var query = doctorSearchQuery.ToLower();
            filteredDoctors = availableDoctors
                .Where(d => 
                    d.FullName.ToLower().Contains(query) ||
                    d.PrimarySpecialization.ToLower().Contains(query))
                .ToList();
        }
        StateHasChanged();
    }

    /// <summary>
    /// 发送消息
    /// 使用 Facade 处理消息发送和 AI 响应
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
            // 使用 Facade 发送消息（自动处理 AI 响应）
            isTyping = true;
            var result = await ConsultationFacade.SendPatientMessageAsync(
                currentConversation.Id,
                AuthState.CurrentUser!.Id,
                messageContent
            );

            // 添加患者消息到界面
            messages.Add(result.PatientMessage);
            StateHasChanged();
            await ScrollToBottom();

            // 如果有 AI 响应，添加到界面
            if (result.AiResponse != null)
            {
                await Task.Delay(500); // 短暂延迟，让用户看到 typing indicator
                messages.Add(result.AiResponse);
                isTyping = false;
                StateHasChanged();
                await ScrollToBottom();
            }
            else
            {
                isTyping = false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
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

    private string FormatTime(DateTime dateTime)
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

    private string GetStatusBadgeClass(ConversationStatus status)
    {
        return status switch
        {
            ConversationStatus.Active => "status-active",
            ConversationStatus.Closed => "status-closed",
            ConversationStatus.Archived => "status-archived",
            _ => ""
        };
    }

    private string GetAvailabilityStatusText(DoctorAvailabilityStatus status)
    {
        return status switch
        {
            DoctorAvailabilityStatus.Available => "🟢 Available",
            DoctorAvailabilityStatus.Busy => "🟡 Busy",
            DoctorAvailabilityStatus.Offline => "🔴 Offline",
            _ => "Unknown"
        };
    }

    private string GetAvailabilityStatusStyle(DoctorAvailabilityStatus status)
    {
        return status switch
        {
            DoctorAvailabilityStatus.Available => "color: #10b981; font-weight: 500;",
            DoctorAvailabilityStatus.Busy => "color: #f59e0b; font-weight: 500;",
            DoctorAvailabilityStatus.Offline => "color: #ef4444; font-weight: 500;",
            _ => "color: #6b7280;"
        };
    }
}
