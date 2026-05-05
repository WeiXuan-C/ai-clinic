using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using ai_clinic.Services.Facades;
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

            // Use DoctorFacade instead of DoctorProfileService
            profileData = await DoctorFacade.GetDoctorProfileAsync(currentUserId);
            
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

            // Use DoctorFacade to save profile
            await DoctorFacade.SaveDoctorProfileAsync(profileData);

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
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Size > maxFileSize)
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

            // Show uploading message
            successMessage = "Uploading photo...";
            StateHasChanged();

            // Read photo data with chunked reading for better reliability
            byte[] photoData;
            try
            {
                using var memoryStream = new MemoryStream();
                using var stream = file.OpenReadStream(maxAllowedSize: maxFileSize);
                
                // Read in chunks to avoid timeout issues
                var buffer = new byte[81920]; // 80KB chunks
                int bytesRead;
                var totalBytesRead = 0L;
                
                while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await memoryStream.WriteAsync(buffer, 0, bytesRead);
                    totalBytesRead += bytesRead;
                    
                    // Optional: Update progress
                    if (totalBytesRead % (512 * 1024) == 0) // Every 512KB
                    {
                        Console.WriteLine($"[DoctorProfile] Read {totalBytesRead} / {file.Size} bytes");
                    }
                }
                
                photoData = memoryStream.ToArray();
                Console.WriteLine($"[DoctorProfile] Photo data size: {photoData.Length} bytes");
            }
            catch (Exception readEx)
            {
                Console.WriteLine($"[DoctorProfile] Error reading file stream: {readEx.Message}");
                errorMessage = "Failed to read the image file. Please try again or use a different image.";
                successMessage = null;
                StateHasChanged();
                return;
            }

            // Use DoctorFacade to update profile photo
            var success = await DoctorFacade.UpdateDoctorProfilePhotoAsync(currentUserId, photoData);
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
                successMessage = null;
                Console.WriteLine("[DoctorProfile] Failed to upload photo");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorProfile] Error uploading photo: {ex.Message}");
            Console.WriteLine($"[DoctorProfile] Stack trace: {ex.StackTrace}");
            errorMessage = $"Error uploading photo. Please try again with a smaller image.";
            successMessage = null;
        }
        finally
        {
            StateHasChanged();
        }
    }

    private async Task DeletePhotoAsync()
    {
        try
        {
            errorMessage = null;
            successMessage = null;

            // Use DoctorFacade to delete profile photo
            var success = await DoctorFacade.UpdateDoctorProfilePhotoAsync(currentUserId, null!);
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
