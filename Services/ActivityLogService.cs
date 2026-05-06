using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

/// <summary>
/// Service for managing Activity Logs
/// Tracks user actions and system events
/// </summary>
public class ActivityLogService
{
    public async Task LogActivityAsync(Guid? userId, string action, string? details = null, string? ipAddress = null)
    {
        var log = new ActivityLog
        {
            UserId = userId,
            Action = action,
            Details = details,
            IpAddress = ipAddress,
            CreatedAt = DateTime.UtcNow
        };

        using var db = DbClient.Instance.GetDb();
        db.ActivityLogs.Add(log);
        await db.SaveChangesAsync();
    }

    public async Task<List<ActivityLog>> GetByUserIdAsync(Guid userId, int limit = 50)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.ActivityLogs
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<ActivityLog>> GetRecentLogsAsync(int limit = 100)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.ActivityLogs
            .Include(al => al.User)
            .OrderByDescending(al => al.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<ActivityLog>> GetByActionAsync(string action, int limit = 50)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.ActivityLogs
            .Include(al => al.User)
            .Where(al => al.Action == action)
            .OrderByDescending(al => al.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<List<ActivityLog>> GetRecentLogsByUserAsync(Guid userId, int limit = 50)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.ActivityLogs
            .Where(al => al.UserId == userId)
            .OrderByDescending(al => al.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }
}
