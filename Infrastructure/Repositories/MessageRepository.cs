using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using AiClinic.Infrastructure.Data;

namespace AiClinic.Infrastructure.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly SupabaseContext _context;

    public MessageRepository(SupabaseContext context)
    {
        _context = context;
    }

    public async Task<Message?> GetByIdAsync(Guid id)
    {
        var response = await _context.Client
            .From<Message>()
            .Where(m => m.Id == id)
            .Single();
        return response;
    }

    public async Task<IEnumerable<Message>> GetAllAsync()
    {
        var response = await _context.Client
            .From<Message>()
            .Get();
        return response.Models;
    }

    public async Task<Message> AddAsync(Message entity)
    {
        var response = await _context.Client
            .From<Message>()
            .Insert(entity);
        return response.Models.First();
    }

    public async Task<Message> UpdateAsync(Message entity)
    {
        var response = await _context.Client
            .From<Message>()
            .Update(entity);
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _context.Client
            .From<Message>()
            .Where(m => m.Id == id)
            .Delete();
        return true;
    }

    public async Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId)
    {
        var response = await _context.Client
            .From<Message>()
            .Where(m => m.ConversationId == conversationId)
            .Order("sent_at", Postgrest.Constants.Ordering.Ascending)
            .Get();
        return response.Models;
    }

    public async Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId)
    {
        var response = await _context.Client
            .From<Message>()
            .Where(m => m.ConversationId == conversationId && !m.IsRead && m.SenderId != userId)
            .Get();
        return response.Models.Count;
    }
}
