using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using AiClinic.Infrastructure.Data;

namespace AiClinic.Infrastructure.Repositories;

public class DoctorRatingRepository : IDoctorRatingRepository
{
    private readonly SupabaseContext _context;

    public DoctorRatingRepository(SupabaseContext context)
    {
        _context = context;
    }

    public async Task<DoctorRating?> GetByIdAsync(Guid id)
    {
        var response = await _context.Client
            .From<DoctorRating>()
            .Where(r => r.Id == id)
            .Single();
        return response;
    }

    public async Task<IEnumerable<DoctorRating>> GetAllAsync()
    {
        var response = await _context.Client
            .From<DoctorRating>()
            .Get();
        return response.Models;
    }

    public async Task<DoctorRating> AddAsync(DoctorRating entity)
    {
        var response = await _context.Client
            .From<DoctorRating>()
            .Insert(entity);
        return response.Models.First();
    }

    public async Task<DoctorRating> UpdateAsync(DoctorRating entity)
    {
        var response = await _context.Client
            .From<DoctorRating>()
            .Update(entity);
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _context.Client
            .From<DoctorRating>()
            .Where(r => r.Id == id)
            .Delete();
        return true;
    }

    public async Task<IEnumerable<DoctorRating>> GetByDoctorIdAsync(Guid doctorId)
    {
        var response = await _context.Client
            .From<DoctorRating>()
            .Where(r => r.DoctorId == doctorId)
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        return response.Models;
    }

    public async Task<IEnumerable<DoctorRating>> GetByPatientIdAsync(Guid patientId)
    {
        var response = await _context.Client
            .From<DoctorRating>()
            .Where(r => r.PatientId == patientId)
            .Get();
        return response.Models;
    }

    public async Task<DoctorRating?> GetByConversationIdAsync(Guid conversationId)
    {
        var response = await _context.Client
            .From<DoctorRating>()
            .Where(r => r.ConversationId == conversationId)
            .Single();
        return response;
    }

    public async Task<double> GetAverageRatingAsync(Guid doctorId)
    {
        var ratings = await GetByDoctorIdAsync(doctorId);
        return ratings.Any() ? ratings.Average(r => r.Rating) : 0.0;
    }
}
