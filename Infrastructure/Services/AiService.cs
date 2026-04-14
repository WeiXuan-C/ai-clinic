using AiClinic.Application.Services;
using AiClinic.Core.Interfaces;
using Azure.AI.OpenAI;

namespace AiClinic.Infrastructure.Services;

public class AiService : IAiService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly IConfiguration _configuration;

    public AiService(
        IDocumentRepository documentRepository,
        IConfiguration configuration)
    {
        _documentRepository = documentRepository;
        _configuration = configuration;
    }

    public async Task<string> GenerateResponseAsync(string question, Guid patientId)
    {
        // Search relevant documents
        var relevantDocs = await SearchRelevantDocumentsAsync(question, patientId);
        
        // Build context from documents
        var context = string.Join("\n", relevantDocs);
        
        // TODO: Integrate with Azure OpenAI
        // For now, return a placeholder response
        var response = $"Based on your medical documents, here's what I found:\n\n{context}\n\nPlease consult with a doctor for personalized advice.";
        
        return await Task.FromResult(response);
    }

    public async Task<IEnumerable<string>> SearchRelevantDocumentsAsync(string query, Guid patientId)
    {
        var documents = await _documentRepository.GetByPatientIdAsync(patientId);
        
        // TODO: Implement vector similarity search
        // For now, return simple text matching
        var relevantDocs = documents
            .Where(d => d.FileName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(d => $"Document: {d.FileName}")
            .ToList();
        
        return relevantDocs;
    }
}
