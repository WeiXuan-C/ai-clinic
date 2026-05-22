using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

public class SupportTicketService
{
    public async Task<SupportTicket?> GetByIdAsync(Guid ticketId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.SupportTickets
            .Include(st => st.User)
            .Include(st => st.Attachments)
            .Include(st => st.Responses)
                .ThenInclude(r => r.Responder)
            .FirstOrDefaultAsync(st => st.Id == ticketId);
    }

    public async Task<List<SupportTicket>> GetByUserIdAsync(Guid userId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.SupportTickets
            .Include(st => st.Responses)
            .Where(st => st.UserId == userId)
            .OrderByDescending(st => st.CreatedAt)
            .ToListAsync();
    }

    public async Task<SupportTicket> CreateAsync(SupportTicket ticket)
    {
        ticket.CreatedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.SupportTickets.Add(ticket);
        await db.SaveChangesAsync();
        return ticket;
    }

    public async Task<SupportTicketResponse> AddResponseAsync(SupportTicketResponse response)
    {
        response.CreatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.SupportTicketResponses.Add(response);

        var ticket = await db.SupportTickets.FindAsync(response.TicketId);
        if (ticket != null)
        {
            ticket.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
        return response;
    }

    public async Task UpdateStatusAsync(Guid ticketId, string status)
    {
        using var db = DbClient.Instance.GetDb();
        var ticket = await db.SupportTickets.FindAsync(ticketId);
        if (ticket != null)
        {
            ticket.Status = status;
            ticket.UpdatedAt = DateTime.UtcNow;
            if (status == "resolved")
            {
                ticket.ResolvedAt = DateTime.UtcNow;
            }
            else if (status == "closed")
            {
                ticket.ClosedAt = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();
        }
    }

    public async Task<List<SupportTicket>> GetOpenTicketsAsync()
    {
        using var db = DbClient.Instance.GetDb();
        return await db.SupportTickets
            .Include(st => st.User)
            .Where(st => st.Status == "open" || st.Status == "in_progress")
            .OrderByDescending(st => st.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SupportTicket>> GetAllTicketsAsync()
    {
        using var db = DbClient.Instance.GetDb();
        return await db.SupportTickets
            .Include(st => st.User)
            .Include(st => st.Attachments)
            .Include(st => st.Responses)
                .ThenInclude(r => r.Responder)
            .OrderByDescending(st => st.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get all support tickets (alias for GetAllTicketsAsync)
    /// </summary>
    public async Task<List<SupportTicket>> GetAllAsync()
    {
        return await GetAllTicketsAsync();
    }

    /// <summary>
    /// Get support tickets by status
    /// </summary>
    public async Task<List<SupportTicket>> GetByStatusAsync(string status)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.SupportTickets
            .Include(st => st.User)
            .Include(st => st.Attachments)
            .Include(st => st.Responses)
                .ThenInclude(r => r.Responder)
            .Where(st => st.Status == status)
            .OrderByDescending(st => st.CreatedAt)
            .ToListAsync();
    }
}
