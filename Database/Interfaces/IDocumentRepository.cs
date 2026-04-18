using AiClinic.Core.Entities;

namespace AiClinic.Core.Interfaces;

public interface IDocumentRepository : IRepository<Document>
{
    Task<IEnumerable<Document>> GetByConversationIdAsync(Guid conversationId);
    Task<IEnumerable<Document>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Document>> GetProcessedDocumentsAsync(Guid conversationId);
}
