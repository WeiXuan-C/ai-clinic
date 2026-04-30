using ai_clinic.Interfaces;

namespace ai_clinic.Controller;

public class DocumentController(Services.DocumentService documentService)
{
    public Task<IDocument?> UploadDocumentAsync(IFormFile file, string userId, string documentType)
    {
        var request = new
        {
            file.FileName,
            FileType = documentType,
            UploadedByUserId = userId,
            ConversationId = Guid.Empty.ToString()
        };
        return documentService.UploadDocumentAsync(request);
    }

    public Task<IDocument?> GetDocumentByIdAsync(string documentId)
    {
        return documentService.GetDocumentByIdAsync(documentId);
    }

    public async Task<IEnumerable<IDocument>> GetDocumentsByUserIdAsync(string userId)
    {
        if (Guid.TryParse(userId, out var guid))
        {
            return await documentService.GetDocumentsByUserIdAsync(guid);
        }
        return Enumerable.Empty<IDocument>();
    }

    public async Task<(Stream fileStream, string contentType, string fileName)> DownloadDocumentAsync(string documentId)
    {
        var bytes = await documentService.DownloadDocumentAsync(documentId);
        if (bytes == null)
        {
            return (Stream.Null, "application/octet-stream", "file");
        }
        return (new MemoryStream(bytes), "application/octet-stream", "document");
    }

    public Task<bool> DeleteDocumentAsync(string documentId)
    {
        return documentService.DeleteDocumentAsync(documentId);
    }
}
