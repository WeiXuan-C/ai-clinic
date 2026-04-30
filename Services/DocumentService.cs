using ai_clinic.Interfaces;
using ai_clinic.UI.State;

namespace ai_clinic.Services;

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
    public async Task<IEnumerable<IDocument>> GetAllDocumentsAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Gets a document by ID
    /// </summary>
    public async Task<IDocument?> GetDocumentByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Gets documents by conversation ID
    /// </summary>
    public async Task<IEnumerable<IDocument>> GetDocumentsByConversationIdAsync(Guid conversationId)
    {
        return await _state.GetByConversationIdAsync(conversationId);
    }

    /// <summary>
    /// Gets documents by user ID
    /// </summary>
    public async Task<IEnumerable<IDocument>> GetDocumentsByUserIdAsync(Guid userId)
    {
        return await _state.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Gets processed documents for a conversation
    /// </summary>
    public async Task<IEnumerable<IDocument>> GetProcessedDocumentsAsync(Guid conversationId)
    {
        return await _state.GetProcessedDocumentsAsync(conversationId);
    }

    /// <summary>
    /// Creates a new document
    /// </summary>
    public async Task<IDocument?> CreateDocumentAsync(IDocument document)
    {
        var concreteDocument = document as Document ?? new Document
        {
            Id = document.Id,
            ConversationId = document.ConversationId,
            UploadedByUserId = document.UploadedByUserId,
            FileName = document.FileName,
            FileType = document.FileType,
            FileSizeBytes = document.FileSizeBytes,
            FileUrl = document.FileUrl,
            MimeType = document.MimeType,
            IsProcessed = document.IsProcessed,
            ExtractedText = document.ExtractedText,
            Description = document.Description,
            Tags = document.Tags,
            CreatedAt = document.CreatedAt
        };
        return await _state.CreateAsync(concreteDocument);
    }

    /// <summary>
    /// Updates a document
    /// </summary>
    public async Task<IDocument?> UpdateDocumentAsync(IDocument document)
    {
        var concreteDocument = document as Document ?? new Document
        {
            Id = document.Id,
            ConversationId = document.ConversationId,
            UploadedByUserId = document.UploadedByUserId,
            FileName = document.FileName,
            FileType = document.FileType,
            FileSizeBytes = document.FileSizeBytes,
            FileUrl = document.FileUrl,
            MimeType = document.MimeType,
            IsProcessed = document.IsProcessed,
            ExtractedText = document.ExtractedText,
            Description = document.Description,
            Tags = document.Tags,
            CreatedAt = document.CreatedAt
        };
        return await _state.UpdateAsync(concreteDocument);
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
    public IReadOnlyList<IDocument> GetCachedDocuments()
    {
        return _state.Documents.Cast<IDocument>().ToList();
    }

    /// <summary>
    /// Gets the currently selected document
    /// </summary>
    public IDocument? GetSelectedDocument()
    {
        return _state.SelectedDocument;
    }

    /// <summary>
    /// Sets the selected document
    /// </summary>
    public void SetSelectedDocument(IDocument? document)
    {
        _state.SelectedDocument = document as Document;
    }

    // Controller-facing methods (adapters for backward compatibility)
    
    public async Task<IDocument?> UploadDocumentAsync(object request)
    {
        // Extract properties from request object dynamically
        var requestType = request.GetType();
        var conversationId = Guid.Parse(requestType.GetProperty("ConversationId")?.GetValue(request)?.ToString() ?? Guid.NewGuid().ToString());
        var uploadedByUserId = Guid.Parse(requestType.GetProperty("UploadedByUserId")?.GetValue(request)?.ToString() ?? Guid.NewGuid().ToString());
        var fileName = requestType.GetProperty("FileName")?.GetValue(request)?.ToString() ?? string.Empty;
        var fileType = requestType.GetProperty("FileType")?.GetValue(request)?.ToString() ?? string.Empty;
        var fileUrl = requestType.GetProperty("FileUrl")?.GetValue(request)?.ToString() ?? string.Empty;
        
        var document = new Document
        {
            Id = Guid.NewGuid(),
            ConversationId = conversationId,
            UploadedByUserId = uploadedByUserId,
            FileName = fileName,
            FileType = fileType,
            FileSizeBytes = 0,
            FileUrl = fileUrl,
            IsProcessed = false,
            CreatedAt = DateTime.UtcNow
        };
        
        return await CreateDocumentAsync((IDocument)document);
    }
    
    public async Task<IDocument?> GetDocumentByIdAsync(string documentId)
    {
        if (Guid.TryParse(documentId, out var guid))
        {
            return await GetDocumentByIdAsync(guid);
        }
        return null;
    }
    
    public async Task<IEnumerable<IDocument>> GetDocumentsByConversationIdAsync(string conversationId)
    {
        if (Guid.TryParse(conversationId, out var guid))
        {
            return await GetDocumentsByConversationIdAsync(guid);
        }
        return Enumerable.Empty<IDocument>();
    }
    
    public async Task<byte[]?> DownloadDocumentAsync(string documentId)
    {
        // Placeholder implementation
        return null;
    }
    
    public async Task<bool> DeleteDocumentAsync(string documentId)
    {
        if (Guid.TryParse(documentId, out var guid))
        {
            return await DeleteDocumentAsync(guid);
        }
        return false;
    }
}
