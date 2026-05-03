using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ai_clinic.Services;
using ai_clinic.Models;

namespace ai_clinic.UI.Pages.Doctor;

public partial class Profile : ComponentBase
{
    private DoctorProfile? profileData;
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

            profileData = await DoctorProfileService.GetByUserIdAsync(currentUserId);
            
            if (profileData == null)
            {
                // Create new profile
                profileData = new DoctorProfile 
                { 
                    UserId = currentUserId,
                    User = AuthState.CurrentUser!,
                    FullName = "",
                    LicenseNumber = "",
                    PrimarySpecialization = ""
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

            Console.WriteLine($"[DoctorProfile] Saving profile for user {currentUserId}");
            Console.WriteLine($"[DoctorProfile] Profile ID: {profileData!.Id}");
            Console.WriteLine($"[DoctorProfile] Full Name: {profileData.FullName}");

            if (profileData!.Id == Guid.Empty)
            {
                // Create new profile
                Console.WriteLine("[DoctorProfile] Creating new profile");
                await DoctorProfileService.CreateAsync(profileData);
            }
            else
            {
                // Update existing profile
                Console.WriteLine("[DoctorProfile] Updating existing profile");
                await DoctorProfileService.UpdateAsync(profileData);
            }

            successMessage = "Profile saved successfully!";
            isEditing = false;
            Console.WriteLine("[DoctorProfile] Profile saved successfully");
            
            await LoadProfileAsync();
            
            // Trigger AuthState update to refresh sidebar
            AuthState.NotifyStateChanged();
            Console.WriteLine("[DoctorProfile] AuthState change event triggered");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorProfile] Error saving profile: {ex.Message}");
            Console.WriteLine($"[DoctorProfile] Stack trace: {ex.StackTrace}");
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

            Console.WriteLine($"[DoctorProfile] Uploading photo: {file.Name}, Size: {file.Size}");

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

            Console.WriteLine($"[DoctorProfile] Photo data size: {photoData.Length} bytes");

            // Update profile photo in database
            var success = await DoctorProfileService.UpdateProfilePhotoAsync(currentUserId, photoData);
            if (success)
            {
                successMessage = "Photo uploaded successfully!";
                photoDataUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(photoData)}";
                
                // Reload profile to get updated data
                await LoadProfileAsync();
                
                // Trigger AuthState update to refresh sidebar
                AuthState.NotifyStateChanged();
                Console.WriteLine("[DoctorProfile] Photo uploaded, profile reloaded, and AuthState change event triggered");
                
                StateHasChanged();
            }
            else
            {
                errorMessage = "Failed to upload photo";
                Console.WriteLine("[DoctorProfile] Failed to upload photo");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorProfile] Error uploading photo: {ex.Message}");
            Console.WriteLine($"[DoctorProfile] Stack trace: {ex.StackTrace}");
            errorMessage = $"Error uploading photo: {ex.Message}";
        }
    }

    private async Task DeletePhotoAsync()
    {
        try
        {
            errorMessage = null;
            successMessage = null;

            var success = await DoctorProfileService.UpdateProfilePhotoAsync(currentUserId, null!);
            if (success)
            {
                successMessage = "Photo deleted successfully!";
                photoDataUrl = null;
                
                // Trigger AuthState update to refresh sidebar
                AuthState.NotifyStateChanged();
                
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
