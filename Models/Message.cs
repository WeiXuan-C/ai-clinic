using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ai_clinic.Models;

[Table("messages")]
public class Message
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [Column("conversation_id")]
    public Guid ConversationId { get; set; }

    [Column("sender_id")]
    public Guid? SenderId { get; set; }

    [Required]
    [Column("sender_type")]
    public MessageSenderType SenderType { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [MaxLength(100)]
    [Column("ai_model_used")]
    public string? AiModelUsed { get; set; }

    [Column("ai_confidence_score")]
    public decimal? AiConfidenceScore { get; set; }

    [Column("document_references")]
    public string? DocumentReferences { get; set; } // JSON array string of UUIDs

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ConversationId")]
    public Conversation Conversation { get; set; } = null!;

    [ForeignKey("SenderId")]
    public User? Sender { get; set; }
}
