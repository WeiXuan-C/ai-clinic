using Microsoft.AspNetCore.Components;
using ai_clinic.Services.Facades;
using ai_clinic.Models;

namespace ai_clinic.UI.Pages.Doctor;

public partial class Analytics : ComponentBase
{
    [Inject] private DoctorFacade DoctorFacade { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private NavigationManager Navigation { get; set; } = null!;

    private DoctorAnalyticsFullData? analyticsData;
    private bool isLoading = true;
    private string errorMessage = string.Empty;
    private string selectedPeriod = "month";

    protected override async Task OnInitializedAsync()
    {
        if (!AuthFacade.IsAuthenticated || AuthFacade.CurrentUser?.Role != UserRole.Doctor)
        {
            Navigation.NavigateTo("/auth/signin");
            return;
        }

        await LoadAnalyticsData();
    }

    private async Task LoadAnalyticsData()
    {
        isLoading = true;
        errorMessage = string.Empty;

        try
        {
            var userId = AuthFacade.CurrentUser!.Id;
            analyticsData = await DoctorFacade.GetDoctorAnalyticsAsync(userId, selectedPeriod);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load analytics data: {ex.Message}";
            Console.WriteLine($"[ANALYTICS ERROR] {ex}");
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnPeriodChanged(ChangeEventArgs e)
    {
        selectedPeriod = e.Value?.ToString() ?? "month";
        await LoadAnalyticsData();
    }

    private int GetConditionPercentage(TopConditionData condition)
    {
        if (analyticsData == null || !analyticsData.TopConditions.Any())
            return 0;

        var maxCount = analyticsData.TopConditions.Max(c => c.PatientCount);
        if (maxCount == 0)
            return 0;

        return (int)((double)condition.PatientCount / maxCount * 100);
    }
}
