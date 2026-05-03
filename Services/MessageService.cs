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
    /// Create a new patient message
    /// </summary>
    public async Task<Message> CreatePatientMessageAsync(Guid conversationId, Guid patientId, string content)
    {
        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = patientId,
            SenderType = MessageSenderType.Patient,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        return await CreateAsync(message);
    }

    /// <summary>
    /// Create a new AI message
    /// </summary>
    public async Task<Message> CreateAiMessageAsync(Guid conversationId, string content, string? aiModel = null, decimal? confidenceScore = null)
    {
        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = null,
            SenderType = MessageSenderType.AI,
            Content = content,
            AiModelUsed = aiModel,
            AiConfidenceScore = confidenceScore,
            CreatedAt = DateTime.UtcNow
        };

        return await CreateAsync(message);
    }

    /// <summary>
    /// Create a new doctor message
    /// </summary>
    public async Task<Message> CreateDoctorMessageAsync(Guid conversationId, Guid doctorId, string content)
    {
        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = doctorId,
            SenderType = MessageSenderType.Doctor,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        return await CreateAsync(message);
    }

    /// <summary>
    /// Create a new message
    /// </summary>
    public async Task<Message> CreateAsync(Message message)
    {
        message.CreatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.Messages.Add(message);

        // Update conversation's last activity time and message count
        var conversation = await db.Conversations.FindAsync(message.ConversationId);
        if (conversation != null)
        {
            conversation.LastMessageAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;
            conversation.TotalMessages++;
            
            // Update message type counts
            if (message.SenderType == MessageSenderType.AI)
            {
                conversation.AiMessagesCount++;
            }
            else if (message.SenderType == MessageSenderType.Doctor)
            {
                conversation.DoctorMessagesCount++;
            }
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
            message.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Mark all messages in a conversation as read for a specific user
    /// </summary>
    public async Task MarkConversationAsReadAsync(Guid conversationId, MessageSenderType excludeSenderType)
    {
        using var db = DbClient.Instance.GetDb();
        var unreadMessages = await db.Messages
            .Where(m => m.ConversationId == conversationId && !m.IsRead && m.SenderType != excludeSenderType)
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Get unread message count for a conversation
    /// </summary>
    public async Task<int> GetUnreadCountAsync(Guid conversationId, MessageSenderType excludeSenderType)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Messages
            .Where(m => m.ConversationId == conversationId && !m.IsRead && m.SenderType != excludeSenderType)
            .CountAsync();
    }
}
