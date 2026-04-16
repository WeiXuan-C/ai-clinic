using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using AiClinic.Infrastructure.Data;

namespace AiClinic.Infrastructure.Repositories;

public class OrganizationRepository : IOrganizationRepository
{
    private readonly SupabaseContext _context;

    public OrganizationRepository(SupabaseContext context)
    {
        _context = context;
    }

    public async Task<Organization?> GetByIdAsync(Guid id)
    {
        var response = await _context.Client
            .From<Organization>()
            .Where(o => o.Id == id)
            .Single();
        return response;
    }

    public async Task<IEnumerable<Organization>> GetAllAsync()
    {
        var response = await _context.Client
            .From<Organization>()
            .Get();
        return response.Models;
    }

    public async Task<Organization> AddAsync(Organization entity)
    {
        var response = await _context.Client
            .From<Organization>()
            .Insert(entity);
        return response.Models.First();
    }

    public async Task<Organization> UpdateAsync(Organization entity)
    {
        var response = await _context.Client
            .From<Organization>()
            .Update(entity);
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _context.Client
            .From<Organization>()
            .Where(o => o.Id == id)
            .Delete();
        return true;
    }

    public async Task<IEnumerable<Organization>> GetVerifiedOrganizationsAsync()
    {
        var response = await _context.Client
            .From<Organization>()
            .Where(o => o.IsVerified == true && o.IsActive == true)
            .Get();
        return response.Models;
    }

    public async Task<Organization?> GetByNameAsync(string name)
    {
        var response = await _context.Client
            .From<Organization>()
            .Where(o => o.Name == name)
            .Single();
        return response;
    }
}
