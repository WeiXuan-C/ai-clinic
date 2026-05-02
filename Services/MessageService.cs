using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

/// <summary>
/// Service for managing Messages in conversations
/// </summary>
public class MessageService
{
    /// <summary>
    /// Get all messages for a conversation
    /// </summary>
    public async Task<List<Message>> GetByConversationIdAsync(Guid conversationId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Messages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Create a new message
    /// </summary>
    public async Task<Message> CreateAsync(Message message)
    {
        message.CreatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.Messages.Add(message);

        // Update conversation's last activity time
        var conversation = await db.Conversations.FindAsync(message.ConversationId);
        if (conversation != null)
        {
            conversation.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return message;
    }

    /// <summary>
    /// Get latest messages for a conversation
    /// </summary>
    public async Task<List<Message>> GetLatestMessagesAsync(Guid conversationId, int count = 10)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Messages
            .Include(m => m.Sender)
            .Where(m => m.ConversationId == conversationId)
            .OrderByDescending(m => m.CreatedAt)
            .Take(count)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Mark message as read
    /// </summary>
    public async Task MarkAsReadAsync(Guid messageId)
    {
        using var db = DbClient.Instance.GetDb();
        var message = await db.Messages.FindAsync(messageId);
        if (message != null)
        {
            message.IsRead = true;
            await db.SaveChangesAsync();
        }
    }
}
