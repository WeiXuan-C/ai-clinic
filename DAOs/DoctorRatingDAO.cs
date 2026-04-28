using AiClinic.Interfaces;
using Supabase;

namespace AiClinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase client interface to IDoctorRatingRepository interface
/// Converts JSON responses from Supabase into C# DoctorRating objects
/// </summary>
public class DoctorRatingDAO : IDoctorRatingRepository
{
    private readonly Client _supabase;

    public DoctorRatingDAO(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<DoctorRating?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabase
                .From<DoctorRating>()
                .Where(x => x.Id == id)
                .Single();
            
            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<DoctorRating>> GetAllAsync()
    {
        var response = await _supabase
            .From<DoctorRating>()
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        return response.Models;
    }

    public async Task<DoctorRating> AddAsync(DoctorRating entity)
    {
        var response = await _supabase
            .From<DoctorRating>()
            .Insert(entity);
        
        return response.Models.FirstOrDefault() ?? entity;
    }

    public async Task<DoctorRating> UpdateAsync(DoctorRating entity)
    {
        var response = await _supabase
            .From<DoctorRating>()
            .Where(x => x.Id == entity.Id)
            .Update(entity);
        
        return response.Models.FirstOrDefault() ?? entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _supabase
                .From<DoctorRating>()
                .Where(x => x.Id == id)
                .Delete();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<DoctorRating>> GetByDoctorIdAsync(Guid doctorId)
    {
        var response = await _supabase
            .From<DoctorRating>()
            .Where(x => x.DoctorId == doctorId)
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        return response.Models;
    }

    public async Task<IEnumerable<DoctorRating>> GetByPatientIdAsync(Guid patientId)
    {
        var response = await _supabase
            .From<DoctorRating>()
            .Where(x => x.PatientId == patientId)
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        return response.Models;
    }

    public async Task<double> GetAverageRatingAsync(Guid doctorId)
    {
        var response = await _supabase
            .From<DoctorRating>()
            .Where(x => x.DoctorId == doctorId)
            .Get();
        
        if (!response.Models.Any())
            return 0.0;
        
        return response.Models.Average(r => r.Rating);
    }

    public async Task<int> GetTotalRatingsCountAsync(Guid doctorId)
    {
        var response = await _supabase
            .From<DoctorRating>()
            .Where(x => x.DoctorId == doctorId)
            .Get();
        
        return response.Models.Count;
    }
}
