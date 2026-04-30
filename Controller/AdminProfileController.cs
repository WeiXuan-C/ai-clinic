namespace ai_clinic.Controller;

public class AdminProfileController(Services.AdminProfileService adminService)
{
    public Task<object?> CreateAdminProfileAsync(CreateAdminProfileRequest request)
    {
        return adminService.CreateAdminProfileAsync(request);
    }

    public Task<object?> GetAdminProfileByIdAsync(string adminId)
    {
        return adminService.GetAdminProfileByIdAsync(adminId);
    }

    public Task<object?> GetAllAdminProfilesAsync()
    {
        return adminService.GetAllAdminProfilesAsync();
    }

    public Task<object?> UpdateAdminProfileAsync(string adminId, UpdateAdminProfileRequest request)
    {
        return adminService.UpdateAdminProfileAsync(adminId, request);
    }

    public Task DeleteAdminProfileAsync(string adminId)
    {
        return adminService.DeleteAdminProfileAsync(adminId);
    }

    public Task<object?> GetSystemStatisticsAsync()
    {
        return adminService.GetSystemStatisticsAsync();
    }
}

public record CreateAdminProfileRequest(string UserId, string FullName, string Email, string? PhoneNumber);
public record UpdateAdminProfileRequest(string? FullName, string? PhoneNumber);
