namespace AiClinic.Controller;

public class AdminProfileController
{
    private readonly Services.AdminProfileService _adminService;

    public AdminProfileController(Services.AdminProfileService adminService)
    {
        _adminService = adminService;
    }

    public async Task<object> CreateAdminProfileAsync(CreateAdminProfileRequest request)
    {
        return await _adminService.CreateAdminProfileAsync(request);
    }

    public async Task<object?> GetAdminProfileByIdAsync(string adminId)
    {
        return await _adminService.GetAdminProfileByIdAsync(adminId);
    }

    public async Task<object> GetAllAdminProfilesAsync()
    {
        return await _adminService.GetAllAdminProfilesAsync();
    }

    public async Task<object> UpdateAdminProfileAsync(string adminId, UpdateAdminProfileRequest request)
    {
        return await _adminService.UpdateAdminProfileAsync(adminId, request);
    }

    public async Task DeleteAdminProfileAsync(string adminId)
    {
        await _adminService.DeleteAdminProfileAsync(adminId);
    }

    public async Task<object> GetSystemStatisticsAsync()
    {
        return await _adminService.GetSystemStatisticsAsync();
    }
}

public record CreateAdminProfileRequest(string UserId, string FullName, string Email, string? PhoneNumber);
public record UpdateAdminProfileRequest(string? FullName, string? PhoneNumber);
