using AiClinic.Core.Entities;

namespace AiClinic.Core.Interfaces;

public interface IDoctorRepository : IRepository<Doctor>
{
    Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync();
    Task<Doctor?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization);
}
