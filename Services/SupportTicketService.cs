using AiClinic.Interfaces;
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
    public async Task<IEnumerable<ISupportTicket>> GetAllTicketsAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Gets a support ticket by ID
    /// </summary>
    public async Task<ISupportTicket?> GetTicketByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets all support tickets for a user
    /// </summary>
    public async Task<IEnumerable<ISupportTicket>> GetUserTicketsAsync(Guid userId)
    {
        return await _state.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Gets tickets by status
    /// </summary>
    public async Task<IEnumerable<ISupportTicket>> GetTicketsByStatusAsync(string status)
    {
        return await _state.GetByStatusAsync(status);
    }

    /// <summary>
    /// Creates a new support ticket
    /// </summary>
    public async Task<ISupportTicket?> CreateTicketAsync(ISupportTicket ticket)
    {
        var concreteTicket = ticket as SupportTicket ?? new SupportTicket
        {
            Id = ticket.Id,
            UserId = ticket.UserId,
            Subject = ticket.Subject,
            Description = ticket.Description,
            Category = ticket.Category,
            Priority = ticket.Priority,
            Status = ticket.Status,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt
        };
        return await _state.CreateAsync(concreteTicket);
    }

    /// <summary>
    /// Updates a support ticket
    /// </summary>
    public async Task<ISupportTicket?> UpdateTicketAsync(ISupportTicket ticket)
    {
        var concreteTicket = ticket as SupportTicket ?? new SupportTicket
        {
            Id = ticket.Id,
            UserId = ticket.UserId,
            Subject = ticket.Subject,
            Description = ticket.Description,
            Category = ticket.Category,
            Priority = ticket.Priority,
            Status = ticket.Status,
            CreatedAt = ticket.CreatedAt,
            UpdatedAt = ticket.UpdatedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt
        };
        return await _state.UpdateAsync(concreteTicket);
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
    public IReadOnlyList<ISupportTicket> GetCachedTickets()
    {
        return _state.Tickets.Cast<ISupportTicket>().ToList();
    }

    /// <summary>
    /// Gets the currently selected ticket
    /// </summary>
    public ISupportTicket? GetSelectedTicket()
    {
        return _state.SelectedTicket;
    }

    /// <summary>
    /// Sets the selected ticket
    /// </summary>
    public void SetSelectedTicket(ISupportTicket? ticket)
    {
        _state.SelectedTicket = ticket as SupportTicket;
    }

    // Controller-facing methods (adapters for backward compatibility)
    
    public async Task<ISupportTicket?> CreateTicketAsync(object request)
    {
        // Extract properties from request object dynamically
        var requestType = request.GetType();
        var userId = Guid.Parse(requestType.GetProperty("UserId")?.GetValue(request)?.ToString() ?? Guid.NewGuid().ToString());
        var subject = requestType.GetProperty("Subject")?.GetValue(request)?.ToString() ?? string.Empty;
        var description = requestType.GetProperty("Description")?.GetValue(request)?.ToString() ?? string.Empty;
        var category = requestType.GetProperty("Category")?.GetValue(request)?.ToString();
        var priority = requestType.GetProperty("Priority")?.GetValue(request)?.ToString() ?? "medium";
        
        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Subject = subject,
            Description = description,
            Category = category,
            Priority = priority,
            Status = "open",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        
        return await CreateTicketAsync((ISupportTicket)ticket);
    }
    
    public async Task<ISupportTicket?> GetTicketByIdAsync(string ticketId)
    {
        if (Guid.TryParse(ticketId, out var guid))
        {
            return await GetTicketByIdAsync(guid);
        }
        return null;
    }
    
    public async Task<IEnumerable<ISupportTicket>> GetTicketsByUserIdAsync(string userId)
    {
        if (Guid.TryParse(userId, out var guid))
        {
            return await GetUserTicketsAsync(guid);
        }
        return Enumerable.Empty<ISupportTicket>();
    }
    
    public async Task<ISupportTicket?> UpdateTicketStatusAsync(string ticketId, string status)
    {
        if (!Guid.TryParse(ticketId, out var guid))
        {
            return null;
        }
        
        var existing = await GetTicketByIdAsync(guid);
        if (existing == null)
        {
            return null;
        }
        
        var updated = new SupportTicket
        {
            Id = guid,
            UserId = existing.UserId,
            Subject = existing.Subject,
            Description = existing.Description,
            Category = existing.Category,
            Priority = existing.Priority,
            Status = status,
            CreatedAt = existing.CreatedAt,
            UpdatedAt = DateTime.UtcNow,
            ResolvedAt = status == "resolved" ? DateTime.UtcNow : existing.ResolvedAt,
            ClosedAt = status == "closed" ? DateTime.UtcNow : existing.ClosedAt
        };
        
        return await UpdateTicketAsync(updated);
    }
    
    public async Task<bool> DeleteTicketAsync(string ticketId)
    {
        if (Guid.TryParse(ticketId, out var guid))
        {
            return await DeleteTicketAsync(guid);
        }
        return false;
    }
}
