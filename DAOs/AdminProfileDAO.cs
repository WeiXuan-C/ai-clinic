using ai_clinic.Interfaces;
using ai_clinic.Database;

namespace ai_clinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase HTTP client to IAdminProfileRepository interface
/// Converts JSON responses from Supabase into C# AdminProfile objects
/// </summary>
public class AdminProfileDAO : IAdminProfileRepository
{
    private readonly SupabaseHttpClient _supabase;

    public AdminProfileDAO(SupabaseHttpClient supabase)
    {
        _supabase = supabase;
    }

    public async Task<AdminProfile?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _supabase.GetSingleAsync<AdminProfile>("admin_profiles", $"id=eq.{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<AdminProfile>> GetAllAsync()
    {
        var filter = "order=created_at.desc";
        return await _supabase.GetAsync<AdminProfile>("admin_profiles", filter);
    }

    public async Task<AdminProfile> AddAsync(AdminProfile entity)
    {
        var result = await _supabase.PostAsync<AdminProfile>("admin_profiles", entity);
        return result ?? entity;
    }

    public async Task<AdminProfile> UpdateAsync(AdminProfile entity)
    {
        var result = await _supabase.PatchAsync<AdminProfile>("admin_profiles", $"id=eq.{entity.Id}", entity);
        return result ?? entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _supabase.DeleteAsync("admin_profiles", $"id=eq.{id}");
    }

    public async Task<AdminProfile?> GetByUserIdAsync(Guid userId)
    {
        try
        {
            return await _supabase.GetSingleAsync<AdminProfile>("admin_profiles", $"user_id=eq.{userId}");
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
        return await _supabase.GetAsync<AdminProfile>("admin_profiles");
    }

    public async Task<IEnumerable<AdminProfile>> GetActiveAdminsAsync()
    {
        // Returns all admin profiles - adjust if you have an IsActive field
        var filter = "order=created_at.desc";
        return await _supabase.GetAsync<AdminProfile>("admin_profiles", filter);
    }
}
