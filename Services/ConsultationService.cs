using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

public class ConsultationService
{
    public async Task<ConsultationNote?> GetByIdAsync(Guid noteId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.ConsultationNotes
            .Include(cn => cn.Doctor)
            .Include(cn => cn.Patient)
            .Include(cn => cn.Conversation)
            .Include(cn => cn.Prescriptions)
            .FirstOrDefaultAsync(cn => cn.Id == noteId);
    }

    public async Task<List<ConsultationNote>> GetByConversationIdAsync(Guid conversationId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.ConsultationNotes
            .Include(cn => cn.Doctor)
            .Include(cn => cn.Prescriptions)
            .Where(cn => cn.ConversationId == conversationId)
            .OrderByDescending(cn => cn.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<ConsultationNote>> GetByPatientIdAsync(Guid patientId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.ConsultationNotes
            .Include(cn => cn.Doctor)
            .Include(cn => cn.Conversation)
            .Where(cn => cn.PatientId == patientId)
            .OrderByDescending(cn => cn.CreatedAt)
            .ToListAsync();
    }

    public async Task<ConsultationNote> CreateAsync(ConsultationNote note)
    {
        note.CreatedAt = DateTime.UtcNow;
        note.UpdatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.ConsultationNotes.Add(note);
        await db.SaveChangesAsync();
        return note;
    }

    public async Task<ConsultationNote> UpdateAsync(ConsultationNote note)
    {
        note.UpdatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.ConsultationNotes.Update(note);
        await db.SaveChangesAsync();
        return note;
    }
}
