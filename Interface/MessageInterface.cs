namespace AiClinic.Interfaces;

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
