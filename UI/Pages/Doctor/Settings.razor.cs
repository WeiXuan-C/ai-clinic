using Microsoft.AspNetCore.Components;
using ai_clinic.Models;
using ai_clinic.Services.Facades;

namespace ai_clinic.UI.Pages.Doctor;

public partial class Settings : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private DoctorFacade DoctorFacade { get; set; } = null!;

    private bool isLoading = true;
    private bool isSaving = false;
    private string? errorMessage;
    private string? successMessage;

    private DoctorSettingsData settings = new();

    protected override async Task OnInitializedAsync()
    {
        if (!AuthFacade.IsAuthenticated || AuthFacade.CurrentUser?.Role != UserRole.Doctor)
        {
            Navigation.NavigateTo("/auth/signin");
            return;
        }

        await LoadSettings();
    }

    private async Task LoadSettings()
    {
        isLoading = true;
        errorMessage = null;

        try
        {
            var userId = AuthFacade.CurrentUser!.Id;
            settings = await DoctorFacade.GetDoctorSettingsAsync(userId);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load settings: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task SaveSettings()
    {
        isSaving = true;
        errorMessage = null;
        successMessage = null;

        try
        {
            var userId = AuthFacade.CurrentUser!.Id;
            await DoctorFacade.SaveDoctorSettingsAsync(userId, settings);
            successMessage = "Settings saved successfully!";
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to save settings: {ex.Message}";
        }
        finally
        {
            isSaving = false;
        }
    }

    private string GetAvailabilityStatusText(DoctorAvailabilityStatus status)
    {
        return status switch
        {
            DoctorAvailabilityStatus.Available => "Available",
            DoctorAvailabilityStatus.Busy => "Busy",
            DoctorAvailabilityStatus.Offline => "Offline",
            _ => "Unknown"
        };
    }
}
