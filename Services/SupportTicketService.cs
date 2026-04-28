using AiClinic.UI.State;

namespace AiClinic.Services;

/// <summary>
/// Support Ticket Service - Business Logic Layer
/// Handles support ticket operations through state management
/// </summary>
public class SupportTicketService
{
    private readonly SupportTicketState _state;

    public SupportTicketService(SupportTicketState state)
    {
        _state = state;
    }

    /// <summary>
    /// Gets all support tickets
    /// </summary>
    public async Task<IEnumerable<SupportTicket>> GetAllTicketsAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Gets a support ticket by ID
    /// </summary>
    public async Task<SupportTicket?> GetTicketByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets all support tickets for a user
    /// </summary>
    public async Task<IEnumerable<SupportTicket>> GetUserTicketsAsync(Guid userId)
    {
        return await _state.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Gets tickets by status
    /// </summary>
    public async Task<IEnumerable<SupportTicket>> GetTicketsByStatusAsync(string status)
    {
        return await _state.GetByStatusAsync(status);
    }

    /// <summary>
    /// Creates a new support ticket
    /// </summary>
    public async Task<SupportTicket?> CreateTicketAsync(SupportTicket ticket)
    {
        return await _state.CreateAsync(ticket);
    }

    /// <summary>
    /// Updates a support ticket
    /// </summary>
    public async Task<SupportTicket?> UpdateTicketAsync(SupportTicket ticket)
    {
        return await _state.UpdateAsync(ticket);
    }

    /// <summary>
    /// Deletes a support ticket
    /// </summary>
    public async Task<bool> DeleteTicketAsync(Guid id)
    {
        return await _state.DeleteAsync(id);
    }

    /// <summary>
    /// Gets cached tickets from state
    /// </summary>
    public IReadOnlyList<SupportTicket> GetCachedTickets()
    {
        return _state.Tickets;
    }

    /// <summary>
    /// Gets the currently selected ticket
    /// </summary>
    public SupportTicket? GetSelectedTicket()
    {
        return _state.SelectedTicket;
    }

    /// <summary>
    /// Sets the selected ticket
    /// </summary>
    public void SetSelectedTicket(SupportTicket? ticket)
    {
        _state.SelectedTicket = ticket;
    }
}
