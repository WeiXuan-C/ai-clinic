namespace AiClinic.Interfaces;

/// <summary>
/// Factory Design Pattern - Entity Interface
/// Defines the contract for Admin Profile entity structure (attributes/properties as constraints)
/// </summary>
public interface IAdminProfile
{
    // Entity Properties - Constraints
    Guid Id { get; set; }
    Guid UserId { get; set; }
    string FullName { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    bool ManageUsers { get; set; }
    bool ManageAi { get; set; }
    bool ManageDoctors { get; set; }
    bool ManageTickets { get; set; }
    bool ManagePermissions { get; set; }
}

/// <summary>
/// Factory Design Pattern - Repository Interface
/// Defines the contract for Admin Profile repository operations
/// </summary>
public interface IAdminProfileRepository
{
    Task<AdminProfile?> GetByIdAsync(Guid id);
    Task<IEnumerable<AdminProfile>> GetAllAsync();
    Task<AdminProfile> AddAsync(AdminProfile entity);
    Task<AdminProfile> UpdateAsync(AdminProfile entity);
    Task<bool> DeleteAsync(Guid id);
    Task<AdminProfile?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<AdminProfile>> GetByRoleAsync(string role);
    Task<IEnumerable<AdminProfile>> GetActiveAdminsAsync();
}
