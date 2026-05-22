using ai_clinic.Models;
using ai_clinic.Data;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services.DoctorRecommendation;

/// <summary>
/// Context class for doctor recommendation using Strategy Pattern
/// Allows dynamic switching between different matching algorithms
/// </summary>
public class DoctorRecommendationService
{
    private IDoctorMatchingStrategy _matchingStrategy;

    /// <summary>
    /// Constructor with default strategy
    /// </summary>
    public DoctorRecommendationService()
    {
        // Default to balanced strategy
        _matchingStrategy = new Strategies.BalancedMatchingStrategy();
    }

    /// <summary>
    /// Constructor with specific strategy
    /// </summary>
    public DoctorRecommendationService(IDoctorMatchingStrategy strategy)
    {
        _matchingStrategy = strategy;
    }

    /// <summary>
    /// Property to dynamically change the matching strategy at runtime
    /// </summary>
    public IDoctorMatchingStrategy MatchingStrategy
    {
        get => _matchingStrategy;
        set => _matchingStrategy = value ?? throw new ArgumentNullException(nameof(value));
    }

    /// <summary>
    /// Get recommended doctors based on search criteria
    /// </summary>
    public async Task<List<DoctorMatchResult>> GetRecommendedDoctorsAsync(DoctorSearchCriteria criteria)
    {
        // Fetch available doctors from database
        var availableDoctors = await GetAvailableDoctorsFromDatabaseAsync();

        // Use the current strategy to match doctors
        var results = _matchingStrategy.MatchDoctors(availableDoctors, criteria);

        return results;
    }

    /// <summary>
    /// Get recommended doctors with a specific strategy (one-time use)
    /// </summary>
    public async Task<List<DoctorMatchResult>> GetRecommendedDoctorsAsync(
        DoctorSearchCriteria criteria, 
        IDoctorMatchingStrategy strategy)
    {
        var availableDoctors = await GetAvailableDoctorsFromDatabaseAsync();
        return strategy.MatchDoctors(availableDoctors, criteria);
    }

    /// <summary>
    /// Compare results from multiple strategies
    /// Useful for testing or showing different recommendation approaches
    /// </summary>
    public async Task<Dictionary<string, List<DoctorMatchResult>>> CompareStrategiesAsync(DoctorSearchCriteria criteria)
    {
        var availableDoctors = await GetAvailableDoctorsFromDatabaseAsync();
        
        var results = new Dictionary<string, List<DoctorMatchResult>>
        {
            ["Symptom-Based"] = new Strategies.SymptomBasedMatchingStrategy()
                .MatchDoctors(availableDoctors, criteria),
            
            ["Specialization-Based"] = new Strategies.SpecializationBasedMatchingStrategy()
                .MatchDoctors(availableDoctors, criteria),
            
            ["Balanced"] = new Strategies.BalancedMatchingStrategy()
                .MatchDoctors(availableDoctors, criteria)
        };

        return results;
    }

    /// <summary>
    /// Get the best matching doctor (top recommendation)
    /// </summary>
    public async Task<DoctorMatchResult?> GetBestMatchAsync(DoctorSearchCriteria criteria)
    {
        var results = await GetRecommendedDoctorsAsync(criteria);
        return results.FirstOrDefault();
    }

    /// <summary>
    /// Fetch available doctors from database
    /// </summary>
    private async Task<List<DoctorProfile>> GetAvailableDoctorsFromDatabaseAsync()
    {
        using var db = DbClient.Instance.GetDb();
        
        return await db.DoctorProfiles
            .Include(d => d.User)
            .Where(d => d.IsActive && 
                       d.IsVerified && 
                       d.IsAcceptingPatients &&
                       d.AvailabilityStatus == DoctorAvailabilityStatus.Available)
            .ToListAsync();
    }

    /// <summary>
    /// Get recommended doctors by symptom (convenience method)
    /// </summary>
    public async Task<List<DoctorMatchResult>> GetDoctorsBySymptomAsync(params string[] symptoms)
    {
        var criteria = new DoctorSearchCriteria
        {
            Symptoms = symptoms.ToList(),
            MaxResults = 5
        };

        // Use symptom-based strategy
        _matchingStrategy = new Strategies.SymptomBasedMatchingStrategy();
        return await GetRecommendedDoctorsAsync(criteria);
    }

    /// <summary>
    /// Get recommended doctors by specialization (convenience method)
    /// </summary>
    public async Task<List<DoctorMatchResult>> GetDoctorsBySpecializationAsync(
        string specialization, 
        int minYearsExperience = 0)
    {
        var criteria = new DoctorSearchCriteria
        {
            PreferredSpecialization = specialization,
            MinYearsOfExperience = minYearsExperience,
            MaxResults = 10
        };

        // Use specialization-based strategy
        _matchingStrategy = new Strategies.SpecializationBasedMatchingStrategy();
        return await GetRecommendedDoctorsAsync(criteria);
    }
}
