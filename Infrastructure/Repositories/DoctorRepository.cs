using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using AiClinic.Infrastructure.Data;

namespace AiClinic.Infrastructure.Repositories;

public class DoctorRepository : IDoctorRepository
{
    private readonly SupabaseContext _context;

    public DoctorRepository(SupabaseContext context)
    {
        _context = context;
    }

    public async Task<Doctor?> GetByIdAsync(Guid id)
    {
        var response = await _context.Client
            .From<Doctor>()
            .Where(d => d.Id == id)
            .Single();
        return response;
    }

    public async Task<IEnumerable<Doctor>> GetAllAsync()
    {
        var response = await _context.Client
            .From<Doctor>()
            .Get();
        return response.Models;
    }

    public async Task<Doctor> AddAsync(Doctor entity)
    {
        var response = await _context.Client
            .From<Doctor>()
            .Insert(entity);
        return response.Models.First();
    }

    public async Task<Doctor> UpdateAsync(Doctor entity)
    {
        var response = await _context.Client
            .From<Doctor>()
            .Update(entity);
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _context.Client
            .From<Doctor>()
            .Where(d => d.Id == id)
            .Delete();
        return true;
    }

    public async Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync()
    {
        var response = await _context.Client
            .From<Doctor>()
            .Where(d => d.Status == DoctorStatus.Available)
            .Get();
        return response.Models;
    }

    public async Task<Doctor?> GetByUserIdAsync(Guid userId)
    {
        var response = await _context.Client
            .From<Doctor>()
            .Where(d => d.UserId == userId)
            .Single();
        return response;
    }

    public async Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization)
    {
        var response = await _context.Client
            .From<Doctor>()
            .Where(d => d.Specialization == specialization)
            .Get();
        return response.Models;
    }
}
