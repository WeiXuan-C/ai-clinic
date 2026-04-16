using AiClinic.Core.Entities;

namespace AiClinic.Core.Interfaces;

public interface IOrganizationRepository : IRepository<Organization>
{
    Task<IEnumerable<Organization>> GetVerifiedOrganizationsAsync();
    Task<Organization?> GetByNameAsync(string name);
}
