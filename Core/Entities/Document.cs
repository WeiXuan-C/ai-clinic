namespace AiClinic.Core.Entities;

public class Document
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string? VectorEmbedding { get; set; }
    public DateTime UploadedAt { get; set; }
    
    public User? Patient { get; set; }
}
