using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

/// <summary>
/// Service for managing Patient profiles
/// </summary>
public class PatientProfileService
{
    /// <summary>
    /// Get patient profile by user ID
    /// </summary>
    public async Task<PatientProfile?> GetByUserIdAsync(Guid userId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.PatientProfiles
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    /// <summary>
    /// Create a new patient profile
    /// </summary>
    public async Task<PatientProfile> CreateAsync(PatientProfile profile)
    {
        using var db = DbClient.Instance.GetDb();
        db.PatientProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    /// <summary>
    /// Update patient profile
    /// </summary>
    public async Task<PatientProfile> UpdateAsync(PatientProfile profile)
    {
        using var db = DbClient.Instance.GetDb();
        profile.UpdatedAt = DateTime.UtcNow;
        db.PatientProfiles.Update(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    /// <summary>
    /// Update profile photo
    /// </summary>
    public async Task<bool> UpdateProfilePhotoAsync(Guid userId, byte[] photoData)
    {
        using var db = DbClient.Instance.GetDb();
        var profile = await db.PatientProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return false;
        }

        profile.ProfilePhoto = photoData;
        profile.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Get profile photo
    /// </summary>
    public async Task<byte[]?> GetProfilePhotoAsync(Guid userId)
    {
        using var db = DbClient.Instance.GetDb();
        var profile = await db.PatientProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        return profile?.ProfilePhoto;
    }

    /// <summary>
    /// Get patient's medical history summary
    /// </summary>
    public async Task<string> GetMedicalHistorySummaryAsync(Guid userId)
    {
        using var db = DbClient.Instance.GetDb();
        var profile = await db.PatientProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);

        if (profile == null)
        {
            return "No medical history available";
        }

        return $"Allergies: {profile.Allergies}, " +
               $"Chronic Conditions: {profile.ChronicConditions}, " +
               $"Current Medications: {profile.CurrentMedications}";
    }
}
