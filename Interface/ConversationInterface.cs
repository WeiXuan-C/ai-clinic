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
public class Conversation : BaseModel, IConversation
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid? AssignedDoctorId { get; set; }
    public string? Title { get; set; }
    public string Status { get; set; } = "active";
    public string[]? InitialSymptoms { get; set; }
    public string? AiSuggestedSpecialization { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int TotalMessages { get; set; }
    public int AiMessagesCount { get; set; }
    public int DoctorMessagesCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string ConsultationStatus { get; set; } = "pending";
    public bool DiagnosisCompleted { get; set; }
    public bool PrescriptionGenerated { get; set; }
    public string? RequiredSpecialization { get; set; }
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
