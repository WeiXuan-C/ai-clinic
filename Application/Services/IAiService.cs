namespace AiClinic.Application.Services;

public interface IAiService
{
    Task<string> GenerateResponseAsync(string question, Guid patientId);
    Task<IEnumerable<string>> SearchRelevantDocumentsAsync(string query, Guid patientId);
}
