using ai_clinic.Interfaces;
using ai_clinic.Database;

namespace ai_clinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase HTTP client to IDoctorRatingRepository interface
/// Converts JSON responses from Supabase into C# DoctorRating objects
/// </summary>
public class DoctorRatingDAO : IDoctorRatingRepository
{
    private readonly SupabaseHttpClient _supabase;

    public DoctorRatingDAO(SupabaseHttpClient supabase)
    {
        _supabase = supabase;
    }

    public async Task<DoctorRating?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _supabase.GetSingleAsync<DoctorRating>("doctor_ratings", $"id=eq.{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<DoctorRating>> GetAllAsync()
    {
        var filter = "order=created_at.desc";
        return await _supabase.GetAsync<DoctorRating>("doctor_ratings", filter);
    }

    public async Task<DoctorRating> AddAsync(DoctorRating entity)
    {
        var result = await _supabase.PostAsync<DoctorRating>("doctor_ratings", entity);
        return result ?? entity;
    }

    public async Task<DoctorRating> UpdateAsync(DoctorRating entity)
    {
        var result = await _supabase.PatchAsync<DoctorRating>("doctor_ratings", $"id=eq.{entity.Id}", entity);
        return result ?? entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _supabase.DeleteAsync("doctor_ratings", $"id=eq.{id}");
    }

    public async Task<IEnumerable<DoctorRating>> GetByDoctorIdAsync(Guid doctorId)
    {
        var filter = $"doctor_id=eq.{doctorId}&order=created_at.desc";
        return await _supabase.GetAsync<DoctorRating>("doctor_ratings", filter);
    }

    public async Task<IEnumerable<DoctorRating>> GetByPatientIdAsync(Guid patientId)
    {
        var filter = $"patient_id=eq.{patientId}&order=created_at.desc";
        return await _supabase.GetAsync<DoctorRating>("doctor_ratings", filter);
    }

    public async Task<double> GetAverageRatingAsync(Guid doctorId)
    {
        var ratings = await GetByDoctorIdAsync(doctorId);
        var ratingsList = ratings.ToList();

        if (ratingsList.Count == 0)
            return 0.0;

        return ratingsList.Average(r => r.Rating);
    }

    public async Task<int> GetTotalRatingsCountAsync(Guid doctorId)
    {
        var ratings = await GetByDoctorIdAsync(doctorId);
        return ratings.Count();
    }
}
