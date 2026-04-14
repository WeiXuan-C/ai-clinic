using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;
using AiClinic.Infrastructure.Data;

namespace AiClinic.Infrastructure.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly SupabaseContext _context;

    public DocumentRepository(SupabaseContext context)
    {
        _context = context;
    }

    public async Task<Document?> GetByIdAsync(Guid id)
    {
        var response = await _context.Client
            .From<Document>()
            .Where(d => d.Id == id)
            .Single();
        return response;
    }

    public async Task<IEnumerable<Document>> GetAllAsync()
    {
        var response = await _context.Client
            .From<Document>()
            .Get();
        return response.Models;
    }

    public async Task<Document> AddAsync(Document entity)
    {
        var response = await _context.Client
            .From<Document>()
            .Insert(entity);
        return response.Models.First();
    }

    public async Task<Document> UpdateAsync(Document entity)
    {
        var response = await _context.Client
            .From<Document>()
            .Update(entity);
        return response.Models.First();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        await _context.Client
            .From<Document>()
            .Where(d => d.Id == id)
            .Delete();
        return true;
    }

    public async Task<IEnumerable<Document>> GetByPatientIdAsync(Guid patientId)
    {
        var response = await _context.Client
            .From<Document>()
            .Where(d => d.PatientId == patientId)
            .Get();
        return response.Models;
    }

    public async Task<IEnumerable<Document>> SearchByVectorAsync(string query)
    {
        // TODO: Implement vector similarity search using pgvector
        // For now, return empty list
        return await Task.FromResult(new List<Document>());
    }
}
