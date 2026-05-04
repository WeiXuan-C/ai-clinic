namespace ai_clinic.Services.DoctorRecommendation;

/// <summary>
/// Strategy interface for doctor matching algorithms
/// Defines the contract for different doctor recommendation strategies
/// </summary>
public interface IDoctorMatchingStrategy
{
    /// <summary>
    /// Match and score doctors based on patient criteria
    /// </summary>
    /// <param name="doctors">List of available doctors</param>
    /// <param name="criteria">Patient search criteria</param>
    /// <returns>List of doctors with match scores, sorted by relevance</returns>
    List<DoctorMatchResult> MatchDoctors(List<Models.DoctorProfile> doctors, DoctorSearchCriteria criteria);
}
