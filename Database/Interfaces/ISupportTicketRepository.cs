using AiClinic.Core.Entities;

namespace AiClinic.Core.Interfaces;

public interface ISupportTicketRepository : IRepository<SupportTicket>
{
    Task<IEnumerable<SupportTicket>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<SupportTicket>> GetByStatusAsync(string status);
}
