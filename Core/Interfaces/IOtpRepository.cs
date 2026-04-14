using AiClinic.Core.Entities;

namespace AiClinic.Core.Interfaces;

public interface IOtpRepository : IRepository<OtpToken>
{
    Task<OtpToken?> GetValidOtpAsync(string email, string code);
    Task MarkAsUsedAsync(Guid id);
}
