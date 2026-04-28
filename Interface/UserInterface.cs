namespace AiClinic.Interfaces;

/// <summary>
/// Factory Design Pattern - Entity Interface
/// Defines the contract for User entity structure (attributes/properties as constraints)
/// </summary>
public interface IUser
{
    // Entity Properties - Constraints
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

/// <summary>
/// Factory Design Pattern - Repository Interface
/// Defines the contract for User repository operations
/// </summary>
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
