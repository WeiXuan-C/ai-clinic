using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

/// <summary>
/// Service for managing User entities
/// Handles user authentication, registration, and profile management
/// </summary>
public class UserService
{
    /// <summary>
    /// Get user by ID
    /// </summary>
    public async Task<User?> GetByIdAsync(Guid userId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Users
            .Include(u => u.PatientProfile)
            .Include(u => u.DoctorProfile)
            .Include(u => u.AdminProfile)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    /// <summary>
    /// Get user by email
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Users
            .Include(u => u.PatientProfile)
            .Include(u => u.DoctorProfile)
            .Include(u => u.AdminProfile)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    /// <summary>
    /// Create a new user with password hashing
    /// </summary>
    public async Task<User> CreateAsync(User user, string plainPassword)
    {
        // Hash password using BCrypt
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);
        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Verify user password
    /// </summary>
    public async Task<bool> VerifyPasswordAsync(string email, string plainPassword)
    {
        var user = await GetByEmailAsync(email);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return false;
        }

        return BCrypt.Net.BCrypt.Verify(plainPassword, user.PasswordHash);
    }

    /// <summary>
    /// Authenticate user and update last login time
    /// </summary>
    public async Task<User?> AuthenticateAsync(string email, string plainPassword)
    {
        var user = await GetByEmailAsync(email);
        if (user == null || string.IsNullOrEmpty(user.PasswordHash))
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(plainPassword, user.PasswordHash))
        {
            return null;
        }

        // Update last login time
        using var db = DbClient.Instance.GetDb();
        user.LastLoginAt = DateTime.UtcNow;
        db.Users.Update(user);
        await db.SaveChangesAsync();

        return user;
    }

    /// <summary>
    /// Update user information
    /// </summary>
    public async Task<User> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.Users.Update(user);
        await db.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Deactivate user account
    /// </summary>
    public async Task DeactivateAsync(Guid userId)
    {
        using var db = DbClient.Instance.GetDb();
        var user = await db.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get all users by role
    /// </summary>
    public async Task<List<User>> GetByRoleAsync(UserRole role)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Users
            .Where(u => u.Role == role && u.IsActive)
            .ToListAsync();
    }

    /// <summary>
    /// Get all users
    /// </summary>
    public async Task<List<User>> GetAllUsersAsync()
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Users.ToListAsync();
    }

    /// <summary>
    /// Activate user account
    /// </summary>
    public async Task ActivateAsync(Guid userId)
    {
        using var db = DbClient.Instance.GetDb();
        var user = await db.Users.FindAsync(userId);
        if (user != null)
        {
            user.IsActive = true;
            user.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Delete user account
    /// </summary>
    public async Task DeleteAsync(Guid userId)
    {
        using var db = DbClient.Instance.GetDb();
        var user = await db.Users.FindAsync(userId);
        if (user != null)
        {
            db.Users.Remove(user);
            await db.SaveChangesAsync();
        }
    }
}
