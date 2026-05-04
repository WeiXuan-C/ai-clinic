namespace ai_clinic.Services.DoctorRecommendation;

/// <summary>
/// Criteria for searching and matching doctors
/// </summary>
public class DoctorSearchCriteria
{
    /// <summary>
    /// Patient's symptoms (e.g., "headache", "fever", "cough")
    /// </summary>
    public List<string> Symptoms { get; set; } = new List<string>();

    /// <summary>
    /// Preferred specialization (e.g., "Cardiology", "Pediatrics")
    /// </summary>
    public string? PreferredSpecialization { get; set; }

    /// <summary>
    /// Specific conditions (e.g., "diabetes", "hypertension")
    /// </summary>
    public List<string> Conditions { get; set; } = new List<string>();

    /// <summary>
    /// Patient's age group (e.g., "child", "adult", "senior")
    /// </summary>
    public string? AgeGroup { get; set; }

    /// <summary>
    /// Preferred language (e.g., "English", "Chinese")
    /// </summary>
    public string? PreferredLanguage { get; set; }

    /// <summary>
    /// Minimum years of experience required
    /// </summary>
    public int? MinYearsOfExperience { get; set; }

    /// <summary>
    /// Minimum average rating required (0-5)
    /// </summary>
    public decimal? MinRating { get; set; }

    /// <summary>
    /// Maximum number of results to return
    /// </summary>
    public int MaxResults { get; set; } = 10;
}
