using System.Text.Json.Serialization;

namespace ai_clinic.Interfaces;

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

public class User : IUser
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }
    
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;
    
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
    
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;
    
    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;
    
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
    
    [JsonPropertyName("last_login_at")]
    public DateTime? LastLoginAt { get; set; }
    
    [JsonPropertyName("data_sharing_enabled")]
    public bool DataSharingEnabled { get; set; } = true;
    
    [JsonPropertyName("ai_analysis_enabled")]
    public bool AiAnalysisEnabled { get; set; } = true;
    
    [JsonPropertyName("activity_tracking_enabled")]
    public bool ActivityTrackingEnabled { get; set; } = true;
    
    [JsonPropertyName("is_deactivated")]
    public bool IsDeactivated { get; set; }
    
    [JsonPropertyName("deactivated_at")]
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
