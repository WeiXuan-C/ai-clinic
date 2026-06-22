
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ai_clinic.Services.AI
{
    /// <summary>
    /// Adaptee - The legacy/external OpenRouter API client
    /// </summary>
    public class OpenRouterApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private const string BaseUrl = "https://openrouter.ai/api/v1";

        public OpenRouterApiClient(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _apiKey = configuration["OpenRouter:ApiKey"] 
                ?? throw new InvalidOperationException("OpenRouter API key not configured");
            
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://ai-clinic.app");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "AI Clinic");
            
            Console.WriteLine($"[API CLIENT] Initialized with API Key: {_apiKey.Substring(0, 10)}...");
        }

        /// <summary>
        /// Makes a raw API call to OpenRouter
        /// </summary>
        public async Task<OpenRouterResponse> CallApiAsync(OpenRouterRequest request)
        {
            const string endpoint = "https://openrouter.ai/api/v1/chat/completions";
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, request);
                
                Console.WriteLine($"[API] Response Status: {response.StatusCode}");
                
                // Read response content for debugging
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[API] Response Content Length: {responseContent.Length} chars");
                Console.WriteLine($"[API] Response Preview: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}...");
                
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[API ERROR] Status Code: {response.StatusCode}");
                    Console.WriteLine($"[API ERROR] Full Response: {responseContent}");
                    
                    // Try to parse error response
                    try
                    {
                        var errorResponse = JsonSerializer.Deserialize<OpenRouterErrorResponse>(responseContent);
                        if (errorResponse?.Error != null)
                        {
                            throw new HttpRequestException(
                                $"OpenRouter API Error {errorResponse.Error.Code}: {errorResponse.Error.Message}");
                        }
                    }
                    catch (JsonException)
                    {
                        // If error response is not JSON, throw with raw content
                    }
                    
                    throw new HttpRequestException($"OpenRouter API returned {response.StatusCode}: {responseContent}");
                }
                
                // Try to parse as error response first (OpenRouter sometimes returns errors with 200 status)
                try
                {
                    var errorCheck = JsonSerializer.Deserialize<OpenRouterErrorResponse>(responseContent);
                    if (errorCheck?.Error != null)
                    {
                        Console.WriteLine($"[API ERROR] Error in successful response: {errorCheck.Error.Message}");
                        throw new HttpRequestException(
                            $"OpenRouter API Error {errorCheck.Error.Code}: {errorCheck.Error.Message}");
                    }
                }
                catch (JsonException)
                {
                    // Not an error response, continue
                }
                
                // Deserialize the response
                var result = JsonSerializer.Deserialize<OpenRouterResponse>(responseContent);
                
                if (result == null)
                {
                    Console.WriteLine("[API ERROR] Failed to deserialize response");
                    Console.WriteLine($"[API ERROR] Raw Response: {responseContent}");
                    throw new InvalidOperationException("Failed to deserialize OpenRouter response");
                }
                
                Console.WriteLine($"[API] Response ID: {result.Id}");
                Console.WriteLine($"[API] Choices Count: {result.Choices?.Length ?? 0}");
                
                // Log full response if no choices
                if (result.Choices == null || result.Choices.Length == 0)
                {
                    Console.WriteLine("[API ERROR] No choices in response!");
                    Console.WriteLine($"[API ERROR] Full Response: {responseContent}");
                }
                else
                {
                    var firstChoice = result.Choices[0];
                    var content = firstChoice.Message?.Content;
                    int contentLength = 0;
                    
                    if (content is string str)
                    {
                        contentLength = str.Length;
                    }
                    else if (content is System.Text.Json.JsonElement jsonElement)
                    {
                        // For JsonElement, get the raw text length
                        contentLength = jsonElement.GetRawText()?.Length ?? 0;
                    }
                    
                    Console.WriteLine($"[API] First Choice Content Length: {contentLength} chars (Type: {content?.GetType().Name ?? "null"})");
                }
                Console.WriteLine("=== [OPENROUTER API DEBUG] CallApiAsync Completed ===\n");
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== [OPENROUTER API ERROR] ===");
                Console.WriteLine($"[API ERROR] Exception Type: {ex.GetType().Name}");
                Console.WriteLine($"[API ERROR] Message: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[API ERROR] Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"[API ERROR] Stack Trace: {ex.StackTrace}");
                throw;
            }
        }

        /// <summary>
        /// Gets model information from OpenRouter
        /// </summary>
        public async Task<ModelInfo> GetModelInfoAsync(string modelId)
        {
            var endpoint = $"https://openrouter.ai/api/v1/models/{modelId}";
            var response = await _httpClient.GetAsync(endpoint);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<ModelInfo>();
            return result ?? throw new InvalidOperationException("Failed to get model info");
        }

        /// <summary>
        /// Makes a streaming API call to OpenRouter
        /// </summary>
        public async IAsyncEnumerable<string> CallApiStreamingAsync(OpenRouterRequest request)
        {
            const string endpoint = "https://openrouter.ai/api/v1/chat/completions";
            
            Console.WriteLine("=== [OPENROUTER API] CallApiStreamingAsync Started ===");
            Console.WriteLine($"[API] Model: {request.Model}");
            
            // Ensure streaming is enabled
            request.Stream = true;

            var response = await _httpClient.PostAsJsonAsync(endpoint, request);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"[API ERROR] {response.StatusCode}: {errorContent}");
                throw new HttpRequestException($"OpenRouter API returned {response.StatusCode}");
            }

            // Read the stream
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                
                if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("data: "))
                    continue;

                var data = line.Substring(6); // Remove "data: " prefix
                
                if (data == "[DONE]")
                    break;

                // Parse chunk outside of try-catch to allow yield
                StreamingChunk? chunk = null;
                bool parseSuccess = false;
                
                try
                {
                    chunk = JsonSerializer.Deserialize<StreamingChunk>(data);
                    parseSuccess = true;
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[API WARN] Failed to parse chunk: {ex.Message}");
                }
                
                if (parseSuccess && chunk != null)
                {
                    var content = chunk.Choices?[0]?.Delta?.Content;
                    
                    if (!string.IsNullOrEmpty(content))
                    {
                        yield return content;
                    }
                }
            }

            Console.WriteLine("=== [OPENROUTER API] CallApiStreamingAsync Completed ===\n");
        }
    }

    // Streaming response DTOs
    public class StreamingChunk
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("choices")]
        public StreamingChoice[]? Choices { get; set; }
    }

    public class StreamingChoice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("delta")]
        public Delta? Delta { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class Delta
    {
        [JsonPropertyName("role")]
        public string? Role { get; set; }

        [JsonPropertyName("content")]
        public string? Content { get; set; }
    }

    // OpenRouter API Request/Response DTOs
    public class OpenRouterRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public Message[] Messages { get; set; } = Array.Empty<Message>();

        [JsonPropertyName("temperature")]
        public double? Temperature { get; set; }

        [JsonPropertyName("max_tokens")]
        public int? MaxTokens { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; } = false;
    }

    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public object? Content { get; set; } // Can be string or ContentPart[]
    }

    /// <summary>
    /// Content part for multimodal messages (text + images)
    /// </summary>
    public class ContentPart
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty; // "text" or "image_url"

        [JsonPropertyName("text")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Text { get; set; }

        [JsonPropertyName("image_url")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public ImageUrl? ImageUrl { get; set; }
    }

    /// <summary>
    /// Image URL for multimodal messages
    /// </summary>
    public class ImageUrl
    {
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty; // Can be URL or data:image/jpeg;base64,...
    }

    public class OpenRouterResponse
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("choices")]
        public Choice[] Choices { get; set; } = Array.Empty<Choice>();

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("index")]
        public int Index { get; set; }

        [JsonPropertyName("message")]
        public Message? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    public class ModelInfo
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("context_length")]
        public int ContextLength { get; set; }
    }

    // OpenRouter Error Response
    public class OpenRouterErrorResponse
    {
        [JsonPropertyName("error")]
        public ErrorDetail? Error { get; set; }
    }

    public class ErrorDetail
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("metadata")]
        public object? Metadata { get; set; }
    }
}
