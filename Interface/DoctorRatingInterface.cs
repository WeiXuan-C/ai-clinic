namespace AiClinic.Interfaces;

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
