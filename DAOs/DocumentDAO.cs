using ai_clinic.Interfaces;
using ai_clinic.Database;

namespace ai_clinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase HTTP client to IDocumentRepository interface
/// </summary>
public class DocumentDAO : IDocumentRepository
{
    private readonly SupabaseHttpClient _supabase;

    public DocumentDAO(SupabaseHttpClient supabase)
    {
        _supabase = supabase;
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        try
        {
            return await _supabase.GetSingleAsync<Document>("documents", $"id=eq.{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<IEnumerable<Document>> GetAllAsync()
    {
        return await _supabase.GetAsync<Document>("documents");
    }

    public async Task<Document> AddAsync(Document entity)
    {
        var result = await _supabase.PostAsync<Document>("documents", entity);
        return result ?? entity;
    }

    public async Task<Document> UpdateAsync(Document entity)
    {
        var result = await _supabase.PatchAsync<Document>("documents", $"id=eq.{entity.Id}", entity);
        return result ?? entity;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _supabase.DeleteAsync("documents", $"id=eq.{id}");
    }

    public async Task<IEnumerable<Document>> GetByConversationIdAsync(Guid conversationId)
    {
        var filter = $"conversation_id=eq.{conversationId}&order=created_at.desc";
        return await _supabase.GetAsync<Document>("documents", filter);
    }

    public async Task<IEnumerable<Document>> GetByUserIdAsync(Guid userId)
    {
        var filter = $"uploaded_by_user_id=eq.{userId}&order=created_at.desc";
        return await _supabase.GetAsync<Document>("documents", filter);
    }

    public async Task<IEnumerable<Document>> GetProcessedDocumentsAsync(Guid conversationId)
    {
        var filter = $"conversation_id=eq.{conversationId}&is_processed=eq.true";
        return await _supabase.GetAsync<Document>("documents", filter);
    }
}
