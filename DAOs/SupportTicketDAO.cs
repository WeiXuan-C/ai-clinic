using ai_clinic.Interfaces;
using ai_clinic.Database;

namespace ai_clinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase HTTP client to ISupportTicketRepository interface
/// </summary>
public class SupportTicketDAO : ISupportTicketRepository
{
    private readonly SupabaseHttpClient _supabase;

    public SupportTicketDAO(SupabaseHttpClient supabase)
    {
        _supabase = supabase;
    }

    public async Task<SupportTicket?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _supabase.GetSingleAsync<SupportTicket>("support_tickets", $"id=eq.{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<SupportTicket>> GetAllAsync()
    {
        var filter = "order=created_at.desc";
        return await _supabase.GetAsync<SupportTicket>("support_tickets", filter);
    }

    public async Task<SupportTicket> AddAsync(SupportTicket entity)
    {
        var result = await _supabase.PostAsync<SupportTicket>("support_tickets", entity);
        return result ?? entity;
    }

    public async Task<SupportTicket> UpdateAsync(SupportTicket entity)
    {
        var result = await _supabase.PatchAsync<SupportTicket>("support_tickets", $"id=eq.{entity.Id}", entity);
        return result ?? entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _supabase.DeleteAsync("support_tickets", $"id=eq.{id}");
    }

    public async Task<IEnumerable<SupportTicket>> GetByUserIdAsync(Guid userId)
    {
        var filter = $"user_id=eq.{userId}&order=created_at.desc";
        return await _supabase.GetAsync<SupportTicket>("support_tickets", filter);
    }

    public async Task<IEnumerable<SupportTicket>> GetByStatusAsync(string status)
    {
        var filter = $"status=eq.{status}&order=created_at.desc";
        return await _supabase.GetAsync<SupportTicket>("support_tickets", filter);
    }
}
