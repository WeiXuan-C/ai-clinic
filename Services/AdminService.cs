using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

/// <summary>
/// Service for admin-specific operations
/// Handles user management, doctor verification, and system administration
/// </summary>
public class AdminService
{
    private readonly ActivityLogService _activityLogService;

    public AdminService(ActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Get all users with pagination and filtering
    /// </summary>
    public async Task<PaginatedResult<User>> GetAllUsersAsync(
        int page = 1,
        int pageSize = 20,
        UserRole? roleFilter = null,
        string? searchTerm = null,
        bool? isActiveFilter = null)
    {
        using var db = DbClient.Instance.GetDb();
        var query = db.Users
            .Include(u => u.PatientProfile)
            .Include(u => u.DoctorProfile)
            .Include(u => u.AdminProfile)
            .AsQueryable();

        // Apply filters
        if (roleFilter.HasValue)
        {
            query = query.Where(u => u.Role == roleFilter.Value);
        }

        if (isActiveFilter.HasValue)
        {
            query = query.Where(u => u.IsActive == isActiveFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(u =>
                u.Email.Contains(searchTerm) ||
                (u.PatientProfile != null && u.PatientProfile.FullName.Contains(searchTerm)) ||
                (u.DoctorProfile != null && u.DoctorProfile.FullName.Contains(searchTerm)) ||
                (u.AdminProfile != null && u.AdminProfile.FullName.Contains(searchTerm)));
        }

        var totalCount = await query.CountAsync();
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<User>
        {
            Items = users,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    /// <summary>
    /// Get all doctors with verification status
    /// </summary>
    public async Task<List<DoctorProfile>> GetAllDoctorsAsync(bool? isVerified = null)
    {
        using var db = DbClient.Instance.GetDb();
        var query = db.DoctorProfiles
            .Include(d => d.User)
            .AsQueryable();

        if (isVerified.HasValue)
        {
            query = query.Where(d => d.IsVerified == isVerified.Value);
        }

        return await query
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get pending doctor verifications
    /// </summary>
    public async Task<List<DoctorProfile>> GetPendingVerificationsAsync()
    {
        using var db = DbClient.Instance.GetDb();
        return await db.DoctorProfiles
            .Include(d => d.User)
            .Where(d => !d.IsVerified && d.IsActive)
            .OrderBy(d => d.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Verify or reject a doctor
    /// </summary>
    public async Task<bool> VerifyDoctorAsync(Guid doctorUserId, Guid adminId, bool isVerified, string? notes = null)
    {
        using var db = DbClient.Instance.GetDb();
        var doctor = await db.DoctorProfiles
            .FirstOrDefaultAsync(d => d.UserId == doctorUserId);

        if (doctor == null)
        {
            return false;
        }

        doctor.IsVerified = isVerified;
        doctor.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Log the activity
        await _activityLogService.LogActivityAsync(
            adminId,
            "VerifyDoctor",
            $"Doctor {doctor.FullName} (ID: {doctorUserId}) - Verified: {isVerified}. Notes: {notes ?? "None"}");

        return true;
    }

    /// <summary>
    /// Suspend or unsuspend a user
    /// </summary>
    public async Task<bool> SuspendUserAsync(Guid userId, Guid adminId, bool suspend, string reason)
    {
        using var db = DbClient.Instance.GetDb();
        var user = await db.Users.FindAsync(userId);

        if (user == null)
        {
            return false;
        }

        user.IsActive = !suspend;
        user.IsDeactivated = suspend;
        user.DeactivatedAt = suspend ? DateTime.UtcNow : null;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Log the activity
        await _activityLogService.LogActivityAsync(
            adminId,
            suspend ? "SuspendUser" : "UnsuspendUser",
            $"User {user.Email} (ID: {userId}) - Reason: {reason}");

        return true;
    }

    /// <summary>
    /// Delete a user permanently
    /// </summary>
    public async Task<bool> DeleteUserAsync(Guid userId, Guid adminId, string reason)
    {
        using var db = DbClient.Instance.GetDb();
        var user = await db.Users
            .Include(u => u.PatientProfile)
            .Include(u => u.DoctorProfile)
            .Include(u => u.AdminProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return false;
        }

        // Log before deletion
        await _activityLogService.LogActivityAsync(
            adminId,
            "DeleteUser",
            $"User {user.Email} (ID: {userId}) - Role: {user.Role} - Reason: {reason}");

        // Delete related profiles
        if (user.PatientProfile != null)
        {
            db.PatientProfiles.Remove(user.PatientProfile);
        }
        if (user.DoctorProfile != null)
        {
            db.DoctorProfiles.Remove(user.DoctorProfile);
        }
        if (user.AdminProfile != null)
        {
            db.AdminProfiles.Remove(user.AdminProfile);
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Get admin profile by user ID
    /// </summary>
    public async Task<AdminProfile?> GetAdminProfileAsync(Guid userId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.AdminProfiles
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.UserId == userId);
    }

    /// <summary>
    /// Check if user has admin permission
    /// </summary>
    public async Task<bool> HasPermissionAsync(Guid userId, string permission)
    {
        var admin = await GetAdminProfileAsync(userId);
        if (admin == null)
        {
            return false;
        }

        return permission switch
        {
            "ManageUsers" => admin.ManageUsers,
            "ManageAi" => admin.ManageAi,
            "ManageDoctors" => admin.ManageDoctors,
            "ManageTickets" => admin.ManageTickets,
            "ManagePermissions" => admin.ManagePermissions,
            _ => false
        };
    }

    /// <summary>
    /// Update admin permissions
    /// </summary>
    public async Task<bool> UpdateAdminPermissionsAsync(
        Guid adminUserId,
        Guid updaterAdminId,
        AdminPermissions permissions)
    {
        // Check if updater has permission to manage permissions
        if (!await HasPermissionAsync(updaterAdminId, "ManagePermissions"))
        {
            return false;
        }

        using var db = DbClient.Instance.GetDb();
        var admin = await db.AdminProfiles
            .FirstOrDefaultAsync(a => a.UserId == adminUserId);

        if (admin == null)
        {
            return false;
        }

        admin.ManageUsers = permissions.ManageUsers;
        admin.ManageAi = permissions.ManageAi;
        admin.ManageDoctors = permissions.ManageDoctors;
        admin.ManageTickets = permissions.ManageTickets;
        admin.ManagePermissions = permissions.ManagePermissions;
        admin.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Log the activity
        await _activityLogService.LogActivityAsync(
            updaterAdminId,
            "UpdateAdminPermissions",
            $"Updated permissions for admin {admin.FullName} (ID: {adminUserId})");

        return true;
    }

    /// <summary>
    /// Get system statistics for dashboard
    /// </summary>
    public async Task<AdminDashboardStats> GetDashboardStatsAsync()
    {
        using var db = DbClient.Instance.GetDb();

        var totalUsers = await db.Users.CountAsync(u => u.IsActive);
        var totalDoctors = await db.DoctorProfiles.CountAsync(d => d.IsActive);
        var verifiedDoctors = await db.DoctorProfiles.CountAsync(d => d.IsVerified && d.IsActive);
        var pendingVerifications = await db.DoctorProfiles.CountAsync(d => !d.IsVerified && d.IsActive);
        var totalPatients = await db.PatientProfiles.CountAsync();
        var activeConversations = await db.Conversations.CountAsync(c => c.Status == ConversationStatus.Active);
        var totalConversations = await db.Conversations.CountAsync();
        var openTickets = await db.SupportTickets.CountAsync(t => t.Status == "Open" || t.Status == "In Progress");
        var totalTickets = await db.SupportTickets.CountAsync();

        // Get recent registrations (last 7 days)
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var recentRegistrations = await db.Users
            .Where(u => u.CreatedAt >= sevenDaysAgo)
            .CountAsync();

        return new AdminDashboardStats
        {
            TotalUsers = totalUsers,
            TotalDoctors = totalDoctors,
            VerifiedDoctors = verifiedDoctors,
            PendingVerifications = pendingVerifications,
            TotalPatients = totalPatients,
            ActiveConversations = activeConversations,
            TotalConversations = totalConversations,
            OpenSupportTickets = openTickets,
            TotalSupportTickets = totalTickets,
            RecentRegistrations = recentRegistrations
        };
    }
}

// DTOs
public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class AdminPermissions
{
    public bool ManageUsers { get; set; }
    public bool ManageAi { get; set; }
    public bool ManageDoctors { get; set; }
    public bool ManageTickets { get; set; }
    public bool ManagePermissions { get; set; }
}

public class AdminDashboardStats
{
    public int TotalUsers { get; set; }
    public int TotalDoctors { get; set; }
    public int VerifiedDoctors { get; set; }
    public int PendingVerifications { get; set; }
    public int TotalPatients { get; set; }
    public int ActiveConversations { get; set; }
    public int TotalConversations { get; set; }
    public int OpenSupportTickets { get; set; }
    public int TotalSupportTickets { get; set; }
    public int RecentRegistrations { get; set; }
}
