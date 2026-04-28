namespace AiClinic.Controller;

public class SupportTicketController
{
    private readonly Services.SupportTicketService _supportTicketService;

    public SupportTicketController(Services.SupportTicketService supportTicketService)
    {
        _supportTicketService = supportTicketService;
    }

    public async Task<object> CreateTicketAsync(CreateTicketRequest request)
    {
        return await _supportTicketService.CreateTicketAsync(request);
    }

    public async Task<object?> GetTicketByIdAsync(string ticketId)
    {
        return await _supportTicketService.GetTicketByIdAsync(ticketId);
    }

    public async Task<object> GetTicketsByUserIdAsync(string userId)
    {
        return await _supportTicketService.GetTicketsByUserIdAsync(userId);
    }

    public async Task<object> UpdateTicketStatusAsync(string ticketId, string status)
    {
        return await _supportTicketService.UpdateTicketStatusAsync(ticketId, status);
    }

    public async Task DeleteTicketAsync(string ticketId)
    {
        await _supportTicketService.DeleteTicketAsync(ticketId);
    }
}

public record CreateTicketRequest(string UserId, string Subject, string Description, string Priority);
public record UpdateStatusRequest(string Status);
