using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("documents")]
public class Document : BaseModel
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    // Primary Key and Foreign Keys
    [PrimaryKey("id")]
    public Guid Id { get; private set; }

    [Column("conversation_id")]
    public Guid ConversationId { get; private set; }

    [Column("uploaded_by_user_id")]
    public Guid UploadedByUserId { get; private set; }

    [Column("message_id")]
    public Guid? MessageId { get; private set; }

    // File Information
    [Column("file_name")]
    public string FileName { get; private set; } = string.Empty;

    [Column("file_type")]
    public string FileType { get; private set; } = string.Empty;

    [Column("file_size_bytes")]
    public long FileSizeBytes { get; private set; }

    [Column("file_url")]
    public string FileUrl { get; private set; } = string.Empty;

    [Column("mime_type")]
    public string? MimeType { get; private set; }

    // Processing Status
    [Column("is_processed")]
    public bool IsProcessed { get; private set; }

    [Column("extracted_text")]
    public string? ExtractedText { get; private set; }

    // Metadata
    [Column("description")]
    public string? Description { get; private set; }

    [Column("tags")]
    public string[]? Tags { get; private set; }

    // Timestamps
    [Column("created_at")]
    public DateTime CreatedAt { get; private set; }

    // Public Methods (Business Logic)
    public void UpdateDescription(string description)
    {
        Description = description?.Trim();
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Tag cannot be empty");

        var tagsList = Tags?.ToList() ?? new List<string>();
        if (!tagsList.Contains(tag, StringComparer.OrdinalIgnoreCase))
        {
            tagsList.Add(tag.Trim());
            Tags = tagsList.ToArray();
        }
    }

    public void RemoveTag(string tag)
    {
        if (Tags != null)
        {
            var tagsList = Tags.ToList();
            tagsList.RemoveAll(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase));
            Tags = tagsList.ToArray();
        }
    }

    public void MarkAsProcessed(string? extractedText)
    {
        IsProcessed = true;
        ExtractedText = extractedText;
    }

    public bool IsImage()
    {
        return MimeType?.StartsWith("image/") ?? false;
    }

    public bool IsPdf()
    {
        return MimeType == "application/pdf";
    }

    public double GetFileSizeMB()
    {
        return FileSizeBytes / (1024.0 * 1024.0);
    }

    // Public method for DAO initialization
    public void Initialize(Guid id, Guid conversationId, Guid uploadedByUserId, string fileName, string fileType, long fileSizeBytes, string fileUrl, DateTime createdAt)
    {
        if (fileSizeBytes > MaxFileSizeBytes)
            throw new ArgumentException($"File size exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)}MB");

        Id = id;
        ConversationId = conversationId;
        UploadedByUserId = uploadedByUserId;
        FileName = fileName;
        FileType = fileType;
        FileSizeBytes = fileSizeBytes;
        FileUrl = fileUrl;
        CreatedAt = createdAt;
    }

    public void SetMimeType(string mimeType)
    {
        MimeType = mimeType;
    }
}
