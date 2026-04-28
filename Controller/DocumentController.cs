namespace AiClinic.Controller;

public class DocumentController
{
    private readonly Interfaces.DocumentInterface _documentService;

    public DocumentController(Interfaces.DocumentInterface documentService)
    {
        _documentService = documentService;
    }

    public async Task<object> UploadDocumentAsync(IFormFile file, string userId, string documentType)
    {
        return await _documentService.UploadDocumentAsync(file, userId, documentType);
    }

    public async Task<object?> GetDocumentByIdAsync(string documentId)
    {
        return await _documentService.GetDocumentByIdAsync(documentId);
    }

    public async Task<object> GetDocumentsByUserIdAsync(string userId)
    {
        return await _documentService.GetDocumentsByUserIdAsync(userId);
    }

    public async Task<(Stream fileStream, string contentType, string fileName)> DownloadDocumentAsync(string documentId)
    {
        return await _documentService.DownloadDocumentAsync(documentId);
    }

    public async Task DeleteDocumentAsync(string documentId)
    {
        await _documentService.DeleteDocumentAsync(documentId);
    }
}
