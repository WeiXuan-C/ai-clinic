using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("messages")]
public class Message : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }
    
    [Column("conversation_id")]
    public Guid ConversationId { get; set; }
    
    [Column("sender_id")]
    public Guid? SenderId { get; set; }
    
    [Column("sender_type")]
    public MessageSenderType SenderType { get; set; } = MessageSenderType.Patient;
    
    [Column("content")]
    public string Content { get; set; } = string.Empty;
    
    [Column("ai_model_used")]
    public string? AiModelUsed { get; set; }
    
    [Column("ai_confidence_score")]
    public decimal? AiConfidenceScore { get; set; }
    
    [Column("document_references")]
    public Guid[]? DocumentReferences { get; set; }
    
    [Column("is_read")]
    public bool IsRead { get; set; }
    
    [Column("read_at")]
    public DateTime? ReadAt { get; set; }
    
    [Column("sent_at")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

public enum MessageSenderType
{
    Patient,
    Doctor,
    AI
}
