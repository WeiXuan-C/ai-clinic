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
        Console.WriteLine("=== [MESSAGE SERVICE DEBUG] CreatePatientMessageAsync Started ===");
        Console.WriteLine($"[MSG SVC] Conversation ID: {conversationId}");
        Console.WriteLine($"[MSG SVC] Patient ID: {patientId}");
        Console.WriteLine($"[MSG SVC] Content Length: {content.Length} chars");

        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = patientId,
            SenderType = MessageSenderType.Patient,
            Content = content,
            CreatedAt = DateTime.UtcNow
        };

        Console.WriteLine("[MSG SVC] Message object created, calling CreateAsync...");
        var result = await CreateAsync(message);
        Console.WriteLine($"[MSG SVC] Patient message saved - ID: {result.Id}");
        Console.WriteLine("=== [MESSAGE SERVICE DEBUG] CreatePatientMessageAsync Completed ===\n");

        return result;
    }

    /// <summary>
    /// Create a new AI message
    /// </summary>
    public async Task<Message> CreateAiMessageAsync(Guid conversationId, string content, string? aiModel = null, decimal? confidenceScore = null)
    {
        Console.WriteLine("=== [MESSAGE SERVICE DEBUG] CreateAiMessageAsync Started ===");
        Console.WriteLine($"[MSG SVC] Conversation ID: {conversationId}");
        Console.WriteLine($"[MSG SVC] Content Length: {content.Length} chars");
        Console.WriteLine($"[MSG SVC] AI Model: {aiModel ?? "null"}");
        Console.WriteLine($"[MSG SVC] Confidence Score: {confidenceScore?.ToString() ?? "null"}");

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

        Console.WriteLine("[MSG SVC] Message object created, calling CreateAsync...");
        var result = await CreateAsync(message);
        Console.WriteLine($"[MSG SVC] AI message saved - ID: {result.Id}");
        Console.WriteLine("=== [MESSAGE SERVICE DEBUG] CreateAiMessageAsync Completed ===\n");

        return result;
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
        Console.WriteLine($"[MSG SVC] CreateAsync - Saving {message.SenderType} message to database...");
        message.CreatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.Messages.Add(message);

        // Update conversation's last activity time and message count
        Console.WriteLine($"[MSG SVC] Updating conversation {message.ConversationId} metadata...");
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
                Console.WriteLine($"[MSG SVC] AI message count updated: {conversation.AiMessagesCount}");
            }
            else if (message.SenderType == MessageSenderType.Doctor)
            {
                conversation.DoctorMessagesCount++;
                Console.WriteLine($"[MSG SVC] Doctor message count updated: {conversation.DoctorMessagesCount}");
            }
            Console.WriteLine($"[MSG SVC] Total messages in conversation: {conversation.TotalMessages}");
        }
        else
        {
            Console.WriteLine($"[MSG SVC WARNING] Conversation {message.ConversationId} not found!");
        }

        Console.WriteLine("[MSG SVC] Saving changes to database...");
        await db.SaveChangesAsync();
        Console.WriteLine($"[MSG SVC] Message saved successfully - ID: {message.Id}");
        
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
