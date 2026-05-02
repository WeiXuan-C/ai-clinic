using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

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
}
