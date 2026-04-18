using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using Supabase;

namespace AiClinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase client interface to IUserRepository interface
/// </summary>
public class UserDAO : IUserRepository
{
    private readonly Client _supabase;

    public UserDAO(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        var response = await _supabase
            .From<User>()
            .Where(x => x.Id == id)
            .Single();
        
        return response;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        var response = await _supabase
            .From<User>()
            .Get();
        
        return response.Models;
    }

    public async Task<User> AddAsync(User entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        
        var response = await _supabase
            .From<User>()
            .Insert(entity);
        
        return response.Models.First();
    }

    public async Task<User> UpdateAsync(User entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        
        var response = await _supabase
            .From<User>()
            .Update(entity);
        
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _supabase
                .From<User>()
                .Where(x => x.Id == id)
                .Delete();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var response = await _supabase
            .From<User>()
            .Where(x => x.Email == email.ToLower())
            .Single();
        
        return response;
    }

    public async Task<bool> ExistsAsync(string email)
    {
        var user = await GetByEmailAsync(email);
        return user != null;
    }
}
