using AiClinic.UI.State;

namespace AiClinic.Services;

/// <summary>
/// Document Service - Business Logic Layer
/// Handles document operations through state management
/// </summary>
public class DocumentService
{
    private readonly DocumentState _state;

    public DocumentService(DocumentState state)
    {
        _state = state;
    }

    /// <summary>
    /// Gets all documents
    /// </summary>
    public async Task<IEnumerable<Document>> GetAllDocumentsAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Gets a document by ID
    /// </summary>
    public async Task<Document?> GetDocumentByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets documents by conversation ID
    /// </summary>
    public async Task<IEnumerable<Document>> GetDocumentsByConversationIdAsync(Guid conversationId)
    {
        return await _state.GetByConversationIdAsync(conversationId);
    }

    /// <summary>
    /// Gets documents by user ID
    /// </summary>
    public async Task<IEnumerable<Document>> GetDocumentsByUserIdAsync(Guid userId)
    {
        return await _state.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Gets processed documents for a conversation
    /// </summary>
    public async Task<IEnumerable<Document>> GetProcessedDocumentsAsync(Guid conversationId)
    {
        return await _state.GetProcessedDocumentsAsync(conversationId);
    }

    /// <summary>
    /// Creates a new document
    /// </summary>
    public async Task<Document?> CreateDocumentAsync(Document document)
    {
        return await _state.CreateAsync(document);
    }

    /// <summary>
    /// Updates a document
    /// </summary>
    public async Task<Document?> UpdateDocumentAsync(Document document)
    {
        return await _state.UpdateAsync(document);
    }

    /// <summary>
    /// Deletes a document
    /// </summary>
    public async Task<bool> DeleteDocumentAsync(Guid id)
    {
        return await _state.DeleteAsync(id);
    }

    /// <summary>
    /// Gets cached documents from state
    /// </summary>
    public IReadOnlyList<Document> GetCachedDocuments()
    {
        return _state.Documents;
    }

    /// <summary>
    /// Gets the currently selected document
    /// </summary>
    public Document? GetSelectedDocument()
    {
        return _state.SelectedDocument;
    }

    /// <summary>
    /// Sets the selected document
    /// </summary>
    public void SetSelectedDocument(Document? document)
    {
        _state.SelectedDocument = document;
    }
}
