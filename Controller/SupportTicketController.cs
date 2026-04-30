using ai_clinic.Interfaces;

namespace ai_clinic.Controller;

public class SupportTicketController(Services.SupportTicketService supportTicketService)
{
    public Task<ISupportTicket?> CreateTicketAsync(CreateTicketRequest request)
    {
        return supportTicketService.CreateTicketAsync(request);
    }

    public Task<ISupportTicket?> GetTicketByIdAsync(string ticketId)
    {
        return supportTicketService.GetTicketByIdAsync(ticketId);
    }

    public Task<IEnumerable<ISupportTicket>> GetTicketsByUserIdAsync(string userId)
    {
        return supportTicketService.GetTicketsByUserIdAsync(userId);
    }

    public Task<ISupportTicket?> UpdateTicketStatusAsync(string ticketId, string status)
    {
        return supportTicketService.UpdateTicketStatusAsync(ticketId, status);
    }

    public Task<bool> DeleteTicketAsync(string ticketId)
    {
        return supportTicketService.DeleteTicketAsync(ticketId);
    }
}

public record CreateTicketRequest(string UserId, string Subject, string Description, string Priority);
public record UpdateStatusRequest(string Status);
