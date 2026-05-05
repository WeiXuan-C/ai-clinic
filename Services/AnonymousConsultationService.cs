using System.Collections.Concurrent;

namespace ai_clinic.Services;

/// <summary>
/// Service to manage anonymous user consultations with query limits
/// Uses in-memory storage to track query counts per session
/// </summary>
public class AnonymousConsultationService
{
    private readonly AiAssistantService _aiAssistantService;
    private readonly ILogger<AnonymousConsultationService> _logger;
    
    // In-memory storage for session query counts
    // Key: SessionId, Value: Query count
    private static readonly ConcurrentDictionary<string, int> _sessionQueryCounts = new();
    
    // Maximum queries allowed for anonymous users
    private const int MAX_ANONYMOUS_QUERIES = 3;

    public AnonymousConsultationService(
        AiAssistantService aiAssistantService,
        ILogger<AnonymousConsultationService> logger)
    {
        _aiAssistantService = aiAssistantService;
        _logger = logger;
    }

    /// <summary>
    /// Gets remaining query count for a session
    /// </summary>
    public int GetRemainingQueries(string sessionId)
    {
        var usedQueries = _sessionQueryCounts.GetOrAdd(sessionId, 0);
        return Math.Max(0, MAX_ANONYMOUS_QUERIES - usedQueries);
    }

    /// <summary>
    /// Checks if session has reached query limit
    /// </summary>
    public bool HasReachedLimit(string sessionId)
    {
        return GetRemainingQueries(sessionId) <= 0;
    }

    /// <summary>
    /// Sends anonymous query and gets AI response
    /// </summary>
    public async Task<AnonymousQueryResult> SendQueryAsync(string sessionId, string query)
    {
        _logger.LogInformation($"[ANONYMOUS] Query from session: {sessionId}");

        // Check if limit reached
        if (HasReachedLimit(sessionId))
        {
            _logger.LogWarning($"[ANONYMOUS] Session {sessionId} has reached query limit");
            return new AnonymousQueryResult
            {
                Success = false,
                ErrorMessage = "You have reached the maximum number of queries. Please sign in to continue.",
                RequiresLogin = true,
                RemainingQueries = 0
            };
        }

        try
        {
            // Increment query count
            _sessionQueryCounts.AddOrUpdate(sessionId, 1, (key, oldValue) => oldValue + 1);
            
            var remainingQueries = GetRemainingQueries(sessionId);
            _logger.LogInformation($"[ANONYMOUS] Remaining queries for session {sessionId}: {remainingQueries}");

            // Generate AI response
            var aiResponse = await _aiAssistantService.GenerateMedicalResponseAsync(
                patientQuery: query,
                medicalContext: null,
                temperature: 0.7
            );

            return new AnonymousQueryResult
            {
                Success = true,
                Response = aiResponse,
                RemainingQueries = remainingQueries,
                RequiresLogin = remainingQueries == 0
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ANONYMOUS] Error processing query");
            return new AnonymousQueryResult
            {
                Success = false,
                ErrorMessage = "An error occurred while processing your query. Please try again.",
                RemainingQueries = GetRemainingQueries(sessionId)
            };
        }
    }

    /// <summary>
    /// Clears session data (useful for testing or cleanup)
    /// </summary>
    public void ClearSession(string sessionId)
    {
        _sessionQueryCounts.TryRemove(sessionId, out _);
    }

    /// <summary>
    /// Gets total query count for a session
    /// </summary>
    public int GetQueryCount(string sessionId)
    {
        return _sessionQueryCounts.GetOrAdd(sessionId, 0);
    }
}

/// <summary>
/// Result of an anonymous query
/// </summary>
public class AnonymousQueryResult
{
    public bool Success { get; set; }
    public string? Response { get; set; }
    public string? ErrorMessage { get; set; }
    public int RemainingQueries { get; set; }
    public bool RequiresLogin { get; set; }
}
