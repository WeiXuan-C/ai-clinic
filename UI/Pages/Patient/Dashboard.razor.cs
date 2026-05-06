using Microsoft.AspNetCore.Components;
using ai_clinic.Services.Facades;
using ai_clinic.Models;

namespace ai_clinic.UI.Pages.Patient;

public partial class Dashboard : ComponentBase
{
    [Inject] private PatientFacade PatientFacade { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private PatientDashboardData? dashboardData;
    private bool isLoading = true;
    private string errorMessage = string.Empty;
    private string displayName = "Guest";

    protected override async Task OnInitializedAsync()
    {
        if (!AuthFacade.IsAuthenticated || AuthFacade.CurrentUser?.Role != UserRole.Patient)
        {
            Navigation.NavigateTo("/auth/signin");
            return;
        }

        displayName = AuthFacade.GetDisplayName();
        await LoadDashboardData();
    }

    private async Task LoadDashboardData()
    {
        isLoading = true;
        errorMessage = string.Empty;

        try
        {
            var userId = AuthFacade.CurrentUser!.Id;
            dashboardData = await PatientFacade.GetDashboardDataAsync(userId);
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

    private string GetConsultationBadgeClass(Conversation conversation)
    {
        if (conversation.AssignedDoctorId.HasValue)
            return "badge-doctor";
        return "badge-ai";
    }

    private string GetConsultationBadgeText(Conversation conversation)
    {
        if (conversation.AssignedDoctorId.HasValue)
            return "DOCTOR VISIT";
        return "AI ASSIST";
    }

    private string GetConsultationIconClass(Conversation conversation)
    {
        if (conversation.AssignedDoctorId.HasValue)
            return "doctor";
        return "ai";
    }

    private string GetConsultationIcon(Conversation conversation)
    {
        if (conversation.AssignedDoctorId.HasValue)
            return "stethoscope";
        return "bot";
    }

    private void NavigateToNewConsultation()
    {
        Navigation.NavigateTo("/patient/consultation");
    }

    private void NavigateToDoctors()
    {
        Navigation.NavigateTo("/doctors");
    }
}
