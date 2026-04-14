namespace AiClinic.Core.Entities;

public class Message
{
    public Guid Id { get; set; }
    public Guid ConversationId { get; set; }
    public Guid? SenderId { get; set; }
    public MessageSenderType SenderType { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
    public bool IsRead { get; set; }
    
    public Conversation? Conversation { get; set; }
}

public enum MessageSenderType
{
    Patient,
    Doctor,
    AI
}
