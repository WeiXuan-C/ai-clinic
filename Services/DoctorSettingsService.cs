using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ai_clinic.Services;

/// <summary>
/// Service for managing Doctor account settings
/// </summary>
public class DoctorSettingsService
{
    private readonly UserService _userService;

    public DoctorSettingsService(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Change doctor's email address
    /// </summary>
    public async Task<(bool Success, string Message)> ChangeEmailAsync(Guid userId, string currentPassword, string newEmail)
    {
        try
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                return (false, "Current password is incorrect");
            }

            // Check if new email is already in use
            var existingUser = await _userService.GetByEmailAsync(newEmail);
            if (existingUser != null && existingUser.Id != userId)
            {
                return (false, "Email address is already in use");
            }

            // Update email
            user.Email = newEmail;
            await _userService.UpdateAsync(user);

            return (true, "Email address updated successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorSettingsService] Error changing email: {ex.Message}");
            return (false, "An error occurred while updating email");
        }
    }

    /// <summary>
    /// Change doctor's password
    /// </summary>
    public async Task<(bool Success, string Message)> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
    {
        try
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
            {
                return (false, "Current password is incorrect");
            }

            // Validate new password
            if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
            {
                return (false, "New password must be at least 6 characters long");
            }

            // Hash and update password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await _userService.UpdateAsync(user);

            return (true, "Password updated successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorSettingsService] Error changing password: {ex.Message}");
            return (false, "An error occurred while updating password");
        }
    }

    /// <summary>
    /// Deactivate doctor account (temporarily)
    /// </summary>
    public async Task<(bool Success, string Message)> DeactivateAccountAsync(Guid userId, string password)
    {
        try
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return (false, "Password is incorrect");
            }

            // Deactivate account
            user.IsDeactivated = true;
            user.DeactivatedAt = DateTime.UtcNow;
            user.IsActive = false;
            await _userService.UpdateAsync(user);

            return (true, "Account deactivated successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorSettingsService] Error deactivating account: {ex.Message}");
            return (false, "An error occurred while deactivating account");
        }
    }

    /// <summary>
    /// Delete doctor account permanently
    /// </summary>
    public async Task<(bool Success, string Message)> DeleteAccountAsync(Guid userId, string password)
    {
        try
        {
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                return (false, "Password is incorrect");
            }

            // Delete the user account
            await _userService.DeleteAsync(userId);

            return (true, "Account deleted successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorSettingsService] Error deleting account: {ex.Message}");
            return (false, "An error occurred while deleting account");
        }
    }

    /// <summary>
    /// Download doctor's data in JSON format
    /// </summary>
    public async Task<string?> DownloadMyDataAsync(Guid userId)
    {
        try
        {
            using var db = DbClient.Instance.GetDb();

            // Get user data
            var user = await db.Users
                .Include(u => u.DoctorProfile)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            // Get all related data
            var conversations = await db.Conversations
                .Where(c => c.AssignedDoctorId == userId)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Status,
                    c.CreatedAt,
                    c.UpdatedAt
                })
                .ToListAsync();

            var messages = await db.Messages
                .Where(m => m.SenderId == userId)
                .Select(m => new
                {
                    m.Id,
                    m.ConversationId,
                    m.Content,
                    m.CreatedAt
                })
                .ToListAsync();

            var consultationNotes = await db.ConsultationNotes
                .Where(cn => cn.DoctorId == userId)
                .Select(cn => new
                {
                    cn.Id,
                    cn.ConversationId,
                    cn.Diagnosis,
                    cn.TreatmentPlan,
                    cn.CreatedAt
                })
                .ToListAsync();

            var prescriptions = await db.Prescriptions
                .Where(p => p.DoctorId == userId)
                .Select(p => new
                {
                    p.Id,
                    p.ConsultationNoteId,
                    p.MedicationName,
                    p.Dosage,
                    p.Frequency,
                    p.Instructions,
                    p.CreatedAt
                })
                .ToListAsync();

            var medicalRecords = await db.MedicalRecords
                .Where(mr => mr.CreatedByDoctorId == userId)
                .Select(mr => new
                {
                    mr.Id,
                    mr.ConversationId,
                    mr.DiagnosisDescription,
                    mr.Content,
                    mr.CreatedAt
                })
                .ToListAsync();

            var ratings = await db.DoctorRatings
                .Where(r => r.DoctorId == userId)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.ReviewText,
                    r.CreatedAt
                })
                .ToListAsync();

            // Compile all data
            var doctorData = new
            {
                ExportedAt = DateTime.UtcNow,
                User = new
                {
                    user.Id,
                    user.Email,
                    user.Phone,
                    user.Role,
                    user.CreatedAt,
                    user.LastLoginAt
                },
                DoctorProfile = user.DoctorProfile == null ? null : new
                {
                    user.DoctorProfile.FullName,
                    user.DoctorProfile.Title,
                    user.DoctorProfile.LicenseNumber,
                    user.DoctorProfile.PrimarySpecialization,
                    user.DoctorProfile.SubSpecializations,
                    user.DoctorProfile.YearsOfExperience,
                    user.DoctorProfile.AverageRating,
                    user.DoctorProfile.TotalConsultations,
                    user.DoctorProfile.TotalRatings,
                    user.DoctorProfile.CreatedAt
                },
                Conversations = conversations,
                Messages = messages,
                ConsultationNotes = consultationNotes,
                Prescriptions = prescriptions,
                MedicalRecords = medicalRecords,
                Ratings = ratings
            };

            // Serialize to JSON with pretty formatting
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return JsonSerializer.Serialize(doctorData, options);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DoctorSettingsService] Error downloading data: {ex.Message}");
            return null;
        }
    }
}
