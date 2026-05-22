using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

/// <summary>
/// Service for managing user suspensions
/// </summary>
public class UserSuspensionService
{
    private readonly ActivityLogService _activityLogService;

    public UserSuspensionService(ActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Suspend a user account
    /// </summary>
    public async Task<UserSuspension> SuspendUserAsync(
        Guid userId, 
        Guid adminId, 
        string reason, 
        DateTime? suspensionEnd = null)
    {
        using var db = DbClient.Instance.GetDb();
        
        // Check if user already has an active suspension
        var existingSuspension = await db.UserSuspensions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

        if (existingSuspension != null)
        {
            throw new InvalidOperationException("User already has an active suspension");
        }

        // Create suspension record
        var suspension = new UserSuspension
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SuspendedByAdminId = adminId,
            Reason = reason,
            SuspensionStart = DateTime.UtcNow,
            SuspensionEnd = suspensionEnd,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.UserSuspensions.Add(suspension);

        // Update user status
        var user = await db.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsActive = false;
            user.IsDeactivated = true;
            user.DeactivatedAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;
            db.Users.Update(user);
        }

        await db.SaveChangesAsync();

        // Log activity
        await _activityLogService.LogActivityAsync(
            adminId,
            "USER_SUSPENDED",
            $"User {userId} suspended. Reason: {reason}. End: {suspensionEnd?.ToString("yyyy-MM-dd") ?? "Indefinite"}"
        );

        return suspension;
    }

    /// <summary>
    /// Lift (remove) a user suspension
    /// </summary>
    public async Task<bool> LiftSuspensionAsync(Guid userId, Guid adminId)
    {
        using var db = DbClient.Instance.GetDb();
        
        var suspension = await db.UserSuspensions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

        if (suspension == null)
        {
            return false;
        }

        // Deactivate suspension
        suspension.IsActive = false;
        suspension.LiftedAt = DateTime.UtcNow;
        suspension.LiftedByAdminId = adminId;
        db.UserSuspensions.Update(suspension);

        // Reactivate user
        var user = await db.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsActive = true;
            user.IsDeactivated = false;
            user.DeactivatedAt = null;
            user.UpdatedAt = DateTime.UtcNow;
            db.Users.Update(user);
        }

        await db.SaveChangesAsync();

        // Log activity
        await _activityLogService.LogActivityAsync(
            adminId,
            "USER_SUSPENSION_LIFTED",
            $"Suspension lifted for user {userId}"
        );

        return true;
    }

    /// <summary>
    /// Get active suspension for a user
    /// </summary>
    public async Task<UserSuspension?> GetActiveSuspensionAsync(Guid userId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.UserSuspensions
            .Include(s => s.SuspendedByAdmin)
            .Include(s => s.LiftedByAdmin)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);
    }

    /// <summary>
    /// Get all suspensions for a user (history)
    /// </summary>
    public async Task<List<UserSuspension>> GetUserSuspensionHistoryAsync(Guid userId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.UserSuspensions
            .Include(s => s.SuspendedByAdmin)
            .Include(s => s.LiftedByAdmin)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Check if a user is currently suspended
    /// </summary>
    public async Task<bool> IsUserSuspendedAsync(Guid userId)
    {
        using var db = DbClient.Instance.GetDb();
        var suspension = await db.UserSuspensions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

        if (suspension == null)
            return false;

        // Check if suspension has expired
        if (suspension.SuspensionEnd.HasValue && suspension.SuspensionEnd.Value < DateTime.UtcNow)
        {
            // Auto-lift expired suspension
            await LiftSuspensionAsync(userId, suspension.SuspendedByAdminId);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Get all currently suspended users
    /// </summary>
    public async Task<List<UserSuspension>> GetAllActiveSuspensionsAsync()
    {
        using var db = DbClient.Instance.GetDb();
        return await db.UserSuspensions
            .Include(s => s.User)
            .Include(s => s.SuspendedByAdmin)
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Extend an existing suspension
    /// </summary>
    public async Task<bool> ExtendSuspensionAsync(Guid userId, DateTime newEndDate, Guid adminId)
    {
        using var db = DbClient.Instance.GetDb();
        
        var suspension = await db.UserSuspensions
            .FirstOrDefaultAsync(s => s.UserId == userId && s.IsActive);

        if (suspension == null)
            return false;

        suspension.SuspensionEnd = newEndDate;
        db.UserSuspensions.Update(suspension);
        await db.SaveChangesAsync();

        // Log activity
        await _activityLogService.LogActivityAsync(
            adminId,
            "USER_SUSPENSION_EXTENDED",
            $"Suspension extended for user {userId} until {newEndDate:yyyy-MM-dd}"
        );

        return true;
    }
}
