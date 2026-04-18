using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("documents")]
public class Document : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }
    
    [Column("conversation_id")]
    public Guid ConversationId { get; set; }
    
    [Column("uploaded_by_user_id")]
    public Guid UploadedByUserId { get; set; }
    
    [Column("message_id")]
    public Guid? MessageId { get; set; }
    
    [Column("file_name")]
    public string FileName { get; set; } = string.Empty;
    
    [Column("file_type")]
    public string FileType { get; set; } = string.Empty;
    
    [Column("file_size_bytes")]
    public long FileSizeBytes { get; set; }
    
    [Column("file_url")]
    public string FileUrl { get; set; } = string.Empty;
    
    [Column("mime_type")]
    public string? MimeType { get; set; }
    
    [Column("is_processed")]
    public bool IsProcessed { get; set; }
    
    [Column("extracted_text")]
    public string? ExtractedText { get; set; }
    
    [Column("description")]
    public string? Description { get; set; }
    
    [Column("tags")]
    public string[]? Tags { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}
