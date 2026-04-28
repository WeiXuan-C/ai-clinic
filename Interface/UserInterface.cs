using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Interfaces;

public interface IUser
{
    Guid Id { get; set; }
    string Email { get; set; }
    string? Phone { get; set; }
    string Role { get; set; }
    bool IsActive { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    DateTime? LastLoginAt { get; set; }
    bool DataSharingEnabled { get; set; }
    bool AiAnalysisEnabled { get; set; }
    bool ActivityTrackingEnabled { get; set; }
    bool IsDeactivated { get; set; }
    DateTime? DeactivatedAt { get; set; }
}

public class User : BaseModel, IUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public bool DataSharingEnabled { get; set; } = true;
    public bool AiAnalysisEnabled { get; set; } = true;
    public bool ActivityTrackingEnabled { get; set; } = true;
    public bool IsDeactivated { get; set; }
    public DateTime? DeactivatedAt { get; set; }

    public User WithUpdatedLogin()
    {
        return new User
        {
            Id = this.Id,
            Email = this.Email,
            Phone = this.Phone,
            Role = this.Role,
            IsActive = this.IsActive,
            CreatedAt = this.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
            DataSharingEnabled = this.DataSharingEnabled,
            AiAnalysisEnabled = this.AiAnalysisEnabled,
            ActivityTrackingEnabled = this.ActivityTrackingEnabled,
            IsDeactivated = this.IsDeactivated,
            DeactivatedAt = this.DeactivatedAt
        };
    }
}

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<IEnumerable<User>> GetAllAsync();
    Task<User> AddAsync(User entity);
    Task<User> UpdateAsync(User entity);
    Task<bool> DeleteAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<bool> ExistsAsync(string email);
}
