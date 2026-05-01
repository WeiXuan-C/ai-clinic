using ai_clinic.Interfaces;
using ai_clinic.Database;

namespace ai_clinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase HTTP client to IPatientProfileRepository interface
/// </summary>
public class PatientProfileDAO : IPatientProfileRepository
{
    private readonly SupabaseHttpClient _supabase;

    public PatientProfileDAO(SupabaseHttpClient supabase)
    {
        _supabase = supabase;
    }

    public async Task<PatientProfile?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _supabase.GetSingleAsync<PatientProfile>("patient_profiles", $"id=eq.{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<PatientProfile>> GetAllAsync()
    {
        return await _supabase.GetAsync<PatientProfile>("patient_profiles");
    }

    public async Task<PatientProfile> AddAsync(PatientProfile entity)
    {
        Console.WriteLine($"🎯 PatientProfileDAO.AddAsync called");
        Console.WriteLine($"   Entity: Id={entity.Id}, UserId={entity.UserId}, FullName={entity.FullName}");
        Console.WriteLine($"   CreatedAt={entity.CreatedAt}, UpdatedAt={entity.UpdatedAt}");
        
        Console.WriteLine($"📞 Calling _supabase.PostAsync...");
        var result = await _supabase.PostAsync<PatientProfile>("patient_profiles", entity);
        
        if (result == null)
        {
            Console.WriteLine($"❌ _supabase.PostAsync returned null");
        }
        else
        {
            Console.WriteLine($"✅ _supabase.PostAsync succeeded: {result.Id}");
        }
        
        return result ?? entity;
    }

    public async Task<PatientProfile> UpdateAsync(PatientProfile entity)
    {
        var result = await _supabase.PatchAsync<PatientProfile>("patient_profiles", $"user_id=eq.{entity.UserId}", entity);
        return result ?? entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _supabase.DeleteAsync("patient_profiles", $"id=eq.{id}");
    }

    public async Task<PatientProfile?> GetByUserIdAsync(Guid userId)
    {
        try
        {
            return await _supabase.GetSingleAsync<PatientProfile>("patient_profiles", $"user_id=eq.{userId}");
        }
        catch
        {
            return null;
        }
    }
}
