using AiClinic.Core.Entities;

namespace AiClinic.Core.Interfaces;

public interface IActivityLogRepository : IRepository<ActivityLog>
{
    Task<IEnumerable<ActivityLog>> GetByUserIdAsync(Guid userId, int limit = 50);
    Task<IEnumerable<ActivityLog>> GetByEntityAsync(string entityType, Guid entityId);
    Task LogActivityAsync(Guid? userId, string action, string? entityType = null, Guid? entityId = null, 
        string? ipAddress = null, string? userAgent = null, Dictionary<string, object>? details = null);
}
