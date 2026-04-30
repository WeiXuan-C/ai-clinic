using ai_clinic.Interfaces;
using ai_clinic.Database;

namespace ai_clinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase HTTP client to IMessageRepository interface
/// </summary>
public class MessageDAO : IMessageRepository
{
    private readonly SupabaseHttpClient _supabase;

    public MessageDAO(SupabaseHttpClient supabase)
    {
        _supabase = supabase;
    }

    public async Task<Message?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _supabase.GetSingleAsync<Message>("messages", $"id=eq.{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<Message>> GetAllAsync()
    {
        return await _supabase.GetAsync<Message>("messages");
    }

    public async Task<Message> AddAsync(Message entity)
    {
        var result = await _supabase.PostAsync<Message>("messages", entity);
        return result ?? entity;
    }

    public async Task<Message> UpdateAsync(Message entity)
    {
        var result = await _supabase.PatchAsync<Message>("messages", $"id=eq.{entity.Id}", entity);
        return result ?? entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _supabase.DeleteAsync("messages", $"id=eq.{id}");
    }

    public async Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId)
    {
        var filter = $"conversation_id=eq.{conversationId}&order=sent_at.asc";
        return await _supabase.GetAsync<Message>("messages", filter);
    }

    public async Task<Message?> GetLatestMessageAsync(Guid conversationId)
    {
        try
        {
            var filter = $"conversation_id=eq.{conversationId}&order=sent_at.desc&limit=1";
            var results = await _supabase.GetAsync<Message>("messages", filter);
            return results.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId)
    {
        var filter = $"conversation_id=eq.{conversationId}&sender_id=neq.{userId}&is_read=eq.false";
        var results = await _supabase.GetAsync<Message>("messages", filter);
        return results.Count();
    }

    public async Task MarkAsReadAsync(Guid messageId)
    {
        var message = await GetByIdAsync(messageId);
        if (message != null)
        {
            var updatedMessage = message.WithMarkedAsRead();
            await UpdateAsync(updatedMessage);
        }
    }
}
