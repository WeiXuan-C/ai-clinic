namespace AiClinic.Core.Entities;

public class Conversation
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? DoctorId { get; set; }
    public ConversationStatus Status { get; set; }
    public ConversationType Type { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    
    public User? Patient { get; set; }
    public Doctor? Doctor { get; set; }
    public List<Message> Messages { get; set; } = new();
}

public enum ConversationStatus
{
    Active,
    Closed,
    Escalated
}

public enum ConversationType
{
    AI,
    Doctor,
    Hybrid
}
