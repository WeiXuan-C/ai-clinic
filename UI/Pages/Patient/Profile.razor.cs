using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ai_clinic.Services;
using ai_clinic.Models;

namespace ai_clinic.UI.Pages.Patient;

public partial class Profile : ComponentBase
{
    private PatientProfile? profileData;
    private bool isLoading = true;
    private bool isEditing = false;
    private bool isSaving = false;
    private string? errorMessage;
    private string? successMessage;
    private string? photoDataUrl;
    private Guid currentUserId;

    protected override async Task OnInitializedAsync()
    {
        var user = AuthState.CurrentUser;
        if (user == null)
        {
            Navigation.NavigateTo("/auth/signin");
            return;
        }

        currentUserId = user.Id;
        await LoadProfileAsync();
    }

    private async Task LoadProfileAsync()
    {
        try
        {
            isLoading = true;
            errorMessage = null;

            profileData = await PatientProfileService.GetByUserIdAsync(currentUserId);
            
            if (profileData == null)
            {
                // Create new profile
                profileData = new PatientProfile 
                { 
                    UserId = currentUserId,
                    User = AuthState.CurrentUser!
                };
            }
            else
            {
                // Load photo if exists
                if (profileData.ProfilePhoto != null && profileData.ProfilePhoto.Length > 0)
                {
                    photoDataUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(profileData.ProfilePhoto)}";
                }
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading profile: {ex.Message}";
        }
        finally
        {
            isLoading = false;
        }
    }

    private void ToggleEdit()
    {
        isEditing = !isEditing;
        successMessage = null;
        errorMessage = null;
    }

    private async Task SaveProfileAsync()
    {
        try
        {
            isSaving = true;
            errorMessage = null;
            successMessage = null;

            Console.WriteLine($"[Profile] Saving profile for user {currentUserId}");
            Console.WriteLine($"[Profile] Profile ID: {profileData!.Id}");
            Console.WriteLine($"[Profile] Full Name: {profileData.FullName}");

            if (profileData!.Id == Guid.Empty)
            {
                // Create new profile
                Console.WriteLine("[Profile] Creating new profile");
                await PatientProfileService.CreateAsync(profileData);
            }
            else
            {
                // Update existing profile
                Console.WriteLine("[Profile] Updating existing profile");
                await PatientProfileService.UpdateAsync(profileData);
            }

            successMessage = "Profile saved successfully!";
            isEditing = false;
            Console.WriteLine("[Profile] Profile saved successfully");
            
            await LoadProfileAsync();
            
            // Trigger AuthState update to refresh sidebar
            AuthState.NotifyStateChanged();
            Console.WriteLine("[Profile] AuthState change event triggered");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Profile] Error saving profile: {ex.Message}");
            Console.WriteLine($"[Profile] Stack trace: {ex.StackTrace}");
            errorMessage = $"Error saving profile: {ex.Message}";
        }
        finally
        {
            isSaving = false;
            StateHasChanged();
        }
    }

    private void DiscardChanges()
    {
        isEditing = false;
        successMessage = null;
        errorMessage = null;
        _ = LoadProfileAsync();
    }

    private async Task HandlePhotoUpload(InputFileChangeEventArgs e)
    {
        try
        {
            errorMessage = null;
            successMessage = null;

            var file = e.File;
            if (file == null) return;

            Console.WriteLine($"[Profile] Uploading photo: {file.Name}, Size: {file.Size}");

            // Validate file size (5MB)
            if (file.Size > 5 * 1024 * 1024)
            {
                errorMessage = "File size exceeds 5MB limit";
                return;
            }

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
            {
                errorMessage = "Invalid file type. Only JPEG, PNG, and GIF are allowed.";
                return;
            }

            // Read photo data
            using var memoryStream = new MemoryStream();
            await file.OpenReadStream(maxAllowedSize: 5 * 1024 * 1024).CopyToAsync(memoryStream);
            var photoData = memoryStream.ToArray();

            Console.WriteLine($"[Profile] Photo data size: {photoData.Length} bytes");

            // Update profile photo in database
            var success = await PatientProfileService.UpdateProfilePhotoAsync(currentUserId, photoData);
            if (success)
            {
                successMessage = "Photo uploaded successfully!";
                photoDataUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(photoData)}";
                
                // Reload profile to get updated data
                await LoadProfileAsync();
                
                // Trigger AuthState update to refresh sidebar
                AuthState.NotifyStateChanged();
                Console.WriteLine("[Profile] Photo uploaded, profile reloaded, and AuthState change event triggered");
                
                StateHasChanged();
            }
            else
            {
                errorMessage = "Failed to upload photo";
                Console.WriteLine("[Profile] Failed to upload photo");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Profile] Error uploading photo: {ex.Message}");
            Console.WriteLine($"[Profile] Stack trace: {ex.StackTrace}");
            errorMessage = $"Error uploading photo: {ex.Message}";
        }
    }

    private async Task DeletePhotoAsync()
    {
        try
        {
            errorMessage = null;
            successMessage = null;

            var success = await PatientProfileService.UpdateProfilePhotoAsync(currentUserId, null!);
            if (success)
            {
                successMessage = "Photo deleted successfully!";
                photoDataUrl = null;
                StateHasChanged();
            }
            else
            {
                errorMessage = "Failed to delete photo";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error deleting photo: {ex.Message}";
        }
    }
}
