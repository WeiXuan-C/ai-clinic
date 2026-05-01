using Postgrest.Attributes;
using Postgrest.Models;

namespace ai_clinic.Interfaces;

/// <summary>
/// Factory Design Pattern - Entity Interface
/// Defines the contract for Doctor Rating entity structure (attributes/properties as constraints)
/// </summary>
public interface IDoctorRating
{
    // Entity Properties - Constraints
    Guid Id { get; set; }
    Guid DoctorId { get; set; }
    Guid PatientId { get; set; }
    Guid ConversationId { get; set; }
    int Rating { get; set; }
    string? ReviewText { get; set; }
    int? ProfessionalismRating { get; set; }
    int? CommunicationRating { get; set; }
    int? KnowledgeRating { get; set; }
    int? ResponseTimeRating { get; set; }
    DateTime CreatedAt { get; set; }
}

/// <summary>
/// DoctorRating entity implementation
/// </summary>
[Table("doctor_ratings")]
public class DoctorRating : BaseModel, IDoctorRating
{
    [PrimaryKey("id", false)]
    public Guid Id { get; set; }
    
    [Column("doctor_id")]
    public Guid DoctorId { get; set; }
    
    [Column("patient_id")]
    public Guid PatientId { get; set; }
    
    [Column("conversation_id")]
    public Guid ConversationId { get; set; }
    
    [Column("rating")]
    public int Rating { get; set; }
    
    [Column("review_text")]
    public string? ReviewText { get; set; }
    
    [Column("professionalism_rating")]
    public int? ProfessionalismRating { get; set; }
    
    [Column("communication_rating")]
    public int? CommunicationRating { get; set; }
    
    [Column("knowledge_rating")]
    public int? KnowledgeRating { get; set; }
    
    [Column("response_time_rating")]
    public int? ResponseTimeRating { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Factory Design Pattern - Repository Interface
/// Defines the contract for Doctor Rating repository operations
/// </summary>
public interface IDoctorRatingRepository
{
    Task<DoctorRating?> GetByIdAsync(Guid id);
    Task<IEnumerable<DoctorRating>> GetAllAsync();
    Task<DoctorRating> AddAsync(DoctorRating entity);
    Task<DoctorRating> UpdateAsync(DoctorRating entity);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<DoctorRating>> GetByDoctorIdAsync(Guid doctorId);
    Task<IEnumerable<DoctorRating>> GetByPatientIdAsync(Guid patientId);
    Task<double> GetAverageRatingAsync(Guid doctorId);
    Task<int> GetTotalRatingsCountAsync(Guid doctorId);
}
