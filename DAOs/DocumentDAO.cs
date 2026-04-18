using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using Supabase;

namespace AiClinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase client interface to IDocumentRepository interface
/// </summary>
public class DocumentDAO : IDocumentRepository
{
    private readonly Client _supabase;

    public DocumentDAO(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        var response = await _supabase
            .From<Document>()
            .Where(x => x.Id == id)
            .Single();
        
        return response;
    }

    public async Task<IEnumerable<Document>> GetAllAsync()
    {
        var response = await _supabase
            .From<Document>()
            .Get();
        
        return response.Models;
    }

    public async Task<Document> AddAsync(Document entity)
    {
        entity.Id = Guid.NewGuid();
        entity.CreatedAt = DateTime.UtcNow;
        
        var response = await _supabase
            .From<Document>()
            .Insert(entity);
        
        return response.Models.First();
    }

    public async Task<Document> UpdateAsync(Document entity)
    {
        var response = await _supabase
            .From<Document>()
            .Update(entity);
        
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await _supabase
                .From<Document>()
                .Where(x => x.Id == id)
                .Delete();
            
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<IEnumerable<Document>> GetByConversationIdAsync(Guid conversationId)
    {
        var response = await _supabase
            .From<Document>()
            .Where(x => x.ConversationId == conversationId)
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        return response.Models;
    }

    public async Task<IEnumerable<Document>> GetByUserIdAsync(Guid userId)
    {
        var response = await _supabase
            .From<Document>()
            .Where(x => x.UploadedByUserId == userId)
            .Order("created_at", Postgrest.Constants.Ordering.Descending)
            .Get();
        
        return response.Models;
    }

    public async Task<IEnumerable<Document>> GetProcessedDocumentsAsync(Guid conversationId)
    {
        var response = await _supabase
            .From<Document>()
            .Where(x => x.ConversationId == conversationId)
            .Where(x => x.IsProcessed == true)
            .Get();
        
        return response.Models;
    }
}
