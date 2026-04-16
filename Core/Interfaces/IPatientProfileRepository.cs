using AiClinic.Core.Entities;

namespace AiClinic.Core.Interfaces;

public interface IPatientProfileRepository : IRepository<PatientProfile>
{
    Task<PatientProfile?> GetByUserIdAsync(Guid userId);
}
