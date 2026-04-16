using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using AiClinic.Infrastructure.Data;

namespace AiClinic.Infrastructure.Repositories;

public class PatientProfileRepository : IPatientProfileRepository
{
    private readonly SupabaseContext _context;

    public PatientProfileRepository(SupabaseContext context)
    {
        _context = context;
    }

    public async Task<PatientProfile?> GetByIdAsync(Guid id)
    {
        var response = await _context.Client
            .From<PatientProfile>()
            .Where(p => p.Id == id)
            .Single();
        return response;
    }

    public async Task<IEnumerable<PatientProfile>> GetAllAsync()
    {
        var response = await _context.Client
            .From<PatientProfile>()
            .Get();
        return response.Models;
    }

    public async Task<PatientProfile> AddAsync(PatientProfile entity)
    {
        var response = await _context.Client
            .From<PatientProfile>()
            .Insert(entity);
        return response.Models.First();
    }

    public async Task<PatientProfile> UpdateAsync(PatientProfile entity)
    {
        var response = await _context.Client
            .From<PatientProfile>()
            .Update(entity);
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _context.Client
            .From<PatientProfile>()
            .Where(p => p.Id == id)
            .Delete();
        return true;
    }

    public async Task<PatientProfile?> GetByUserIdAsync(Guid userId)
    {
        var response = await _context.Client
            .From<PatientProfile>()
            .Where(p => p.UserId == userId)
            .Single();
        return response;
    }
}
