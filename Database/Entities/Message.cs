using Postgrest.Attributes;
using Postgrest.Models;
using System.Text.Json.Serialization;

namespace AiClinic.Core.Entities;

[Table("messages")]
public class Message : BaseModel
{
    // Primary Key and Foreign Keys
    [PrimaryKey("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [Column("conversation_id")]
    [JsonPropertyName("conversation_id")]
    public Guid ConversationId { get; set; }

    [Column("sender_id")]
    [JsonPropertyName("sender_id")]
    public Guid? SenderId { get; set; }

    [Column("sender_type")]
    [JsonPropertyName("sender_type")]
    public MessageSenderType SenderType { get; set; } = MessageSenderType.Patient;

    // Message Content
    [Column("content")]
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    // AI Metadata
    [Column("ai_model_used")]
    [JsonPropertyName("ai_model_used")]
    public string? AiModelUsed { get; set; }

    [Column("ai_confidence_score")]
    [JsonPropertyName("ai_confidence_score")]
    public decimal? AiConfidenceScore { get; set; }

    [Column("document_references")]
    [JsonPropertyName("document_references")]
    public Guid[]? DocumentReferences { get; set; }

    // Read Status
    [Column("is_read")]
    [JsonPropertyName("is_read")]
    public bool IsRead { get; set; }

    [Column("read_at")]
    [JsonPropertyName("read_at")]
    public DateTime? ReadAt { get; set; }

    // Timestamps
    [Column("sent_at")]
    [JsonPropertyName("sent_at")]
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    // Parameterless constructor for ORM
    public Message() { }

    // JSON constructor for deserialization
    [JsonConstructor]
    public Message(
        Guid id, Guid conversation_id, Guid? sender_id, MessageSenderType sender_type,
        string content, string? ai_model_used, decimal? ai_confidence_score,
        Guid[]? document_references, bool is_read, DateTime? read_at,
        DateTime sent_at, DateTime created_at)
    {
        Id = id;
        ConversationId = conversation_id;
        SenderId = sender_id;
        SenderType = sender_type;
        Content = content;
        AiModelUsed = ai_model_used;
        AiConfidenceScore = ai_confidence_score;
        DocumentReferences = document_references;
        IsRead = is_read;
        ReadAt = read_at;
        SentAt = sent_at;
        CreatedAt = created_at;
    }

    // Factory method for creating patient message
    public static Message CreatePatientMessage(Guid conversationId, Guid senderId, string content)
    {
        var now = DateTime.UtcNow;
        return new Message(
            id: Guid.NewGuid(),
            conversation_id: conversationId,
            sender_id: senderId,
            sender_type: MessageSenderType.Patient,
            content: content,
            ai_model_used: null,
            ai_confidence_score: null,
            document_references: null,
            is_read: false,
            read_at: null,
            sent_at: now,
            created_at: now
        );
    }

    // Factory method for creating AI message
    public static Message CreateAIMessage(Guid conversationId, string content, string aiModel, decimal confidenceScore)
    {
        var now = DateTime.UtcNow;
        return new Message(
            id: Guid.NewGuid(),
            conversation_id: conversationId,
            sender_id: null,
            sender_type: MessageSenderType.AI,
            content: content,
            ai_model_used: aiModel,
            ai_confidence_score: confidenceScore,
            document_references: null,
            is_read: false,
            read_at: null,
            sent_at: now,
            created_at: now
        );
    }

    // Factory method for marking as read
    public Message WithMarkedAsRead()
    {
        return new Message(
            Id, ConversationId, SenderId, SenderType, Content,
            AiModelUsed, AiConfidenceScore, DocumentReferences,
            true, DateTime.UtcNow, SentAt, CreatedAt
        );
    }

    // Public Methods (Business Logic)
    public void MarkAsRead() { }
    public void AttachDocuments(Guid[] documentIds) { }
    public bool IsFromAI() => SenderType == MessageSenderType.AI;
    public bool IsFromDoctor() => SenderType == MessageSenderType.Doctor;
    public bool IsFromPatient() => SenderType == MessageSenderType.Patient;
}

public enum MessageSenderType
{
    Patient,
    Doctor,
    AI
}
