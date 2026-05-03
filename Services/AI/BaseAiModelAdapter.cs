using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ai_clinic.Services.AI
{
    /// <summary>
    /// Adapter Base Class - Adapts OpenRouter API to our unified interface
    /// 适配器基类 - 将OpenRouter API适配到我们的统一接口
    /// 
    /// This follows the Adapter pattern by:
    /// 1. Implementing the Target interface (IAiModelStrategy)
    /// 2. Wrapping the Adaptee (OpenRouterApiClient)
    /// 3. Converting between incompatible interfaces
    /// </summary>
    public abstract class BaseAiModelAdapter : IAiModelStrategy
    {
        protected readonly OpenRouterApiClient _apiClient;

        public abstract string ModelId { get; }
        public abstract string ModelName { get; }

        protected BaseAiModelAdapter(OpenRouterApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <summary>
        /// Adapts the OpenRouter API call to our unified interface
        /// 将OpenRouter API调用适配到我们的统一接口
        /// </summary>
        public virtual async Task<string> GenerateResponseAsync(
            string prompt,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                throw new ArgumentException("Prompt cannot be empty", nameof(prompt));

            // Build messages array
            var messages = new List<Message>();
            
            if (!string.IsNullOrWhiteSpace(systemInstructions))
            {
                messages.Add(new Message 
                { 
                    Role = "system", 
                    Content = systemInstructions 
                });
            }

            messages.Add(new Message 
            { 
                Role = "user", 
                Content = prompt 
            });

            // Create request using the Adaptee's format
            var request = new OpenRouterRequest
            {
                Model = ModelId,
                Messages = messages.ToArray(),
                Temperature = temperature,
                MaxTokens = maxTokens,
                Stream = false
            };

            // Call the Adaptee (OpenRouter API)
            var response = await _apiClient.CallApiAsync(request);

            // Adapt the response to our expected format
            if (response.Choices == null || response.Choices.Length == 0)
                throw new InvalidOperationException("No response from AI model");

            return response.Choices[0].Message?.Content 
                ?? throw new InvalidOperationException("Empty response from AI model");
        }

        /// <summary>
        /// Streaming response adapter (placeholder for future implementation)
        /// 流式响应适配器(未来实现的占位符)
        /// </summary>
        public virtual async Task<IAsyncEnumerable<string>> GenerateStreamingResponseAsync(
            string prompt,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000)
        {
            // For now, return a simple async enumerable with the full response
            var response = await GenerateResponseAsync(prompt, systemInstructions, temperature, maxTokens);
            return CreateAsyncEnumerable(response);
        }

        private async IAsyncEnumerable<string> CreateAsyncEnumerable(string response)
        {
            await Task.Yield();
            yield return response;
        }

        /// <summary>
        /// Hook for model-specific preprocessing
        /// 模型特定预处理的钩子方法
        /// </summary>
        protected virtual string PreprocessPrompt(string prompt)
        {
            return prompt;
        }

        /// <summary>
        /// Hook for model-specific postprocessing
        /// 模型特定后处理的钩子方法
        /// </summary>
        protected virtual string PostprocessResponse(string response)
        {
            return response;
        }
    }
}
