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
public class Document : BaseModel, IDocument
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileUrl { get; set; } = string.Empty;
    public string? MimeType { get; set; }
    public bool IsProcessed { get; set; }
    public string? ExtractedText { get; set; }
    public string? Description { get; set; }
    public string[]? Tags { get; set; }
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
