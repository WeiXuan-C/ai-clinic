using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ai_clinic.Models;
using ai_clinic.Services.Facades;

namespace ai_clinic.UI.Pages.Patient;

public partial class Settings : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private PatientFacade PatientFacade { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private bool isLoading = true;
    private string? errorMessage;
    private string? successMessage;

    private PatientSettingsData settings = new();

    // Email change fields
    private bool showEmailChangeModal = false;
    private string newEmail = string.Empty;
    private string emailErrorMessage = string.Empty;
    private string emailSuccessMessage = string.Empty;
    private bool isChangingEmail = false;

    // Password change fields
    private bool showPasswordChangeModal = false;
    private string currentPassword = string.Empty;
    private string newPassword = string.Empty;
    private string confirmPassword = string.Empty;
    private string passwordErrorMessage = string.Empty;
    private string passwordSuccessMessage = string.Empty;
    private bool isChangingPassword = false;
    
    // Password visibility toggles
    private bool showCurrentPassword = false;
    private bool showNewPassword = false;
    private bool showConfirmPassword = false;

    // Download data
    private bool isDownloadingData = false;

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
        errorMessage = null;

        try
        {
            var userId = AuthFacade.CurrentUser!.Id;
            await PatientFacade.SavePatientSettingsAsync(userId, settings);
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to save settings: {ex.Message}";
        }
    }

    private void OpenEmailChangeModal()
    {
        showEmailChangeModal = true;
        newEmail = string.Empty;
        emailErrorMessage = string.Empty;
        emailSuccessMessage = string.Empty;
    }

    private void CloseEmailChangeModal()
    {
        showEmailChangeModal = false;
        newEmail = string.Empty;
        emailErrorMessage = string.Empty;
        emailSuccessMessage = string.Empty;
    }

    private async Task SubmitEmailChange()
    {
        emailErrorMessage = string.Empty;
        emailSuccessMessage = string.Empty;
        isChangingEmail = true;

        try
        {
            var userId = AuthFacade.CurrentUser!.Id;
            var result = await PatientFacade.ChangeEmailAsync(userId, newEmail);

            if (result.Success)
            {
                emailSuccessMessage = result.Message;
                settings.Email = newEmail;
                await Task.Delay(1500);
                CloseEmailChangeModal();
                await LoadSettings(); // Reload to get updated data
            }
            else
            {
                emailErrorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            emailErrorMessage = $"Failed to change email: {ex.Message}";
        }
        finally
        {
            isChangingEmail = false;
        }
    }

    private void OpenPasswordChangeModal()
    {
        showPasswordChangeModal = true;
        currentPassword = string.Empty;
        newPassword = string.Empty;
        confirmPassword = string.Empty;
        passwordErrorMessage = string.Empty;
        passwordSuccessMessage = string.Empty;
    }

    private void ClosePasswordChangeModal()
    {
        showPasswordChangeModal = false;
        currentPassword = string.Empty;
        newPassword = string.Empty;
        confirmPassword = string.Empty;
        passwordErrorMessage = string.Empty;
        passwordSuccessMessage = string.Empty;
        showCurrentPassword = false;
        showNewPassword = false;
        showConfirmPassword = false;
    }

    private void ToggleCurrentPasswordVisibility()
    {
        showCurrentPassword = !showCurrentPassword;
    }

    private void ToggleNewPasswordVisibility()
    {
        showNewPassword = !showNewPassword;
    }

    private void ToggleConfirmPasswordVisibility()
    {
        showConfirmPassword = !showConfirmPassword;
    }

    private async Task DownloadMyData()
    {
        isDownloadingData = true;
        errorMessage = null;
        successMessage = null;

        try
        {
            var userId = AuthFacade.CurrentUser!.Id;
            var pdfBytes = await PatientFacade.ExportAllPatientDataAsync(userId);

            // Generate filename with timestamp
            var fileName = $"MyData_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";

            // Download file using JavaScript
            await JS.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(pdfBytes), "application/pdf");

            successMessage = "Your data has been exported successfully!";
            
            // Clear success message after 5 seconds
            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                successMessage = null;
                await InvokeAsync(StateHasChanged);
            });
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to download data: {ex.Message}";
        }
        finally
        {
            isDownloadingData = false;
        }
    }

    private async Task SubmitPasswordChange()
    {
        passwordErrorMessage = string.Empty;
        passwordSuccessMessage = string.Empty;
        isChangingPassword = true;

        try
        {
            var userId = AuthFacade.CurrentUser!.Id;
            var result = await PatientFacade.ChangePasswordAsync(userId, currentPassword, newPassword, confirmPassword);

            if (result.Success)
            {
                passwordSuccessMessage = result.Message;
                await Task.Delay(1500);
                ClosePasswordChangeModal();
            }
            else
            {
                passwordErrorMessage = result.Message;
            }
        }
        catch (Exception ex)
        {
            passwordErrorMessage = $"Failed to change password: {ex.Message}";
        }
        finally
        {
            isChangingPassword = false;
        }
    }
}
