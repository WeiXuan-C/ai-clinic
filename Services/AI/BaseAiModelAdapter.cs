using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace ai_clinic.Services.AI
{
    /// <summary>
    /// Adapter Base Class - Adapts OpenRouter API to our unified interface
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
        public virtual bool SupportsVision => false; // Default: no vision support

        protected BaseAiModelAdapter(OpenRouterApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        }

        /// <summary>
        /// Adapts the OpenRouter API call to our unified interface
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

            var content = response.Choices[0].Message?.Content;
            if (content is string textContent)
            {
                return textContent;
            }

            throw new InvalidOperationException("Empty response from AI model");
        }

        /// <summary>
        /// Generates response with image support
        /// </summary>
        public virtual async Task<string> GenerateResponseWithImagesAsync(
            string prompt,
            List<string> imageBase64List,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000)
        {
            if (!SupportsVision)
                throw new NotSupportedException($"Model {ModelName} does not support vision/image input");

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

            // Build multimodal content
            var contentParts = new List<ContentPart>
            {
                new ContentPart
                {
                    Type = "text",
                    Text = prompt
                }
            };

            // Add images
            foreach (var imageBase64 in imageBase64List)
            {
                contentParts.Add(new ContentPart
                {
                    Type = "image_url",
                    ImageUrl = new ImageUrl
                    {
                        Url = $"data:image/jpeg;base64,{imageBase64}"
                    }
                });
            }

            messages.Add(new Message
            {
                Role = "user",
                Content = contentParts.ToArray()
            });

            // Create request
            var request = new OpenRouterRequest
            {
                Model = ModelId,
                Messages = messages.ToArray(),
                Temperature = temperature,
                MaxTokens = maxTokens,
                Stream = false
            };

            // Call API
            var response = await _apiClient.CallApiAsync(request);

            if (response.Choices == null || response.Choices.Length == 0)
                throw new InvalidOperationException("No response from AI model");

            var content = response.Choices[0].Message?.Content;
            if (content is string textContent)
            {
                return textContent;
            }

            throw new InvalidOperationException("Empty response from AI model");
        }

        /// <summary>
        /// Streaming response adapter with real streaming support
        /// </summary>
        public virtual async Task<IAsyncEnumerable<string>> GenerateStreamingResponseAsync(
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

            // Create request with streaming enabled
            var request = new OpenRouterRequest
            {
                Model = ModelId,
                Messages = messages.ToArray(),
                Temperature = temperature,
                MaxTokens = maxTokens,
                Stream = true
            };

            // Return the streaming enumerable
            return StreamResponseAsync(request);
        }

        /// <summary>
        /// Streaming response with image support
        /// </summary>
        public virtual async Task<IAsyncEnumerable<string>> GenerateStreamingResponseWithImagesAsync(
            string prompt,
            List<string> imageBase64List,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000)
        {
            if (!SupportsVision)
                throw new NotSupportedException($"Model {ModelName} does not support vision/image input");

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

            // Build multimodal content
            var contentParts = new List<ContentPart>
            {
                new ContentPart
                {
                    Type = "text",
                    Text = prompt
                }
            };

            // Add images
            foreach (var imageBase64 in imageBase64List)
            {
                contentParts.Add(new ContentPart
                {
                    Type = "image_url",
                    ImageUrl = new ImageUrl
                    {
                        Url = $"data:image/jpeg;base64,{imageBase64}"
                    }
                });
            }

            messages.Add(new Message
            {
                Role = "user",
                Content = contentParts.ToArray()
            });

            // Create request with streaming enabled
            var request = new OpenRouterRequest
            {
                Model = ModelId,
                Messages = messages.ToArray(),
                Temperature = temperature,
                MaxTokens = maxTokens,
                Stream = true
            };

            // Return the streaming enumerable
            return StreamResponseAsync(request);
        }

        /// <summary>
        /// Internal method to handle streaming
        /// </summary>
        private async IAsyncEnumerable<string> StreamResponseAsync(OpenRouterRequest request)
        {
            await foreach (var chunk in _apiClient.CallApiStreamingAsync(request))
            {
                if (!string.IsNullOrEmpty(chunk))
                {
                    yield return chunk;
                }
            }
        }

        /// <summary>
        /// Hook for model-specific preprocessing
        /// </summary>
        protected virtual string PreprocessPrompt(string prompt)
        {
            return prompt;
        }

        /// <summary>
        /// Hook for model-specific postprocessing
        /// </summary>
        protected virtual string PostprocessResponse(string response)
        {
            return response;
        }
    }
}
