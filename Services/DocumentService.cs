using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

/// <summary>
/// Service for managing Documents (medical files, images, etc.)
/// </summary>
public class DocumentService
{
    public async Task<Document?> GetByIdAsync(Guid documentId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Documents
            .Include(d => d.UploadedByUser)
            .Include(d => d.Conversation)
            .FirstOrDefaultAsync(d => d.Id == documentId);
    }

    public async Task<List<Document>> GetByConversationIdAsync(Guid conversationId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Documents
            .Include(d => d.UploadedByUser)
            .Where(d => d.ConversationId == conversationId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Document>> GetByPatientIdAsync(Guid patientId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Documents
            .Include(d => d.UploadedByUser)
            .Where(d => d.PatientId == patientId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Document> CreateAsync(Document document)
    {
        document.CreatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.Documents.Add(document);
        await db.SaveChangesAsync();
        return document;
    }

    public async Task<bool> DeleteAsync(Guid documentId)
    {
        using var db = DbClient.Instance.GetDb();
        var document = await db.Documents.FindAsync(documentId);
        if (document != null)
        {
            db.Documents.Remove(document);
            await db.SaveChangesAsync();
            return true;
        }
        return false;
    }

    public async Task<List<Document>> GetByFileTypeAsync(Guid conversationId, DocumentType fileType)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Documents
            .Where(d => d.ConversationId == conversationId && d.FileType == fileType)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }
}
