using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using AiClinic.Infrastructure.Data;

namespace AiClinic.Infrastructure.Repositories;

public class ActivityLogRepository : IActivityLogRepository
{
    private readonly SupabaseContext _context;

    public ActivityLogRepository(SupabaseContext context)
    {
        _context = context;
    }

    public async Task<ActivityLog?> GetByIdAsync(Guid id)
    {
        var response = await _context.Client
            .From<ActivityLog>()
            .Where(a => a.Id == id)
            .Single();
        return response;
    }

    public async Task<IEnumerable<ActivityLog>> GetAllAsync()
    {
        var response = await _context.Client
            .From<ActivityLog>()
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        return response.Models;
    }

    public async Task<ActivityLog> AddAsync(ActivityLog entity)
    {
        var response = await _context.Client
            .From<ActivityLog>()
            .Insert(entity);
        return response.Models.First();
    }

    public async Task<ActivityLog> UpdateAsync(ActivityLog entity)
    {
        var response = await _context.Client
            .From<ActivityLog>()
            .Update(entity);
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _context.Client
            .From<ActivityLog>()
            .Where(a => a.Id == id)
            .Delete();
        return true;
    }

    public async Task<IEnumerable<ActivityLog>> GetByUserIdAsync(Guid userId, int limit = 50)
    {
        var response = await _context.Client
            .From<ActivityLog>()
            .Where(a => a.UserId == userId)
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Limit(limit)
            .Get();
        return response.Models;
    }

    public async Task<IEnumerable<ActivityLog>> GetByEntityAsync(string entityType, Guid entityId)
    {
        var response = await _context.Client
            .From<ActivityLog>()
            .Where(a => a.EntityType == entityType && a.EntityId == entityId)
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        return response.Models;
    }

    public async Task LogActivityAsync(Guid? userId, string action, string? entityType = null, 
        Guid? entityId = null, string? ipAddress = null, string? userAgent = null, 
        Dictionary<string, object>? details = null)
    {
        var log = new ActivityLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            Details = details,
            CreatedAt = DateTime.UtcNow
        };

        await AddAsync(log);
    }
}
