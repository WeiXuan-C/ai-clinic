using ai_clinic.Interfaces;
using ai_clinic.Database;

namespace ai_clinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase HTTP client to IConversationRepository interface
/// </summary>
public class ConversationDAO : IConversationRepository
{
    private readonly SupabaseHttpClient _supabase;

    public ConversationDAO(SupabaseHttpClient supabase)
    {
        _supabase = supabase;
    }

    public async Task<Conversation?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _supabase.GetSingleAsync<Conversation>("conversations", $"id=eq.{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<Conversation>> GetAllAsync()
    {
        return await _supabase.GetAsync<Conversation>("conversations");
    }

    public async Task<Conversation> AddAsync(Conversation entity)
    {
        var result = await _supabase.PostAsync<Conversation>("conversations", entity);
        return result ?? entity;
    }

    public async Task<Conversation> UpdateAsync(Conversation entity)
    {
        var result = await _supabase.PatchAsync<Conversation>("conversations", $"id=eq.{entity.Id}", entity);
        return result ?? entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _supabase.DeleteAsync("conversations", $"id=eq.{id}");
    }

    public async Task<IEnumerable<Conversation>> GetByPatientIdAsync(Guid patientId)
    {
        var filter = $"patient_id=eq.{patientId}&order=last_message_at.desc";
        return await _supabase.GetAsync<Conversation>("conversations", filter);
    }

    public async Task<IEnumerable<Conversation>> GetByDoctorIdAsync(Guid doctorId)
    {
        var filter = $"assigned_doctor_id=eq.{doctorId}&order=last_message_at.desc";
        return await _supabase.GetAsync<Conversation>("conversations", filter);
    }

    public async Task<IEnumerable<Conversation>> GetActiveConversationsAsync()
    {
        var filter = "status=eq.active&order=last_message_at.desc";
        return await _supabase.GetAsync<Conversation>("conversations", filter);
    }

    public async Task<Conversation?> GetActiveConversationByPatientIdAsync(Guid patientId)
    {
        try
        {
            var filter = $"patient_id=eq.{patientId}&status=eq.active&order=last_message_at.desc&limit=1";
            var results = await _supabase.GetAsync<Conversation>("conversations", filter);
            return results.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}
