using ai_clinic.Interfaces;
using ai_clinic.Database;

namespace ai_clinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase HTTP client to IDoctorRepository interface
/// </summary>
public class DoctorProfileDAO : IDoctorRepository
{
    private readonly SupabaseHttpClient _supabase;

    public DoctorProfileDAO(SupabaseHttpClient supabase)
    {
        _supabase = supabase;
    }

    public async Task<Doctor?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _supabase.GetSingleAsync<Doctor>("doctor_profiles", $"id=eq.{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<Doctor>> GetAllAsync()
    {
        return await _supabase.GetAsync<Doctor>("doctor_profiles");
    }

    public async Task<Doctor> AddAsync(Doctor entity)
    {
        Console.WriteLine($"🎯 DoctorProfileDAO.AddAsync called");
        Console.WriteLine($"   Entity: Id={entity.Id}, UserId={entity.UserId}, FullName={entity.FullName}");
        Console.WriteLine($"   LicenseNumber={entity.LicenseNumber}, Specialization={entity.PrimarySpecialization}");
        Console.WriteLine($"   CreatedAt={entity.CreatedAt}, UpdatedAt={entity.UpdatedAt}");
        
        Console.WriteLine($"📞 Calling _supabase.PostAsync...");
        var result = await _supabase.PostAsync<Doctor>("doctor_profiles", entity);
        
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

    public async Task<Doctor> UpdateAsync(Doctor entity)
    {
        var result = await _supabase.PatchAsync<Doctor>("doctor_profiles", $"user_id=eq.{entity.UserId}", entity);
        return result ?? entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _supabase.DeleteAsync("doctor_profiles", $"id=eq.{id}");
    }

    public async Task<Doctor?> GetByUserIdAsync(Guid userId)
    {
        try
        {
            return await _supabase.GetSingleAsync<Doctor>("doctor_profiles", $"user_id=eq.{userId}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync()
    {
        var filter = "availability_status=eq.available&is_active=eq.true&is_verified=eq.true";
        return await _supabase.GetAsync<Doctor>("doctor_profiles", filter);
    }

    public async Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization)
    {
        var filter = $"primary_specialization=eq.{specialization}&is_active=eq.true&is_verified=eq.true&order=average_rating.desc";
        return await _supabase.GetAsync<Doctor>("doctor_profiles", filter);
    }

    public async Task UpdateAvailabilityStatusAsync(Guid doctorId, string status)
    {
        var doctor = await GetByIdAsync(doctorId);
        if (doctor != null)
        {
            doctor.AvailabilityStatus = status;
            await UpdateAsync(doctor);
        }
    }

    public async Task<IEnumerable<Doctor>> GetByOrganizationIdAsync(Guid organizationId)
    {
        // OrganizationId property doesn't exist in Doctor entity
        // Return empty list for now
        return [];
    }
}
