using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

/// <summary>
/// Service for managing Conversations between patients and doctors
/// </summary>
public class ConversationService
{
    /// <summary>
    /// Get conversation by ID with all related data
    /// </summary>
    public async Task<Conversation?> GetByIdAsync(Guid conversationId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Conversations
            .Include(c => c.Patient)
            .Include(c => c.AssignedDoctor)
            .Include(c => c.Messages)
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == conversationId);
    }

    /// <summary>
    /// Get all conversations for a patient
    /// </summary>
    public async Task<List<Conversation>> GetByPatientIdAsync(Guid patientId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Conversations
            .Include(c => c.AssignedDoctor)
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get all conversations for a doctor
    /// </summary>
    public async Task<List<Conversation>> GetByDoctorIdAsync(Guid doctorId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Conversations
            .Include(c => c.Patient)
            .Where(c => c.AssignedDoctorId == doctorId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Create a new conversation
    /// </summary>
    public async Task<Conversation> CreateAsync(Conversation conversation)
    {
        conversation.CreatedAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();
        return conversation;
    }

    /// <summary>
    /// Assign a doctor to a conversation
    /// </summary>
    public async Task AssignDoctorAsync(Guid conversationId, Guid doctorId)
    {
        using var db = DbClient.Instance.GetDb();
        var conversation = await db.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.AssignedDoctorId = doctorId;
            conversation.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Update conversation status
    /// </summary>
    public async Task UpdateStatusAsync(Guid conversationId, ConversationStatus status)
    {
        using var db = DbClient.Instance.GetDb();
        var conversation = await db.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.Status = status;
            conversation.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get active conversations (not closed or completed)
    /// </summary>
    public async Task<List<Conversation>> GetActiveConversationsAsync()
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Conversations
            .Include(c => c.Patient)
            .Include(c => c.AssignedDoctor)
            .Where(c => c.Status == ConversationStatus.Active)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }
}
