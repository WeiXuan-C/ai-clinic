using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("conversations")]
public class Conversation : BaseModel
{
    [PrimaryKey("id")]
    public Guid Id { get; set; }
    
    [Column("patient_id")]
    public Guid PatientId { get; set; }
    
    [Column("assigned_doctor_id")]
    public Guid? AssignedDoctorId { get; set; }
    
    [Column("title")]
    public string? Title { get; set; }
    
    [Column("status")]
    public string Status { get; set; } = "active";
    
    [Column("initial_symptoms")]
    public string[]? InitialSymptoms { get; set; }
    
    [Column("ai_suggested_specialization")]
    public string? AiSuggestedSpecialization { get; set; }
    
    [Column("started_at")]
    public DateTime StartedAt { get; set; }
    
    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }
    
    [Column("last_message_at")]
    public DateTime LastMessageAt { get; set; }
    
    [Column("total_messages")]
    public int TotalMessages { get; set; }
    
    [Column("ai_messages_count")]
    public int AiMessagesCount { get; set; }
    
    [Column("doctor_messages_count")]
    public int DoctorMessagesCount { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
    
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }
}

public enum ConversationStatus
{
    Active,
    Closed,
    Archived,
    Deactive
}
