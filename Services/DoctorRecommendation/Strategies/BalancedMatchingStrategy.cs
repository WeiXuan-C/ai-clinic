using ai_clinic.Models;
using System.Text.Json;

namespace ai_clinic.Services.DoctorRecommendation.Strategies;

/// <summary>
/// Concrete Strategy: Balanced matching considering all factors equally
/// Best for general recommendations when patient has multiple criteria
/// </summary>
public class BalancedMatchingStrategy : IDoctorMatchingStrategy
{
    public List<DoctorMatchResult> MatchDoctors(List<DoctorProfile> doctors, DoctorSearchCriteria criteria)
    {
        var results = new List<DoctorMatchResult>();

        foreach (var doctor in doctors)
        {
            var result = new DoctorMatchResult
            {
                Doctor = doctor,
                MatchScore = 0,
                MatchReasons = new List<string>(),
                ScoreBreakdown = new Dictionary<string, decimal>()
            };

            decimal symptomScore = CalculateSymptomScore(doctor, criteria, result);
            decimal specializationScore = CalculateSpecializationScore(doctor, criteria, result);
            decimal conditionScore = CalculateConditionScore(doctor, criteria, result);
            decimal experienceScore = CalculateExperienceScore(doctor, criteria, result);
            decimal ratingScore = CalculateRatingScore(doctor, criteria, result);
            decimal availabilityScore = CalculateAvailabilityScore(doctor, result);
            decimal languageScore = CalculateLanguageScore(doctor, criteria, result);
            decimal ageGroupScore = CalculateAgeGroupScore(doctor, criteria, result);

            // Balanced weighted scoring
            result.MatchScore = 
                (symptomScore * 0.20m) +        // 20% weight on symptoms
                (specializationScore * 0.20m) + // 20% weight on specialization
                (conditionScore * 0.15m) +      // 15% weight on conditions
                (experienceScore * 0.15m) +     // 15% weight on experience
                (ratingScore * 0.15m) +         // 15% weight on rating
                (availabilityScore * 0.05m) +   // 5% weight on availability
                (languageScore * 0.05m) +       // 5% weight on language
                (ageGroupScore * 0.05m);        // 5% weight on age group

            result.ScoreBreakdown["Symptoms"] = symptomScore;
            result.ScoreBreakdown["Specialization"] = specializationScore;
            result.ScoreBreakdown["Conditions"] = conditionScore;
            result.ScoreBreakdown["Experience"] = experienceScore;
            result.ScoreBreakdown["Rating"] = ratingScore;
            result.ScoreBreakdown["Availability"] = availabilityScore;
            result.ScoreBreakdown["Language"] = languageScore;
            result.ScoreBreakdown["AgeGroup"] = ageGroupScore;

            if (result.MatchScore >= 20)
            {
                results.Add(result);
            }
        }

        return results
            .OrderByDescending(r => r.MatchScore)
            .ThenByDescending(r => r.Doctor.AverageRating)
            .ThenBy(r => r.Doctor.CurrentActiveConversations)
            .Take(criteria.MaxResults)
            .ToList();
    }

    private decimal CalculateSymptomScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (criteria.Symptoms == null || !criteria.Symptoms.Any())
            return 50;

        var doctorSymptoms = ParseJsonArray(doctor.SymptomsExpertise);
        if (!doctorSymptoms.Any())
            return 25;

        int matchCount = 0;
        foreach (var symptom in criteria.Symptoms)
        {
            if (doctorSymptoms.Any(ds => ds.Contains(symptom, StringComparison.OrdinalIgnoreCase)))
            {
                matchCount++;
                result.MatchReasons.Add($"Expertise in {symptom}");
            }
        }

        return (decimal)matchCount / criteria.Symptoms.Count * 100;
    }

    private decimal CalculateSpecializationScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (string.IsNullOrEmpty(criteria.PreferredSpecialization))
            return 50;

        if (doctor.PrimarySpecialization.Equals(criteria.PreferredSpecialization, StringComparison.OrdinalIgnoreCase))
        {
            result.MatchReasons.Add($"Specialist in {doctor.PrimarySpecialization}");
            return 100;
        }

        var subSpecs = ParseJsonArray(doctor.SubSpecializations);
        if (subSpecs.Any(s => s.Equals(criteria.PreferredSpecialization, StringComparison.OrdinalIgnoreCase)))
        {
            return 75;
        }

        return 25;
    }

    private decimal CalculateConditionScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (criteria.Conditions == null || !criteria.Conditions.Any())
            return 50;

        var doctorConditions = ParseJsonArray(doctor.ConditionsTreated);
        if (!doctorConditions.Any())
            return 25;

        int matchCount = 0;
        foreach (var condition in criteria.Conditions)
        {
            if (doctorConditions.Any(dc => dc.Contains(condition, StringComparison.OrdinalIgnoreCase)))
            {
                matchCount++;
                result.MatchReasons.Add($"Treats {condition}");
            }
        }

        return (decimal)matchCount / criteria.Conditions.Count * 100;
    }

    private decimal CalculateExperienceScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (!doctor.YearsOfExperience.HasValue)
            return 50;

        int years = doctor.YearsOfExperience.Value;

        if (criteria.MinYearsOfExperience.HasValue && years < criteria.MinYearsOfExperience.Value)
            return 0;

        decimal score = years switch
        {
            >= 20 => 100,
            >= 15 => 95,
            >= 10 => 85,
            >= 5 => 70,
            >= 2 => 55,
            _ => 40
        };

        if (years >= 10)
        {
            result.MatchReasons.Add($"{years} years experience");
        }

        return score;
    }

    private decimal CalculateRatingScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (criteria.MinRating.HasValue && doctor.AverageRating < criteria.MinRating.Value)
            return 0;

        decimal score = (doctor.AverageRating / 5.0m) * 100;

        // Bonus for having many reviews
        if (doctor.TotalRatings >= 50)
        {
            score = Math.Min(100, score * 1.05m);
        }

        if (doctor.AverageRating >= 4.5m)
        {
            result.MatchReasons.Add($"Rated {doctor.AverageRating:F1}/5.0");
        }

        return score;
    }

    private decimal CalculateAvailabilityScore(DoctorProfile doctor, DoctorMatchResult result)
    {
        if (doctor.AvailabilityStatus == DoctorAvailabilityStatus.Available)
        {
            result.MatchReasons.Add("Currently available");
            return 100;
        }
        else if (doctor.AvailabilityStatus == DoctorAvailabilityStatus.Busy && doctor.CurrentActiveConversations < 5)
        {
            return 70;
        }
        return 20;
    }

    private decimal CalculateLanguageScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (string.IsNullOrEmpty(criteria.PreferredLanguage))
            return 50;

        var languages = ParseJsonArray(doctor.LanguagesSpoken);
        if (languages.Any(l => l.Equals(criteria.PreferredLanguage, StringComparison.OrdinalIgnoreCase)))
        {
            result.MatchReasons.Add($"Speaks {criteria.PreferredLanguage}");
            return 100;
        }

        return 25;
    }

    private decimal CalculateAgeGroupScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (string.IsNullOrEmpty(criteria.AgeGroup))
            return 50;

        var ageGroups = ParseJsonArray(doctor.AgeGroupsTreated);
        if (ageGroups.Any(ag => ag.Equals(criteria.AgeGroup, StringComparison.OrdinalIgnoreCase)))
        {
            return 100;
        }

        return 25;
    }

    private List<string> ParseJsonArray(string? jsonString)
    {
        if (string.IsNullOrWhiteSpace(jsonString))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(jsonString) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}
