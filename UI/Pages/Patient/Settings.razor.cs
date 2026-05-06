using Microsoft.AspNetCore.Components;
using ai_clinic.Models;
using ai_clinic.Services.Facades;

namespace ai_clinic.UI.Pages.Patient;

public partial class Settings : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private PatientFacade PatientFacade { get; set; } = null!;

    private bool isLoading = true;
    private bool isSaving = false;
    private string? errorMessage;
    private string? successMessage;

    private PatientSettingsData settings = new();

    protected override async Task OnInitializedAsync()
    {
        if (!AuthFacade.IsAuthenticated || AuthFacade.CurrentUser?.Role != UserRole.Patient)
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
            settings = await PatientFacade.GetPatientSettingsAsync(userId);
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
            await PatientFacade.SavePatientSettingsAsync(userId, settings);
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
}
