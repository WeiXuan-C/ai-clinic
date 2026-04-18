using AiClinic.Core.Entities;

namespace AiClinic.Core.Interfaces;

public interface IDoctorRatingRepository : IRepository<DoctorRating>
{
    Task<IEnumerable<DoctorRating>> GetByDoctorIdAsync(Guid doctorId);
    Task<IEnumerable<DoctorRating>> GetByPatientIdAsync(Guid patientId);
    Task<DoctorRating?> GetByConversationIdAsync(Guid conversationId);
    Task<double> GetAverageRatingAsync(Guid doctorId);
}
