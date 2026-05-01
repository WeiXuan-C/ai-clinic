using Postgrest.Attributes;
using Postgrest.Models;

namespace ai_clinic.Interfaces;

/// <summary>
/// Factory Design Pattern - Entity Interface
/// Defines the contract for Conversation entity structure (attributes/properties as constraints)
/// </summary>
public interface IConversation
{
    // Entity Properties - Constraints
    Guid Id { get; set; }
    Guid PatientId { get; set; }
    Guid? AssignedDoctorId { get; set; }
    string? Title { get; set; }
    string Status { get; set; }
    string[]? InitialSymptoms { get; set; }
    string? AiSuggestedSpecialization { get; set; }
    DateTime StartedAt { get; set; }
    DateTime? ClosedAt { get; set; }
    DateTime LastMessageAt { get; set; }
    int TotalMessages { get; set; }
    int AiMessagesCount { get; set; }
    int DoctorMessagesCount { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    string ConsultationStatus { get; set; }
    bool DiagnosisCompleted { get; set; }
    bool PrescriptionGenerated { get; set; }
    string? RequiredSpecialization { get; set; }
    decimal? AiConfidenceScore { get; set; }
}

/// <summary>
/// Conversation entity implementation
/// </summary>
[Table("conversations")]
public class Conversation : BaseModel, IConversation
{
    [PrimaryKey("id", false)]
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
    public DateTime UpdatedAt { get; set; }
    
    [Column("consultation_status")]
    public string ConsultationStatus { get; set; } = "pending";
    
    [Column("diagnosis_completed")]
    public bool DiagnosisCompleted { get; set; }
    
    [Column("prescription_generated")]
    public bool PrescriptionGenerated { get; set; }
    
    [Column("required_specialization")]
    public string? RequiredSpecialization { get; set; }
    
    [Column("ai_confidence_score")]
    public decimal? AiConfidenceScore { get; set; }
}

/// <summary>
/// Factory Design Pattern - Repository Interface
/// Defines the contract for Conversation repository operations
/// </summary>
public interface IConversationRepository
{
    Task<Conversation?> GetByIdAsync(Guid id);
    Task<IEnumerable<Conversation>> GetAllAsync();
    Task<Conversation> AddAsync(Conversation entity);
    Task<Conversation> UpdateAsync(Conversation entity);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<Conversation>> GetByPatientIdAsync(Guid patientId);
    Task<IEnumerable<Conversation>> GetByDoctorIdAsync(Guid doctorId);
    Task<IEnumerable<Conversation>> GetActiveConversationsAsync();
    Task<Conversation?> GetActiveConversationByPatientIdAsync(Guid patientId);
}
