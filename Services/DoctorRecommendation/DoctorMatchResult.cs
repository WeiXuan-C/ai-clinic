using ai_clinic.Models;

namespace ai_clinic.Services.DoctorRecommendation;

/// <summary>
/// Result of doctor matching with relevance score
/// </summary>
public class DoctorMatchResult
{
    /// <summary>
    /// The matched doctor profile
    /// </summary>
    public DoctorProfile Doctor { get; set; } = null!;

    /// <summary>
    /// Match score (0-100), higher is better
    /// </summary>
    public decimal MatchScore { get; set; }

    /// <summary>
    /// Explanation of why this doctor was matched
    /// </summary>
    public List<string> MatchReasons { get; set; } = new List<string>();

    /// <summary>
    /// Breakdown of score components
    /// </summary>
    public Dictionary<string, decimal> ScoreBreakdown { get; set; } = new Dictionary<string, decimal>();
}
