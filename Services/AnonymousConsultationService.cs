using System.Collections.Concurrent;
using Microsoft.JSInterop;

namespace ai_clinic.Services;

/// <summary>
/// Service to manage anonymous user consultations with query limits
/// Uses localStorage for persistent credit tracking across page refreshes
/// </summary>
public class AnonymousConsultationService
{
    private readonly AiAssistantService _aiAssistantService;
    private readonly ILogger<AnonymousConsultationService> _logger;
    private readonly IJSRuntime _jsRuntime;
    
    // In-memory storage for session query counts (fallback)
    // Key: SessionId, Value: Query count
    private static readonly ConcurrentDictionary<string, int> _sessionQueryCounts = new();
    
    // Maximum queries allowed for anonymous users
    private const int MAX_ANONYMOUS_QUERIES = 3;
    private const string LOCALSTORAGE_KEY_PREFIX = "ai_clinic_anonymous_credits_";

    public AnonymousConsultationService(
        AiAssistantService aiAssistantService,
        ILogger<AnonymousConsultationService> logger,
        IJSRuntime jsRuntime)
    {
        _aiAssistantService = aiAssistantService;
        _logger = logger;
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Gets remaining query count for a session from localStorage
    /// </summary>
    public async Task<int> GetRemainingQueriesAsync(string sessionId)
    {
        var usedQueries = await GetUsedQueriesFromStorageAsync(sessionId);
        return Math.Max(0, MAX_ANONYMOUS_QUERIES - usedQueries);
    }

    /// <summary>
    /// Checks if session has reached query limit
    /// </summary>
    public async Task<bool> HasReachedLimitAsync(string sessionId)
    {
        var remaining = await GetRemainingQueriesAsync(sessionId);
        return remaining <= 0;
    }

    /// <summary>
    /// Gets used query count from localStorage
    /// </summary>
    private async Task<int> GetUsedQueriesFromStorageAsync(string sessionId)
    {
        try
        {
            var storageKey = $"{LOCALSTORAGE_KEY_PREFIX}{sessionId}";
            var storedValue = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", storageKey);
            
            if (!string.IsNullOrEmpty(storedValue) && int.TryParse(storedValue, out var count))
            {
                return count;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ANONYMOUS] Failed to read from localStorage, using in-memory fallback");
        }
        
        // Fallback to in-memory storage
        return _sessionQueryCounts.GetOrAdd(sessionId, 0);
    }

    /// <summary>
    /// Saves used query count to localStorage
    /// </summary>
    private async Task SaveUsedQueriesToStorageAsync(string sessionId, int count)
    {
        try
        {
            var storageKey = $"{LOCALSTORAGE_KEY_PREFIX}{sessionId}";
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", storageKey, count.ToString());
            
            // Also update in-memory storage as fallback
            _sessionQueryCounts.AddOrUpdate(sessionId, count, (key, oldValue) => count);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ANONYMOUS] Failed to write to localStorage, using in-memory fallback only");
            
            // Update in-memory storage as fallback
            _sessionQueryCounts.AddOrUpdate(sessionId, count, (key, oldValue) => count);
        }
    }

    /// <summary>
    /// Sends anonymous query and gets AI response
    /// </summary>
    public async Task<AnonymousQueryResult> SendQueryAsync(string sessionId, string query)
    {
        _logger.LogInformation($"[ANONYMOUS] Query from session: {sessionId}");

        // Check if limit reached
        if (await HasReachedLimitAsync(sessionId))
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
            // Get current count BEFORE incrementing
            var currentCount = await GetUsedQueriesFromStorageAsync(sessionId);
            
            _logger.LogInformation($"[ANONYMOUS] Processing query for session {sessionId}, current used count: {currentCount}");

            // Generate AI response FIRST (before decrementing credits)
            var aiResponse = await _aiAssistantService.GenerateMedicalResponseAsync(
                patientQuery: query,
                medicalContext: null,
                temperature: 0.7
            );

            // Only decrement credits if AI call was successful
            var newCount = currentCount + 1;
            await SaveUsedQueriesToStorageAsync(sessionId, newCount);
            
            var remainingQueries = MAX_ANONYMOUS_QUERIES - newCount;
            _logger.LogInformation($"[ANONYMOUS] Query successful. Used: {newCount}, Remaining: {remainingQueries}");

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
            // Credits NOT decremented on error - user keeps their credit
            _logger.LogError(ex, "[ANONYMOUS] Error processing query - credits NOT deducted");
            var remainingQueries = await GetRemainingQueriesAsync(sessionId);
            return new AnonymousQueryResult
            {
                Success = false,
                ErrorMessage = "An error occurred while processing your query. Please try again.",
                RemainingQueries = remainingQueries
            };
        }
    }

    /// <summary>
    /// Clears session data (useful for testing or cleanup)
    /// </summary>
    public async Task ClearSessionAsync(string sessionId)
    {
        try
        {
            var storageKey = $"{LOCALSTORAGE_KEY_PREFIX}{sessionId}";
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", storageKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[ANONYMOUS] Failed to clear localStorage");
        }
        
        _sessionQueryCounts.TryRemove(sessionId, out _);
    }

    /// <summary>
    /// Gets total query count for a session
    /// </summary>
    public async Task<int> GetQueryCountAsync(string sessionId)
    {
        return await GetUsedQueriesFromStorageAsync(sessionId);
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
