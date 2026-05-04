using ai_clinic.Models;
using System.Text.Json;

namespace ai_clinic.Services.DoctorRecommendation.Strategies;

/// <summary>
/// Concrete Strategy: Matches doctors primarily based on specialization and conditions
/// Best for patients who know their condition and need a specialist
/// </summary>
public class SpecializationBasedMatchingStrategy : IDoctorMatchingStrategy
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

            decimal specializationScore = CalculateSpecializationScore(doctor, criteria, result);
            decimal conditionScore = CalculateConditionScore(doctor, criteria, result);
            decimal experienceScore = CalculateExperienceScore(doctor, criteria, result);
            decimal ratingScore = CalculateRatingScore(doctor, criteria, result);
            decimal symptomScore = CalculateSymptomScore(doctor, criteria, result);
            decimal languageScore = CalculateLanguageScore(doctor, criteria, result);

            // Weighted scoring - specialization and conditions are most important
            result.MatchScore = 
                (specializationScore * 0.35m) + // 35% weight on specialization
                (conditionScore * 0.30m) +      // 30% weight on conditions
                (experienceScore * 0.15m) +     // 15% weight on experience
                (ratingScore * 0.10m) +         // 10% weight on rating
                (symptomScore * 0.05m) +        // 5% weight on symptoms
                (languageScore * 0.05m);        // 5% weight on language

            result.ScoreBreakdown["Specialization"] = specializationScore;
            result.ScoreBreakdown["Conditions"] = conditionScore;
            result.ScoreBreakdown["Experience"] = experienceScore;
            result.ScoreBreakdown["Rating"] = ratingScore;
            result.ScoreBreakdown["Symptoms"] = symptomScore;
            result.ScoreBreakdown["Language"] = languageScore;

            if (result.MatchScore >= 20)
            {
                results.Add(result);
            }
        }

        return results
            .OrderByDescending(r => r.MatchScore)
            .ThenByDescending(r => r.Doctor.AverageRating)
            .Take(criteria.MaxResults)
            .ToList();
    }

    private decimal CalculateSpecializationScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (string.IsNullOrEmpty(criteria.PreferredSpecialization))
            return 50; // Neutral score

        if (doctor.PrimarySpecialization.Equals(criteria.PreferredSpecialization, StringComparison.OrdinalIgnoreCase))
        {
            result.MatchReasons.Add($"Primary specialization: {doctor.PrimarySpecialization}");
            return 100;
        }

        var subSpecs = ParseJsonArray(doctor.SubSpecializations);
        if (subSpecs.Any(s => s.Equals(criteria.PreferredSpecialization, StringComparison.OrdinalIgnoreCase)))
        {
            result.MatchReasons.Add($"Sub-specialization: {criteria.PreferredSpecialization}");
            return 80;
        }

        return 0;
    }

    private decimal CalculateConditionScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (criteria.Conditions == null || !criteria.Conditions.Any())
            return 50; // Neutral score

        var doctorConditions = ParseJsonArray(doctor.ConditionsTreated);
        if (!doctorConditions.Any())
            return 0;

        int matchCount = 0;
        foreach (var condition in criteria.Conditions)
        {
            if (doctorConditions.Any(dc => dc.Contains(condition, StringComparison.OrdinalIgnoreCase)))
            {
                matchCount++;
                result.MatchReasons.Add($"Treats {condition}");
            }
        }

        decimal score = (decimal)matchCount / criteria.Conditions.Count * 100;
        return score;
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
            >= 15 => 100,
            >= 10 => 90,
            >= 5 => 70,
            >= 2 => 50,
            _ => 30
        };

        if (years >= 15)
        {
            result.MatchReasons.Add($"Highly experienced: {years} years");
        }

        return score;
    }

    private decimal CalculateRatingScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (criteria.MinRating.HasValue && doctor.AverageRating < criteria.MinRating.Value)
            return 0;

        decimal score = (doctor.AverageRating / 5.0m) * 100;

        if (doctor.AverageRating >= 4.5m && doctor.TotalRatings >= 20)
        {
            result.MatchReasons.Add($"Excellent rating: {doctor.AverageRating:F1}/5.0");
        }

        return score;
    }

    private decimal CalculateSymptomScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (criteria.Symptoms == null || !criteria.Symptoms.Any())
            return 50;

        var doctorSymptoms = ParseJsonArray(doctor.SymptomsExpertise);
        if (!doctorSymptoms.Any())
            return 0;

        int matchCount = criteria.Symptoms.Count(symptom => 
            doctorSymptoms.Any(ds => ds.Contains(symptom, StringComparison.OrdinalIgnoreCase)));

        return (decimal)matchCount / criteria.Symptoms.Count * 100;
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

        return 0;
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
