using AiClinic.Core.Entities;

namespace AiClinic.Core.Interfaces;

public interface IDocumentRepository : IRepository<Document>
{
    Task<IEnumerable<Document>> GetByPatientIdAsync(Guid patientId);
    Task<IEnumerable<Document>> GetByConversationIdAsync(Guid conversationId);
    Task<IEnumerable<Document>> SearchByVectorAsync(string query);
}
