using AiClinic.Interfaces;
using Supabase;

namespace AiClinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase client interface to IMessageRepository interface
/// </summary>
public class MessageDAO : IMessageRepository
{
    private readonly Client _supabase;

    public MessageDAO(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<Message?> GetByIdAsync(Guid id)
    {
        var response = await _supabase
            .From<Message>()
            .Where(x => x.Id == id)
            .Single();
        
        return response;
    }

    public async Task<IEnumerable<Message>> GetAllAsync()
    {
        var response = await _supabase
            .From<Message>()
            .Get();
        
        return response.Models;
    }

    public async Task<Message> AddAsync(Message entity)
    {
        var response = await _supabase
            .From<Message>()
            .Insert(entity);
        
        return response.Models.FirstOrDefault() ?? entity;
    }

    public async Task<Message> UpdateAsync(Message entity)
    {
        var response = await _supabase
            .From<Message>()
            .Where(x => x.Id == entity.Id)
            .Update(entity);
        
        return response.Models.FirstOrDefault() ?? entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _supabase
                .From<Message>()
                .Where(x => x.Id == id)
                .Delete();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId)
    {
        var response = await _supabase
            .From<Message>()
            .Where(x => x.ConversationId == conversationId)
            .Order("sent_at", Postgrest.Constants.Ordering.Ascending)
            .Get();
        
        return response.Models;
    }

    public async Task<Message?> GetLatestMessageAsync(Guid conversationId)
    {
        var response = await _supabase
            .From<Message>()
            .Where(x => x.ConversationId == conversationId)
            .Order("sent_at", Postgrest.Constants.Ordering.Descending)
            .Limit(1)
            .Single();
        
        return response;
    }

    public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId)
    {
        var response = await _supabase
            .From<Message>()
            .Where(x => x.ConversationId == conversationId)
            .Where(x => x.SenderId != userId)
            .Where(x => x.IsRead == false)
            .Get();
        
        return response.Models.Count;
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
