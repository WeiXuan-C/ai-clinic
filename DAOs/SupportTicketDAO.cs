using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using Supabase;

namespace AiClinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase client interface to ISupportTicketRepository interface
/// </summary>
public class SupportTicketDAO : ISupportTicketRepository
{
    private readonly Client _supabase;

    public SupportTicketDAO(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<SupportTicket?> GetByIdAsync(Guid id)
    {
        try
        {
            var response = await _supabase
                .From<SupportTicket>()
                .Where(x => x.Id == id)
                .Single();
            
            return response;
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<SupportTicket>> GetAllAsync()
    {
        var response = await _supabase
            .From<SupportTicket>()
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        return response.Models;
    }

    public async Task<SupportTicket> AddAsync(SupportTicket entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        
        var response = await _supabase
            .From<SupportTicket>()
            .Insert(entity);
        
        return response.Models.First();
    }

    public async Task<SupportTicket> UpdateAsync(SupportTicket entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        
        var response = await _supabase
            .From<SupportTicket>()
            .Where(x => x.Id == entity.Id)
            .Update(entity);
        
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _supabase
                .From<SupportTicket>()
                .Where(x => x.Id == id)
                .Delete();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<SupportTicket>> GetByUserIdAsync(Guid userId)
    {
        var response = await _supabase
            .From<SupportTicket>()
            .Where(x => x.UserId == userId)
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        return response.Models;
    }

    public async Task<IEnumerable<SupportTicket>> GetByStatusAsync(string status)
    {
        var response = await _supabase
            .From<SupportTicket>()
            .Where(x => x.Status == status)
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        return response.Models;
    }
}
