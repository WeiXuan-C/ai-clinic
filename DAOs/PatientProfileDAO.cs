using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using Supabase;

namespace AiClinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase client interface to IPatientProfileRepository interface
/// </summary>
public class PatientProfileDAO : IPatientProfileRepository
{
    private readonly Client _supabase;

    public PatientProfileDAO(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<PatientProfile?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabase
                .From<PatientProfile>()
                .Where(x => x.Id == id)
                .Single();
            
            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<PatientProfile>> GetAllAsync()
    {
        var response = await _supabase
            .From<PatientProfile>()
            .Get();
        
        return response.Models;
    }

    public async Task<PatientProfile> AddAsync(PatientProfile entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        
        var response = await _supabase
            .From<PatientProfile>()
            .Insert(entity);
        
        return response.Models.First();
    }

    public async Task<PatientProfile> UpdateAsync(PatientProfile entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        
        var response = await _supabase
            .From<PatientProfile>()
            .Where(x => x.UserId == entity.UserId)
            .Update(entity);
        
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _supabase
                .From<PatientProfile>()
                .Where(x => x.Id == id)
                .Delete();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<PatientProfile?> GetByUserIdAsync(Guid userId)
    {
        try
        {
            var response = await _supabase
                .From<PatientProfile>()
                .Where(x => x.UserId == userId)
                .Single();
            
            return response;
        }
        catch
        {
            return null;
        }
    }
}
