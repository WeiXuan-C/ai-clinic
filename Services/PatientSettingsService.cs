using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace ai_clinic.Services;

public class PatientSettingsService
{
    public async Task UpdateNotificationPreferencesAsync(Guid userId, bool emailNotifications, bool smsNotifications)
    {
        using var db = DbClient.Instance.GetDb();
        var profile = await db.PatientProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile != null)
        {
            var log = new ActivityLog
            {
                UserId = userId,
                Action = "UpdateNotificationPreferences",
                Details = $"Email: {emailNotifications}, SMS: {smsNotifications}",
                CreatedAt = DateTime.UtcNow
            };
            db.ActivityLogs.Add(log);
            await db.SaveChangesAsync();
        }
    }

    public async Task UpdateProfileAsync(Guid userId, string? fullName, string? address, string? emergencyContactName, string? emergencyContactPhone)
    {
        using var db = DbClient.Instance.GetDb();
        var profile = await db.PatientProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile != null)
        {
            if (!string.IsNullOrEmpty(fullName))
                profile.FullName = fullName;
            if (!string.IsNullOrEmpty(address))
                profile.Address = address;
            if (!string.IsNullOrEmpty(emergencyContactName))
                profile.EmergencyContactName = emergencyContactName;
            if (!string.IsNullOrEmpty(emergencyContactPhone))
                profile.EmergencyContactPhone = emergencyContactPhone;

            profile.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Check if an email address is already in use by another user
    /// </summary>
    public async Task<bool> IsEmailInUseAsync(string email, Guid currentUserId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Users
            .AnyAsync(u => u.Email == email && u.Id != currentUserId);
    }

    /// <summary>
    /// Change user's email address
    /// Validates that the new email is not already in use
    /// </summary>
    public async Task<(bool Success, string Message)> ChangeEmailAsync(Guid userId, string newEmail)
    {
        using var db = DbClient.Instance.GetDb();
        
        // Get the current user
        var user = await db.Users.FindAsync(userId);
        if (user == null)
        {
            return (false, "User not found");
        }

        // Validate email format
        if (string.IsNullOrWhiteSpace(newEmail) || !IsValidEmail(newEmail))
        {
            return (false, "Invalid email format");
        }

        // Check if email is already in use
        var emailInUse = await db.Users
            .AnyAsync(u => u.Email == newEmail && u.Id != userId);
        
        if (emailInUse)
        {
            return (false, "This email address is already in use");
        }

        // Update email
        user.Email = newEmail;
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Log activity
        var log = new ActivityLog
        {
            UserId = userId,
            Action = "ChangeEmail",
            Details = $"Email changed to: {newEmail}",
            CreatedAt = DateTime.UtcNow
        };
        db.ActivityLogs.Add(log);
        await db.SaveChangesAsync();

        return (true, "Email address updated successfully");
    }

    /// <summary>
    /// Change user's password
    /// Validates password complexity and prevents using the same password
    /// </summary>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, string confirmPassword)
    {
        using var db = DbClient.Instance.GetDb();
        
        // Get the current user
        var user = await db.Users.FindAsync(userId);
        if (user == null)
        {
            return (false, "User not found");
        }

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            return (false, "Current password is incorrect");
        }

        // Check if new password is the same as old password
        if (BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash))
        {
            return (false, "New password cannot be the same as the current password");
        }

        // Validate new password matches confirmation
        if (newPassword != confirmPassword)
        {
            return (false, "New password and confirmation password do not match");
        }

        // Validate password complexity
        var validation = ValidatePasswordComplexity(newPassword);
        if (!validation.IsValid)
        {
            return (false, validation.Message);
        }

        // Update password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        user.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Log activity
        var log = new ActivityLog
        {
            UserId = userId,
            Action = "ChangePassword",
            Details = "Password changed successfully",
            CreatedAt = DateTime.UtcNow
        };
        db.ActivityLogs.Add(log);
        await db.SaveChangesAsync();

        return (true, "Password updated successfully");
    }

    /// <summary>
    /// Validate password complexity requirements
    /// Password must have at least 8 characters, contain uppercase, lowercase, number, and symbol
    /// </summary>
    private (bool IsValid, string Message) ValidatePasswordComplexity(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return (false, "Password cannot be empty");
        }

        if (password.Length < 8)
        {
            return (false, "Password must be at least 8 characters long");
        }

        if (!Regex.IsMatch(password, @"[a-z]"))
        {
            return (false, "Password must contain at least one lowercase letter");
        }

        if (!Regex.IsMatch(password, @"[A-Z]"))
        {
            return (false, "Password must contain at least one uppercase letter");
        }

        if (!Regex.IsMatch(password, @"[0-9]"))
        {
            return (false, "Password must contain at least one number");
        }

        if (!Regex.IsMatch(password, @"[!@#$%^&*(),.?""':;{}|<>[\]\\/_\-+=`~]"))
        {
            return (false, "Password must contain at least one special character (!@#$%^&*(),.?\":;{}|<>[]\\/_-+=`~)");
        }

        return (true, "Password is valid");
    }

    /// <summary>
    /// Validate email format
    /// </summary>
    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
