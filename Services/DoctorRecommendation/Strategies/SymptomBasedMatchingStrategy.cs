using ai_clinic.Models;
using System.Text.Json;

namespace ai_clinic.Services.DoctorRecommendation.Strategies;

/// <summary>
/// Concrete Strategy: Matches doctors primarily based on symptom expertise
/// Best for patients who know their symptoms but not the condition
/// </summary>
public class SymptomBasedMatchingStrategy : IDoctorMatchingStrategy
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
            decimal experienceScore = CalculateExperienceScore(doctor, criteria, result);
            decimal ratingScore = CalculateRatingScore(doctor, criteria, result);
            decimal languageScore = CalculateLanguageScore(doctor, criteria, result);
            decimal ageGroupScore = CalculateAgeGroupScore(doctor, criteria, result);

            // Weighted scoring - symptoms are most important in this strategy
            result.MatchScore = 
                (symptomScore * 0.40m) +        // 40% weight on symptoms
                (specializationScore * 0.20m) + // 20% weight on specialization
                (experienceScore * 0.15m) +     // 15% weight on experience
                (ratingScore * 0.15m) +         // 15% weight on rating
                (languageScore * 0.05m) +       // 5% weight on language
                (ageGroupScore * 0.05m);        // 5% weight on age group

            result.ScoreBreakdown["Symptoms"] = symptomScore;
            result.ScoreBreakdown["Specialization"] = specializationScore;
            result.ScoreBreakdown["Experience"] = experienceScore;
            result.ScoreBreakdown["Rating"] = ratingScore;
            result.ScoreBreakdown["Language"] = languageScore;
            result.ScoreBreakdown["AgeGroup"] = ageGroupScore;

            // Only include doctors with minimum match score
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

    private decimal CalculateSymptomScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (criteria.Symptoms == null || !criteria.Symptoms.Any())
            return 0;

        var doctorSymptoms = ParseJsonArray(doctor.SymptomsExpertise);
        if (!doctorSymptoms.Any())
            return 0;

        int matchCount = 0;
        foreach (var symptom in criteria.Symptoms)
        {
            if (doctorSymptoms.Any(ds => ds.Contains(symptom, StringComparison.OrdinalIgnoreCase)))
            {
                matchCount++;
                result.MatchReasons.Add($"Expertise in treating {symptom}");
            }
        }

        decimal score = (decimal)matchCount / criteria.Symptoms.Count * 100;
        return score;
    }

    private decimal CalculateSpecializationScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (string.IsNullOrEmpty(criteria.PreferredSpecialization))
            return 50; // Neutral score if no preference

        if (doctor.PrimarySpecialization.Equals(criteria.PreferredSpecialization, StringComparison.OrdinalIgnoreCase))
        {
            result.MatchReasons.Add($"Primary specialization: {doctor.PrimarySpecialization}");
            return 100;
        }

        var subSpecs = ParseJsonArray(doctor.SubSpecializations);
        if (subSpecs.Any(s => s.Equals(criteria.PreferredSpecialization, StringComparison.OrdinalIgnoreCase)))
        {
            result.MatchReasons.Add($"Sub-specialization: {criteria.PreferredSpecialization}");
            return 75;
        }

        return 0;
    }

    private decimal CalculateExperienceScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (!doctor.YearsOfExperience.HasValue)
            return 50; // Neutral score

        int years = doctor.YearsOfExperience.Value;

        if (criteria.MinYearsOfExperience.HasValue && years < criteria.MinYearsOfExperience.Value)
            return 0;

        // Score based on experience: 0-5 years = 60, 5-10 = 80, 10+ = 100
        decimal score = years switch
        {
            >= 10 => 100,
            >= 5 => 80,
            >= 2 => 60,
            _ => 40
        };

        if (years >= 10)
        {
            result.MatchReasons.Add($"{years} years of experience");
        }

        return score;
    }

    private decimal CalculateRatingScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (criteria.MinRating.HasValue && doctor.AverageRating < criteria.MinRating.Value)
            return 0;

        // Convert 0-5 rating to 0-100 score
        decimal score = (doctor.AverageRating / 5.0m) * 100;

        if (doctor.AverageRating >= 4.5m && doctor.TotalRatings >= 10)
        {
            result.MatchReasons.Add($"Highly rated: {doctor.AverageRating:F1}/5.0 ({doctor.TotalRatings} reviews)");
        }

        return score;
    }

    private decimal CalculateLanguageScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (string.IsNullOrEmpty(criteria.PreferredLanguage))
            return 50; // Neutral score

        var languages = ParseJsonArray(doctor.LanguagesSpoken);
        if (languages.Any(l => l.Equals(criteria.PreferredLanguage, StringComparison.OrdinalIgnoreCase)))
        {
            result.MatchReasons.Add($"Speaks {criteria.PreferredLanguage}");
            return 100;
        }

        return 0;
    }

    private decimal CalculateAgeGroupScore(DoctorProfile doctor, DoctorSearchCriteria criteria, DoctorMatchResult result)
    {
        if (string.IsNullOrEmpty(criteria.AgeGroup))
            return 50; // Neutral score

        var ageGroups = ParseJsonArray(doctor.AgeGroupsTreated);
        if (ageGroups.Any(ag => ag.Equals(criteria.AgeGroup, StringComparison.OrdinalIgnoreCase)))
        {
            result.MatchReasons.Add($"Treats {criteria.AgeGroup} patients");
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
