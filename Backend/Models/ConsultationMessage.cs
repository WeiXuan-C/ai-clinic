namespace Backend.Models;

public class ConsultationMessage
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsFromUser { get; set; }
    public bool IsAiReviewed { get; set; }
    public DateTime Timestamp { get; set; }
    public string? AttachmentUrl { get; set; }
    public string? AttachmentName { get; set; }
    public bool HasQuickActions { get; set; }
}

public class ConsultationHistory
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int UnreadCount { get; set; }
    public bool IsUrgent { get; set; }
}
