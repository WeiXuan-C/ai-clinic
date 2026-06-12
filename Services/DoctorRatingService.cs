using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

/// <summary>
/// Service for managing doctor ratings and reviews
/// </summary>
public class DoctorRatingService
{
    /// <summary>
    /// Check if a patient has already rated a conversation
    /// </summary>
    public async Task<DoctorRating?> GetRatingByConversationAsync(Guid conversationId, Guid patientId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.DoctorRatings
            .Include(r => r.Doctor)
            .Include(r => r.Patient)
            .FirstOrDefaultAsync(r => r.ConversationId == conversationId && r.PatientId == patientId);
    }

    /// <summary>
    /// Create or update a rating for a doctor
    /// </summary>
    public async Task<DoctorRating> SubmitRatingAsync(
        Guid conversationId,
        Guid patientId,
        Guid doctorId,
        int overallRating,
        int? professionalismRating = null,
        int? communicationRating = null,
        int? knowledgeRating = null,
        int? responseTimeRating = null,
        string? reviewText = null)
    {
        using var db = DbClient.Instance.GetDb();

        // Check if rating already exists
        var existingRating = await db.DoctorRatings
            .FirstOrDefaultAsync(r => r.ConversationId == conversationId && r.PatientId == patientId);

        if (existingRating != null)
        {
            // Update existing rating
            existingRating.Rating = overallRating;
            existingRating.ProfessionalismRating = professionalismRating;
            existingRating.CommunicationRating = communicationRating;
            existingRating.KnowledgeRating = knowledgeRating;
            existingRating.ResponseTimeRating = responseTimeRating;
            existingRating.ReviewText = reviewText;

            await db.SaveChangesAsync();
            return existingRating;
        }

        // Create new rating
        var rating = new DoctorRating
        {
            ConversationId = conversationId,
            PatientId = patientId,
            DoctorId = doctorId,
            Rating = overallRating,
            ProfessionalismRating = professionalismRating,
            CommunicationRating = communicationRating,
            KnowledgeRating = knowledgeRating,
            ResponseTimeRating = responseTimeRating,
            ReviewText = reviewText,
            CreatedAt = DateTime.UtcNow
        };

        db.DoctorRatings.Add(rating);
        await db.SaveChangesAsync();

        // Update doctor's average rating
        await UpdateDoctorAverageRatingAsync(doctorId);

        return rating;
    }

    /// <summary>
    /// Update doctor's average rating
    /// </summary>
    private async Task UpdateDoctorAverageRatingAsync(Guid doctorId)
    {
        using var db = DbClient.Instance.GetDb();

        var ratings = await db.DoctorRatings
            .Where(r => r.DoctorId == doctorId)
            .ToListAsync();

        if (ratings.Any())
        {
            var averageRating = ratings.Average(r => r.Rating);
            var doctorProfile = await db.DoctorProfiles
                .FirstOrDefaultAsync(d => d.UserId == doctorId);

            if (doctorProfile != null)
            {
                doctorProfile.AverageRating = (decimal)averageRating;
                doctorProfile.TotalRatings = ratings.Count;
                doctorProfile.UpdatedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();

                // Update all statistics (including consultation counts)
                var doctorProfileService = new DoctorProfileService();
                await doctorProfileService.UpdateDoctorStatisticsAsync(doctorId);
            }
        }
    }

    /// <summary>
    /// Get all ratings for a doctor
    /// </summary>
    public async Task<List<DoctorRating>> GetDoctorRatingsAsync(Guid doctorId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.DoctorRatings
            .Include(r => r.Patient).ThenInclude(p => p.PatientProfile)
            .Where(r => r.DoctorId == doctorId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get doctor's rating statistics
    /// </summary>
    public async Task<DoctorRatingStats> GetDoctorRatingStatsAsync(Guid doctorId)
    {
        using var db = DbClient.Instance.GetDb();

        var ratings = await db.DoctorRatings
            .Where(r => r.DoctorId == doctorId)
            .ToListAsync();

        if (!ratings.Any())
        {
            return new DoctorRatingStats();
        }

        return new DoctorRatingStats
        {
            AverageRating = ratings.Average(r => r.Rating),
            TotalRatings = ratings.Count,
            AverageProfessionalism = ratings.Where(r => r.ProfessionalismRating.HasValue)
                .Average(r => r.ProfessionalismRating!.Value),
            AverageCommunication = ratings.Where(r => r.CommunicationRating.HasValue)
                .Average(r => r.CommunicationRating!.Value),
            AverageKnowledge = ratings.Where(r => r.KnowledgeRating.HasValue)
                .Average(r => r.KnowledgeRating!.Value),
            AverageResponseTime = ratings.Where(r => r.ResponseTimeRating.HasValue)
                .Average(r => r.ResponseTimeRating!.Value),
            FiveStarCount = ratings.Count(r => r.Rating == 5),
            FourStarCount = ratings.Count(r => r.Rating == 4),
            ThreeStarCount = ratings.Count(r => r.Rating == 3),
            TwoStarCount = ratings.Count(r => r.Rating == 2),
            OneStarCount = ratings.Count(r => r.Rating == 1)
        };
    }
}

/// <summary>
/// Doctor rating statistics DTO
/// </summary>
public class DoctorRatingStats
{
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public double AverageProfessionalism { get; set; }
    public double AverageCommunication { get; set; }
    public double AverageKnowledge { get; set; }
    public double AverageResponseTime { get; set; }
    public int FiveStarCount { get; set; }
    public int FourStarCount { get; set; }
    public int ThreeStarCount { get; set; }
    public int TwoStarCount { get; set; }
    public int OneStarCount { get; set; }
}
