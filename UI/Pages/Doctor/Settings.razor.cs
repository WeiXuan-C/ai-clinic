using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ai_clinic.Models;
using ai_clinic.Services.Facades;
using System.Text;

namespace ai_clinic.UI.Pages.Doctor;

public partial class Settings : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private DoctorFacade DoctorFacade { get; set; } = null!;
    [Inject] private IJSRuntime JSRuntime { get; set; } = null!;

    private DoctorProfile settings = new();
    private bool isLoading = true;
    private string errorMessage = string.Empty;
    private string successMessage = string.Empty;
    private Guid currentUserId;

    // Change Email
    private bool showChangeEmailModal = false;
    private string emailCurrentPassword = string.Empty;
    private string newEmail = string.Empty;
    private string emailModalError = string.Empty;
    private bool showEmailPassword = false;

    // Change Password
    private bool showChangePasswordModal = false;
    private string currentPassword = string.Empty;
    private string newPassword = string.Empty;
    private string confirmPassword = string.Empty;
    private string passwordModalError = string.Empty;
    private bool showCurrentPassword = false;
    private bool showNewPassword = false;
    private bool showConfirmPassword = false;

    // Deactivate Account
    private bool showDeactivateModal = false;
    private string deactivatePassword = string.Empty;
    private string deactivateModalError = string.Empty;
    private bool showDeactivatePassword = false;

    // Delete Account
    private bool showDeleteModal = false;
    private string deletePassword = string.Empty;
    private string deleteModalError = string.Empty;
    private bool showDeletePassword = false;

    // Download Data
    private bool isDownloading = false;

    // Processing
    private bool isProcessing = false;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            isLoading = true;
            
            // Check if user is authenticated via AuthFacade
            if (!AuthFacade.IsAuthenticated || AuthFacade.CurrentUser?.Role != UserRole.Doctor)
            {
                Navigation.NavigateTo("/login");
                return;
            }

            currentUserId = AuthFacade.CurrentUser.Id;

            // Use DoctorFacade to get settings data (Facade Pattern)
            var settingsData = await DoctorFacade.GetDoctorSettingsAsync(currentUserId);
            
            if (settingsData.Profile == null)
            {
                errorMessage = "Doctor profile not found";
                return;
            }

            settings = settingsData.Profile;
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading settings: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JSRuntime.InvokeVoidAsync("lucide.createIcons");
        }
        else
        {
            await JSRuntime.InvokeVoidAsync("lucide.createIcons");
        }
    }

    private async Task SaveSettings()
    {
        try
        {
            // Use DoctorFacade to save profile (Facade Pattern)
            await DoctorFacade.SaveDoctorProfileAsync(settings);
            
            successMessage = "Settings saved successfully";
            StateHasChanged();

            // Clear success message after 3 seconds
            await Task.Delay(3000);
            successMessage = string.Empty;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error saving settings: {ex.Message}";
        }
    }

    // Change Email Methods
    private void OpenChangeEmailModal()
    {
        showChangeEmailModal = true;
        emailCurrentPassword = string.Empty;
        newEmail = string.Empty;
        emailModalError = string.Empty;
    }

    private void CloseChangeEmailModal()
    {
        showChangeEmailModal = false;
        emailCurrentPassword = string.Empty;
        newEmail = string.Empty;
        emailModalError = string.Empty;
    }

    private async Task ChangeEmail()
    {
        try
        {
            isProcessing = true;
            emailModalError = string.Empty;

            if (string.IsNullOrWhiteSpace(emailCurrentPassword))
            {
                emailModalError = "Please enter your current password";
                return;
            }

            if (string.IsNullOrWhiteSpace(newEmail))
            {
                emailModalError = "Please enter a new email address";
                return;
            }

            // Use DoctorFacade to change email (Facade Pattern)
            var result = await DoctorFacade.ChangeEmailAsync(currentUserId, emailCurrentPassword, newEmail);

            if (result.Success)
            {
                successMessage = result.Message;
                CloseChangeEmailModal();
                StateHasChanged();

                // Clear success message after 3 seconds
                await Task.Delay(3000);
                successMessage = string.Empty;
                StateHasChanged();
            }
            else
            {
                emailModalError = result.Message;
            }
        }
        catch (Exception ex)
        {
            emailModalError = $"Error: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
        }
    }

    // Change Password Methods
    private void OpenChangePasswordModal()
    {
        showChangePasswordModal = true;
        currentPassword = string.Empty;
        newPassword = string.Empty;
        confirmPassword = string.Empty;
        passwordModalError = string.Empty;
    }

    private void CloseChangePasswordModal()
    {
        showChangePasswordModal = false;
        currentPassword = string.Empty;
        newPassword = string.Empty;
        confirmPassword = string.Empty;
        passwordModalError = string.Empty;
    }

    private async Task ChangePassword()
    {
        try
        {
            isProcessing = true;
            passwordModalError = string.Empty;

            if (string.IsNullOrWhiteSpace(currentPassword))
            {
                passwordModalError = "Please enter your current password";
                return;
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                passwordModalError = "Please enter a new password";
                return;
            }

            if (newPassword.Length < 6)
            {
                passwordModalError = "New password must be at least 6 characters long";
                return;
            }

            if (newPassword != confirmPassword)
            {
                passwordModalError = "Passwords do not match";
                return;
            }

            // Use DoctorFacade to change password (Facade Pattern)
            var result = await DoctorFacade.ChangePasswordAsync(currentUserId, currentPassword, newPassword);

            if (result.Success)
            {
                successMessage = result.Message;
                CloseChangePasswordModal();
                StateHasChanged();

                // Clear success message after 3 seconds
                await Task.Delay(3000);
                successMessage = string.Empty;
                StateHasChanged();
            }
            else
            {
                passwordModalError = result.Message;
            }
        }
        catch (Exception ex)
        {
            passwordModalError = $"Error: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
        }
    }

    // Deactivate Account Methods
    private void OpenDeactivateModal()
    {
        showDeactivateModal = true;
        deactivatePassword = string.Empty;
        deactivateModalError = string.Empty;
    }

    private void CloseDeactivateModal()
    {
        showDeactivateModal = false;
        deactivatePassword = string.Empty;
        deactivateModalError = string.Empty;
    }

    private async Task DeactivateAccount()
    {
        try
        {
            isProcessing = true;
            deactivateModalError = string.Empty;

            if (string.IsNullOrWhiteSpace(deactivatePassword))
            {
                deactivateModalError = "Please enter your password to confirm";
                return;
            }

            // Use DoctorFacade to deactivate account (Facade Pattern)
            var result = await DoctorFacade.DeactivateAccountAsync(currentUserId, deactivatePassword);

            if (result.Success)
            {
                // Redirect to logout or home page
                Navigation.NavigateTo("/logout");
            }
            else
            {
                deactivateModalError = result.Message;
            }
        }
        catch (Exception ex)
        {
            deactivateModalError = $"Error: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
        }
    }

    // Delete Account Methods
    private void OpenDeleteModal()
    {
        showDeleteModal = true;
        deletePassword = string.Empty;
        deleteModalError = string.Empty;
    }

    private void CloseDeleteModal()
    {
        showDeleteModal = false;
        deletePassword = string.Empty;
        deleteModalError = string.Empty;
    }

    private async Task DeleteAccount()
    {
        try
        {
            isProcessing = true;
            deleteModalError = string.Empty;

            if (string.IsNullOrWhiteSpace(deletePassword))
            {
                deleteModalError = "Please enter your password to confirm";
                return;
            }

            // Use DoctorFacade to delete account (Facade Pattern)
            var result = await DoctorFacade.DeleteAccountAsync(currentUserId, deletePassword);

            if (result.Success)
            {
                // Redirect to logout or home page
                Navigation.NavigateTo("/logout");
            }
            else
            {
                deleteModalError = result.Message;
            }
        }
        catch (Exception ex)
        {
            deleteModalError = $"Error: {ex.Message}";
        }
        finally
        {
            isProcessing = false;
        }
    }

    // Download Data Methods
    private async Task DownloadMyData()
    {
        try
        {
            isDownloading = true;
            StateHasChanged();

            // Use DoctorFacade to download data (Facade Pattern)
            var jsonData = await DoctorFacade.DownloadMyDataAsync(currentUserId);

            if (jsonData == null)
            {
                errorMessage = "Error downloading data";
                return;
            }

            // Convert to bytes
            var bytes = Encoding.UTF8.GetBytes(jsonData);
            var base64 = Convert.ToBase64String(bytes);

            // Trigger download via JavaScript
            var fileName = $"doctor_data_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            await JSRuntime.InvokeVoidAsync("downloadFile", fileName, base64, "application/json");

            successMessage = "Data downloaded successfully";
            StateHasChanged();

            // Clear success message after 3 seconds
            await Task.Delay(3000);
            successMessage = string.Empty;
            StateHasChanged();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error downloading data: {ex.Message}";
        }
        finally
        {
            isDownloading = false;
        }
    }
}
