using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Interfaces;

/// <summary>
/// Message sender type enumeration
/// </summary>
public static class MessageSenderType
{
    public const string Patient = "patient";
    public const string Doctor = "doctor";
    public const string AI = "ai";
    public const string System = "system";
}

/// <summary>
/// Factory Design Pattern - Entity Interface
/// Defines the contract for Message entity structure (attributes/properties as constraints)
/// </summary>
public interface IMessage
{
    // Entity Properties - Constraints
    Guid Id { get; set; }
    Guid ConversationId { get; set; }
    Guid? SenderId { get; set; }
    string SenderType { get; set; }
    string Content { get; set; }
    string? AiModelUsed { get; set; }
    decimal? AiConfidenceScore { get; set; }
    Guid[]? DocumentReferences { get; set; }
    bool IsRead { get; set; }
    DateTime? ReadAt { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime SentAt { get; set; } // Alias for CreatedAt
}

/// <summary>
/// Message entity implementation
/// </summary>
public class Message : BaseModel, IMessage
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid? SenderId { get; set; }
    public string SenderType { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string? AiModelUsed { get; set; }
    public decimal? AiConfidenceScore { get; set; }
    public Guid[]? DocumentReferences { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime SentAt { get => CreatedAt; set => CreatedAt = value; }

    public Message WithMarkedAsRead()
    {
        return new Message
        {
            Id = this.Id,
            ConversationId = this.ConversationId,
            SenderId = this.SenderId,
            SenderType = this.SenderType,
            Content = this.Content,
            AiModelUsed = this.AiModelUsed,
            AiConfidenceScore = this.AiConfidenceScore,
            DocumentReferences = this.DocumentReferences,
            IsRead = true,
            ReadAt = DateTime.UtcNow,
            CreatedAt = this.CreatedAt
        };
    }
}

/// <summary>
/// Factory Design Pattern - Repository Interface
/// Defines the contract for Message repository operations
/// </summary>
public interface IMessageRepository
{
    Task<Message?> GetByIdAsync(Guid id);
    Task<IEnumerable<Message>> GetAllAsync();
    Task<Message> AddAsync(Message entity);
    Task<Message> UpdateAsync(Message entity);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<Message>> GetByConversationIdAsync(Guid conversationId);
    Task<Message?> GetLatestMessageAsync(Guid conversationId);
    Task<int> GetUnreadCountAsync(Guid conversationId, Guid userId);
    Task MarkAsReadAsync(Guid messageId);
}
