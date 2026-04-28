using AiClinic.Interfaces;
using Supabase;

namespace AiClinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase client interface to IAdminProfileRepository interface
/// Converts JSON responses from Supabase into C# AdminProfile objects
/// </summary>
public class AdminProfileDAO : IAdminProfileRepository
{
    private readonly Client _supabase;

    public AdminProfileDAO(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<AdminProfile?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabase
                .From<AdminProfile>()
                .Where(x => x.Id == id)
                .Single();
            
            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<AdminProfile>> GetAllAsync()
    {
        var response = await _supabase
            .From<AdminProfile>()
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        return response.Models;
    }

    public async Task<AdminProfile> AddAsync(AdminProfile entity)
    {
        var response = await _supabase
            .From<AdminProfile>()
            .Insert(entity);
        
        return response.Models.FirstOrDefault() ?? entity;
    }

    public async Task<AdminProfile> UpdateAsync(AdminProfile entity)
    {
        var response = await _supabase
            .From<AdminProfile>()
            .Where(x => x.Id == entity.Id)
            .Update(entity);
        
        return response.Models.FirstOrDefault() ?? entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _supabase
                .From<AdminProfile>()
                .Where(x => x.Id == id)
                .Delete();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<AdminProfile?> GetByUserIdAsync(Guid userId)
    {
        try
        {
            var response = await _supabase
                .From<AdminProfile>()
                .Where(x => x.UserId == userId)
                .Single();
            
            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<AdminProfile>> GetByRoleAsync(string role)
    {
        // Note: This assumes there's a role field or we filter by permissions
        // Adjust based on your actual schema
        var response = await _supabase
            .From<AdminProfile>()
            .Get();
        
        // Filter by role logic here if needed
        return response.Models;
    }

    public async Task<IEnumerable<AdminProfile>> GetActiveAdminsAsync()
    {
        // Returns all admin profiles - adjust if you have an IsActive field
        var response = await _supabase
            .From<AdminProfile>()
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        return response.Models;
    }
}
