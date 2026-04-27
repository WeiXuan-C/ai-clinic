using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using Supabase;

namespace AiClinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase client interface to IConversationRepository interface
/// </summary>
public class ConversationDAO : IConversationRepository
{
    private readonly Client _supabase;

    public ConversationDAO(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id)
    {
        var response = await _supabase
            .From<Conversation>()
            .Where(x => x.Id == id)
            .Single();
        
        return response;
    }

    public async Task<IEnumerable<Conversation>> GetAllAsync()
    {
        var response = await _supabase
            .From<Conversation>()
            .Get();
        
        return response.Models;
    }

    public async Task<Conversation> AddAsync(Conversation entity)
    {
        // Entity already created with factory method
        var response = await _supabase
            .From<Conversation>()
            .Insert(entity);
        
        return response.Models.FirstOrDefault() ?? entity;
    }

    public async Task<Conversation> UpdateAsync(Conversation entity)
    {
        // Entity already updated with factory method
        var response = await _supabase
            .From<Conversation>()
            .Where(x => x.Id == entity.Id)
            .Update(entity);
        
        return response.Models.FirstOrDefault() ?? entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _supabase
                .From<Conversation>()
                .Where(x => x.Id == id)
                .Delete();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Conversation>> GetByPatientIdAsync(Guid patientId)
    {
        var response = await _supabase
            .From<Conversation>()
            .Where(x => x.PatientId == patientId)
            .Order("last_message_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        return response.Models;
    }

    public async Task<IEnumerable<Conversation>> GetByDoctorIdAsync(Guid doctorId)
    {
        var response = await _supabase
            .From<Conversation>()
            .Where(x => x.AssignedDoctorId == doctorId)
            .Order("last_message_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        return response.Models;
    }

    public async Task<IEnumerable<Conversation>> GetActiveConversationsAsync()
    {
        var response = await _supabase
            .From<Conversation>()
            .Where(x => x.Status == "active")
            .Order("last_message_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        return response.Models;
    }

    public async Task<Conversation?> GetActiveConversationByPatientIdAsync(Guid patientId)
    {
        var response = await _supabase
            .From<Conversation>()
            .Where(x => x.PatientId == patientId)
            .Where(x => x.Status == "active")
            .Order("last_message_at", Postgrest.Constants.Ordering.Descending)
            .Single();
        
        return response;
    }
}
