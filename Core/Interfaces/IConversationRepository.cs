using AiClinic.Core.Entities;

namespace AiClinic.Core.Interfaces;

public interface IConversationRepository : IRepository<Conversation>
{
    Task<IEnumerable<Conversation>> GetByPatientIdAsync(Guid patientId);
    Task<IEnumerable<Conversation>> GetByDoctorIdAsync(Guid doctorId);
    Task<Conversation?> GetActiveConversationAsync(Guid patientId);
}
