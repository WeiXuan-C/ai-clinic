using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

/// <summary>
/// Service for managing Doctor profiles
/// </summary>
public class DoctorProfileService
{
    /// <summary>
    /// Get doctor profile by user ID
    /// </summary>
    public async Task<DoctorProfile?> GetByUserIdAsync(Guid userId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.DoctorProfiles
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == userId);
    }

    /// <summary>
    /// Create a new doctor profile
    /// </summary>
    public async Task<DoctorProfile> CreateAsync(DoctorProfile profile)
    {
        using var db = DbClient.Instance.GetDb();
        db.DoctorProfiles.Add(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    /// <summary>
    /// Get all available doctors
    /// </summary>
    public async Task<List<DoctorProfile>> GetAvailableDoctorsAsync()
    {
        using var db = DbClient.Instance.GetDb();
        return await db.DoctorProfiles
            .Include(d => d.User)
            .Where(d => d.IsActive &&
                       d.IsAcceptingPatients &&
                       d.AvailabilityStatus == DoctorAvailabilityStatus.Available)
            .OrderByDescending(d => d.AverageRating)
            .ToListAsync();
    }

    /// <summary>
    /// Get doctors by specialization
    /// </summary>
    public async Task<List<DoctorProfile>> GetBySpecializationAsync(string specialization)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.DoctorProfiles
            .Include(d => d.User)
            .Where(d => d.IsActive &&
                       d.PrimarySpecialization == specialization)
            .OrderByDescending(d => d.AverageRating)
            .ToListAsync();
    }

    /// <summary>
    /// Update doctor profile
    /// </summary>
    public async Task<DoctorProfile> UpdateAsync(DoctorProfile profile)
    {
        using var db = DbClient.Instance.GetDb();
        profile.UpdatedAt = DateTime.UtcNow;
        db.DoctorProfiles.Update(profile);
        await db.SaveChangesAsync();
        return profile;
    }

    /// <summary>
    /// Update profile photo
    /// </summary>
    public async Task<bool> UpdateProfilePhotoAsync(Guid userId, byte[] photoData)
    {
        using var db = DbClient.Instance.GetDb();
        var profile = await db.DoctorProfiles
            .FirstOrDefaultAsync(d => d.UserId == userId);

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
        var profile = await db.DoctorProfiles
            .FirstOrDefaultAsync(d => d.UserId == userId);

        return profile?.ProfilePhoto;
    }

    /// <summary>
    /// Update doctor availability status
    /// </summary>
    public async Task UpdateAvailabilityAsync(Guid userId, DoctorAvailabilityStatus status)
    {
        using var db = DbClient.Instance.GetDb();
        var doctor = await db.DoctorProfiles
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor != null)
        {
            doctor.AvailabilityStatus = status;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Update doctor rating
    /// </summary>
    public async Task UpdateRatingAsync(Guid userId, decimal newRating, int totalRatings)
    {
        using var db = DbClient.Instance.GetDb();
        var doctor = await db.DoctorProfiles
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor != null)
        {
            doctor.AverageRating = newRating;
            doctor.TotalRatings = totalRatings;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get all doctor profiles
    /// </summary>
    public async Task<List<DoctorProfile>> GetAllAsync()
    {
        using var db = DbClient.Instance.GetDb();
        return await db.DoctorProfiles
            .Include(d => d.User)
            .ToListAsync();
    }

    /// <summary>
    /// Update doctor statistics (Total Consultations, Active Conversations, Ratings)
    /// </summary>
    public async Task UpdateDoctorStatisticsAsync(Guid userId)
    {
        using var db = DbClient.Instance.GetDb();
        var doctor = await db.DoctorProfiles
            .FirstOrDefaultAsync(d => d.UserId == userId);

        if (doctor == null)
        {
            Console.WriteLine($"[DoctorProfileService] Doctor profile not found for userId: {userId}");
            return;
        }

        // Calculate Total Consultations (all conversations assigned to this doctor)
        var allDoctorConversations = await db.Conversations
            .Where(c => c.AssignedDoctorId == userId)
            .ToListAsync();
        
        var totalConsultations = allDoctorConversations.Count;

        Console.WriteLine($"[DoctorProfileService] Found {allDoctorConversations.Count} conversations for doctor {userId}");
        foreach (var conv in allDoctorConversations.Take(5))
        {
            Console.WriteLine($"  - Conversation {conv.Id}: Status={conv.Status}, Title={conv.Title}");
        }

        // Calculate Current Active Conversations
        var activeConversations = allDoctorConversations
            .Count(c => c.Status == ConversationStatus.Active);

        // Calculate Average Rating and Total Ratings
        var ratings = await db.DoctorRatings
            .Where(r => r.DoctorId == userId)
            .ToListAsync();

        var totalRatings = ratings.Count;
        var averageRating = totalRatings > 0 
            ? (decimal)ratings.Average(r => r.Rating) 
            : 0.0m;

        // Update the profile
        doctor.TotalConsultations = totalConsultations;
        doctor.CurrentActiveConversations = activeConversations;
        doctor.AverageRating = averageRating;
        doctor.TotalRatings = totalRatings;
        doctor.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        Console.WriteLine($"[DoctorProfileService] Updated statistics for doctor {userId}:");
        Console.WriteLine($"  - Total Consultations: {totalConsultations}");
        Console.WriteLine($"  - Active Conversations: {activeConversations}");
        Console.WriteLine($"  - Average Rating: {averageRating:F2}");
        Console.WriteLine($"  - Total Ratings: {totalRatings}");
    }
}
