using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("doctor_ratings")]
public class DoctorRating : BaseModel
{
    [PrimaryKey("id")]
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
