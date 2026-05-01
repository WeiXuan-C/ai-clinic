using Postgrest.Attributes;
using Postgrest.Models;

namespace ai_clinic.Interfaces;

/// <summary>
/// Factory Design Pattern - Entity Interface
/// Defines the contract for Document entity structure (attributes/properties as constraints)
/// </summary>
public interface IDocument
{
    // Entity Properties - Constraints
    Guid Id { get; set; }
    Guid ConversationId { get; set; }
    Guid UploadedByUserId { get; set; }
    string FileName { get; set; }
    string FileType { get; set; }
    long FileSizeBytes { get; set; }
    string FileUrl { get; set; }
    string? MimeType { get; set; }
    bool IsProcessed { get; set; }
    string? ExtractedText { get; set; }
    string? Description { get; set; }
    string[]? Tags { get; set; }
    DateTime CreatedAt { get; set; }
}

/// <summary>
/// Document entity implementation
/// </summary>
[Table("documents")]
public class Document : BaseModel, IDocument
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("conversation_id")]
    public Guid ConversationId { get; set; }
    
    [Column("uploaded_by_user_id")]
    public Guid UploadedByUserId { get; set; }
    
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

/// <summary>
/// Factory Design Pattern - Repository Interface
/// Defines the contract for Document repository operations
/// </summary>
public interface IDocumentRepository
{
    Task<Document?> GetByIdAsync(Guid id);
    Task<IEnumerable<Document>> GetAllAsync();
    Task<Document> AddAsync(Document entity);
    Task<Document> UpdateAsync(Document entity);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<Document>> GetByConversationIdAsync(Guid conversationId);
    Task<IEnumerable<Document>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Document>> GetProcessedDocumentsAsync(Guid conversationId);
}
