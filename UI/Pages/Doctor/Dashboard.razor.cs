using Microsoft.AspNetCore.Components;
using ai_clinic.Services.Facades;
using ai_clinic.Models;

namespace ai_clinic.UI.Pages.Doctor;

public partial class Dashboard : ComponentBase
{
    [Inject] private DoctorFacade DoctorFacade { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private DoctorDashboardFullData? dashboardData;
    private bool isLoading = true;
    private string errorMessage = string.Empty;
    private string doctorName = "Doctor";
    private List<Conversation> topConversations = new();

    protected override async Task OnInitializedAsync()
    {
        if (!AuthFacade.IsAuthenticated || AuthFacade.CurrentUser?.Role != UserRole.Doctor)
        {
            Navigation.NavigateTo("/auth/signin");
            return;
        }

        await LoadDashboardData();
    }

    private async Task LoadDashboardData()
    {
        isLoading = true;
        errorMessage = string.Empty;

        try
        {
            var userId = AuthFacade.CurrentUser!.Id;
            dashboardData = await DoctorFacade.GetDoctorDashboardFullDataAsync(userId);
            
            if (dashboardData.Profile != null)
            {
                doctorName = $"Dr. {dashboardData.Profile.FullName}";
            }

            // Load top 10 conversations based on recent activity
            await LoadTopConversations(userId);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load dashboard data: {ex.Message}";
            Console.WriteLine($"[DASHBOARD ERROR] {ex}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task LoadTopConversations(Guid doctorId)
    {
        try
        {
            // Get all conversations for this doctor and take top 10 by last message time
            var allConversations = await DoctorFacade.GetDoctorConversationsAsync(doctorId);
            topConversations = allConversations
                .OrderByDescending(c => c.LastMessageAt)
                .Take(10)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DASHBOARD ERROR] Failed to load top conversations: {ex.Message}");
            topConversations = new();
        }
    }

    private string GetTimeSince(DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        
        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} min ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hour{((int)timeSpan.TotalHours != 1 ? "s" : "")} ago";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} day{((int)timeSpan.TotalDays != 1 ? "s" : "")} ago";
        
        return dateTime.ToString("MMM dd, yyyy");
    }

    private string GetConversationBadgeClass(Conversation conversation)
    {
        if (conversation.Status == ConversationStatus.Active)
            return "badge-urgent";
        if (conversation.Status == ConversationStatus.Deactive)
            return "badge-scheduled";
        return "badge-followup";
    }

    private string GetConversationBadgeText(Conversation conversation)
    {
        if (conversation.Status == ConversationStatus.Active)
            return "ACTIVE";
        if (conversation.Status == ConversationStatus.Deactive)
            return "PENDING";
        return "CLOSED";
    }

    private string GetActivityDescription(ActivityLog log)
    {
        return log.Action switch
        {
            "CompleteConsultation" => $"Completed consultation",
            "AcceptConsultation" => $"Accepted consultation",
            "UpdateDoctorProfile" => $"Updated profile",
            _ => log.Action
        };
    }
}
