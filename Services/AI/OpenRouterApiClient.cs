using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace ai_clinic.Services.AI
{
    /// <summary>
    /// Adaptee - The legacy/external OpenRouter API client
    /// 被适配者 - 外部OpenRouter API客户端
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
            
            _httpClient.BaseAddress = new Uri(BaseUrl);
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        }

        /// <summary>
        /// Makes a raw API call to OpenRouter
        /// 向OpenRouter发起原始API调用
        /// </summary>
        public async Task<OpenRouterResponse> CallApiAsync(OpenRouterRequest request)
        {
            var response = await _httpClient.PostAsJsonAsync("/chat/completions", request);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<OpenRouterResponse>();
            return result ?? throw new InvalidOperationException("Failed to deserialize OpenRouter response");
        }

        /// <summary>
        /// Gets model information from OpenRouter
        /// 从OpenRouter获取模型信息
        /// </summary>
        public async Task<ModelInfo> GetModelInfoAsync(string modelId)
        {
            var response = await _httpClient.GetAsync($"/models/{modelId}");
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<ModelInfo>();
            return result ?? throw new InvalidOperationException("Failed to get model info");
        }
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
        public string Content { get; set; } = string.Empty;
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
}
