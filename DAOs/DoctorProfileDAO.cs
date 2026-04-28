using AiClinic.Interfaces;
using Supabase;

namespace AiClinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase client interface to IDoctorRepository interface
/// </summary>
public class DoctorProfileDAO : IDoctorRepository
{
    private readonly Client _supabase;

    public DoctorProfileDAO(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<Doctor?> GetByIdAsync(Guid id)
    {
        var response = await _supabase
            .From<Doctor>()
            .Where(x => x.Id == id)
            .Single();
        
        return response;
    }

    public async Task<IEnumerable<Doctor>> GetAllAsync()
    {
        var response = await _supabase
            .From<Doctor>()
            .Get();
        
        return response.Models;
    }

    public async Task<Doctor> AddAsync(Doctor entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        
        var response = await _supabase
            .From<Doctor>()
            .Insert(entity);
        
        return response.Models.First();
    }

    public async Task<Doctor> UpdateAsync(Doctor entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        
        var response = await _supabase
            .From<Doctor>()
            .Where(x => x.UserId == entity.UserId)
            .Update(entity);
        
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _supabase
                .From<Doctor>()
                .Where(x => x.Id == id)
                .Delete();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<Doctor?> GetByUserIdAsync(Guid userId)
    {
        var response = await _supabase
            .From<Doctor>()
            .Where(x => x.UserId == userId)
            .Single();
        
        return response;
    }

    public async Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync()
    {
        var response = await _supabase
            .From<Doctor>()
            .Where(x => x.AvailabilityStatus == "available")
            .Where(x => x.IsActive == true)
            .Where(x => x.IsVerified == true)
            .Get();
        
        return response.Models;
    }

    public async Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization)
    {
        var response = await _supabase
            .From<Doctor>()
            .Where(x => x.PrimarySpecialization == specialization)
            .Where(x => x.IsActive == true)
            .Where(x => x.IsVerified == true)
            .Order("average_rating", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        return response.Models;
    }

    public async Task<IEnumerable<Doctor>> GetByOrganizationIdAsync(Guid organizationId)
    {
        // OrganizationId property doesn't exist in Doctor entity
        // Return empty list for now
        return Enumerable.Empty<Doctor>();
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
}
