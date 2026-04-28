using AiClinic.Interfaces;
using AiClinic.UI.State;

namespace AiClinic.Services;

/// <summary>
/// Admin Profile Service - Business Logic Layer
/// Handles admin profile operations through state management
/// </summary>
public class AdminProfileService
{
    private readonly AdminProfileState _state;

    public AdminProfileService(AdminProfileState state)
    {
        _state = state;
    }

    /// <summary>
    /// Gets an admin profile by ID
    /// </summary>
    public async Task<IAdminProfile?> GetProfileByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets an admin profile by user ID
    /// </summary>
    public async Task<IAdminProfile?> GetProfileByUserIdAsync(Guid userId)
    {
        return await _state.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Gets all admin profiles
    /// </summary>
    public async Task<IEnumerable<IAdminProfile>> GetAllProfilesAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Creates a new admin profile
    /// </summary>
    public async Task<IAdminProfile?> CreateProfileAsync(IAdminProfile profile)
    {
        var concreteProfile = profile as AdminProfile ?? new AdminProfile
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FullName = profile.FullName,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            ManageUsers = profile.ManageUsers,
            ManageAi = profile.ManageAi,
            ManageDoctors = profile.ManageDoctors,
            ManageTickets = profile.ManageTickets,
            ManagePermissions = profile.ManagePermissions
        };
        return await _state.CreateAsync(concreteProfile);
    }

    /// <summary>
    /// Updates an admin profile
    /// </summary>
    public async Task<IAdminProfile?> UpdateProfileAsync(IAdminProfile profile)
    {
        var concreteProfile = profile as AdminProfile ?? new AdminProfile
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FullName = profile.FullName,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt,
            ManageUsers = profile.ManageUsers,
            ManageAi = profile.ManageAi,
            ManageDoctors = profile.ManageDoctors,
            ManageTickets = profile.ManageTickets,
            ManagePermissions = profile.ManagePermissions
        };
        return await _state.UpdateAsync(concreteProfile);
    }

    /// <summary>
    /// Deletes an admin profile
    /// </summary>
    public async Task<bool> DeleteProfileAsync(Guid id)
    {
        return await _state.DeleteAsync(id);
    }

    /// <summary>
    /// Gets the current profile from state
    /// </summary>
    public IAdminProfile? GetCurrentProfile()
    {
        return _state.CurrentProfile;
    }

    /// <summary>
    /// Checks if profile exists in state
    /// </summary>
    public bool HasProfile()
    {
        return _state.HasProfile;
    }

    // Controller-facing methods (adapters for backward compatibility)
    
    public async Task<object> CreateAdminProfileAsync(object request)
    {
        // Extract properties from request object dynamically
        var requestType = request.GetType();
        var userId = Guid.Parse(requestType.GetProperty("UserId")?.GetValue(request)?.ToString() ?? Guid.NewGuid().ToString());
        var fullName = requestType.GetProperty("FullName")?.GetValue(request)?.ToString() ?? string.Empty;
        
        var profile = new AdminProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FullName = fullName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            ManageUsers = false,
            ManageAi = false,
            ManageDoctors = false,
            ManageTickets = false,
            ManagePermissions = false
        };
        
        var result = await CreateProfileAsync(profile);
        return result ?? new object();
    }
    
    public async Task<object?> GetAdminProfileByIdAsync(string adminId)
    {
        if (Guid.TryParse(adminId, out var guid))
        {
            return await GetProfileByIdAsync(guid);
        }
        return null;
    }
    
    public async Task<object> GetAllAdminProfilesAsync()
    {
        return await GetAllProfilesAsync();
    }
    
    public async Task<object> UpdateAdminProfileAsync(string adminId, object request)
    {
        if (!Guid.TryParse(adminId, out var guid))
        {
            throw new ArgumentException("Invalid admin ID");
        }
        
        var existing = await GetProfileByIdAsync(guid);
        if (existing == null)
        {
            throw new KeyNotFoundException("Admin profile not found");
        }
        
        // Extract properties from request object dynamically
        var requestType = request.GetType();
        var fullName = requestType.GetProperty("FullName")?.GetValue(request)?.ToString();
        
        var updated = new AdminProfile
        {
            Id = guid,
            UserId = existing.UserId,
            FullName = fullName ?? existing.FullName,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            ManageUsers = existing.ManageUsers,
            ManageAi = existing.ManageAi,
            ManageDoctors = existing.ManageDoctors,
            ManageTickets = existing.ManageTickets,
            ManagePermissions = existing.ManagePermissions
        };
        
        var result = await UpdateProfileAsync(updated);
        return result ?? new object();
    }
    
    public async Task DeleteAdminProfileAsync(string adminId)
    {
        if (Guid.TryParse(adminId, out var guid))
        {
            await DeleteProfileAsync(guid);
        }
    }
    
    public async Task<object> GetSystemStatisticsAsync()
    {
        // Return basic statistics
        var allProfiles = await GetAllProfilesAsync();
        return new
        {
            TotalAdmins = allProfiles.Count(),
            ActiveAdmins = allProfiles.Count(p => p.ManageUsers || p.ManageAi || p.ManageDoctors || p.ManageTickets || p.ManagePermissions)
        };
    }
}
