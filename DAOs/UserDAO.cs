using ai_clinic.Interfaces;
using ai_clinic.Database;

namespace ai_clinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase HTTP client to IUserRepository interface
/// </summary>
public class UserDAO : IUserRepository
{
    private readonly SupabaseHttpClient _supabase;

    public UserDAO(SupabaseHttpClient supabase)
    {
        _supabase = supabase;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _supabase.GetSingleAsync<User>("users", $"id=eq.{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _supabase.GetAsync<User>("users");
    }

    public async Task<User> AddAsync(User entity)
    {
        var result = await _supabase.PostAsync<User>("users", entity);
        return result ?? entity;
    }

    public async Task<User> UpdateAsync(User entity)
    {
        var result = await _supabase.PatchAsync<User>("users", $"id=eq.{entity.Id}", entity);
        return result ?? entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _supabase.DeleteAsync("users", $"id=eq.{id}");
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        try
        {
            Console.WriteLine($"🔍 DATABASE: Querying users table for email: {email}");
            
            var user = await _supabase.GetSingleAsync<User>("users", $"email=eq.{email.ToLower()}");
            
            if (user != null)
            {
                Console.WriteLine($"✅ DATABASE: User found - ID: {user.Id}, Email: {user.Email}, Role: {user.Role}");
            }
            else
            {
                Console.WriteLine($"❌ DATABASE: No user found for email: {email}");
            }
            
            return user;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ DATABASE ERROR: {ex.Message}");
            Console.WriteLine($"❌ STACK TRACE: {ex.StackTrace}");
            return null;
        }
    }

    public async Task<bool> ExistsAsync(string email)
    {
        try
        {
            var user = await GetByEmailAsync(email);
            return user != null;
        }
        catch
        {
            return false;
        }
    }
}
