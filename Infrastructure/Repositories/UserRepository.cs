using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using AiClinic.Infrastructure.Data;

namespace AiClinic.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly SupabaseContext _context;

    public UserRepository(SupabaseContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        var response = await _context.Client
            .From<User>()
            .Where(u => u.Id == id)
            .Single();
        return response;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        var response = await _context.Client
            .From<User>()
            .Get();
        return response.Models;
    }

    public async Task<User> AddAsync(User entity)
    {
        var response = await _context.Client
            .From<User>()
            .Insert(entity);
        return response.Models.First();
    }

    public async Task<User> UpdateAsync(User entity)
    {
        var response = await _context.Client
            .From<User>()
            .Update(entity);
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _context.Client
            .From<User>()
            .Where(u => u.Id == id)
            .Delete();
        return true;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var response = await _context.Client
            .From<User>()
            .Where(u => u.Email == email)
            .Single();
        return response;
    }

    public async Task<bool> ExistsAsync(string email)
    {
        var user = await GetByEmailAsync(email);
        return user != null;
    }
}
