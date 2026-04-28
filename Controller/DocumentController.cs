namespace AiClinic.Controller;

public class DocumentController
{
    private readonly Services.DocumentService _documentService;

    public DocumentController(Services.DocumentService documentService)
    {
        _documentService = documentService;
    }

    public async Task<object> UploadDocumentAsync(IFormFile file, string userId, string documentType)
    {
        var request = new
        {
            FileName = file.FileName,
            FileType = documentType,
            UploadedByUserId = userId,
            ConversationId = Guid.Empty.ToString()
        };
        return await _documentService.UploadDocumentAsync(request);
    }

    public async Task<object?> GetDocumentByIdAsync(string documentId)
    {
        return await _documentService.GetDocumentByIdAsync(documentId);
    }

    public async Task<object> GetDocumentsByUserIdAsync(string userId)
    {
        if (Guid.TryParse(userId, out var guid))
        {
            return await _documentService.GetDocumentsByUserIdAsync(guid);
        }
        return Enumerable.Empty<object>();
    }

    public async Task<(Stream fileStream, string contentType, string fileName)> DownloadDocumentAsync(string documentId)
    {
        var bytes = await _documentService.DownloadDocumentAsync(documentId);
        if (bytes == null)
        {
            return (Stream.Null, "application/octet-stream", "file");
        }
        return (new MemoryStream(bytes), "application/octet-stream", "document");
    }

    public async Task DeleteDocumentAsync(string documentId)
    {
        await _documentService.DeleteDocumentAsync(documentId);
    }
}
