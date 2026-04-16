using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using AiClinic.Infrastructure.Data;

namespace AiClinic.Infrastructure.Repositories;

public class ConversationRepository : IConversationRepository
{
    private readonly SupabaseContext _context;

    public ConversationRepository(SupabaseContext context)
    {
        _context = context;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id)
    {
        var response = await _context.Client
            .From<Conversation>()
            .Where(c => c.Id == id)
            .Single();
        return response;
    }

    public async Task<IEnumerable<Conversation>> GetAllAsync()
    {
        var response = await _context.Client
            .From<Conversation>()
            .Get();
        return response.Models;
    }

    public async Task<Conversation> AddAsync(Conversation entity)
    {
        var response = await _context.Client
            .From<Conversation>()
            .Insert(entity);
        return response.Models.First();
    }

    public async Task<Conversation> UpdateAsync(Conversation entity)
    {
        var response = await _context.Client
            .From<Conversation>()
            .Update(entity);
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _context.Client
            .From<Conversation>()
            .Where(c => c.Id == id)
            .Delete();
        return true;
    }

    public async Task<IEnumerable<Conversation>> GetByPatientIdAsync(Guid patientId)
    {
        var response = await _context.Client
            .From<Conversation>()
            .Where(c => c.PatientId == patientId)
            .Get();
        return response.Models;
    }

    public async Task<IEnumerable<Conversation>> GetByDoctorIdAsync(Guid doctorId)
    {
        var response = await _context.Client
            .From<Conversation>()
            .Where(c => c.AssignedDoctorId == doctorId)
            .Get();
        return response.Models;
    }

    public async Task<Conversation?> GetActiveConversationAsync(Guid patientId)
    {
        var response = await _context.Client
            .From<Conversation>()
            .Where(c => c.PatientId == patientId && c.Status == "active")
            .Single();
        return response;
    }
}
