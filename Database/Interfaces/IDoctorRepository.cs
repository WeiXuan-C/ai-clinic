using AiClinic.Core.Entities;

namespace AiClinic.Core.Interfaces;

public interface IDoctorRepository : IRepository<Doctor>
{
    Task<Doctor?> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync();
    Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization);
    Task<IEnumerable<Doctor>> GetByOrganizationIdAsync(Guid organizationId);
    Task UpdateAvailabilityStatusAsync(Guid doctorId, string status);
}
