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
            {
                Console.WriteLine($"[ADAPTER ERROR] No choices in response for model: {ModelId}");
                throw new InvalidOperationException($"No response from AI model {ModelName} (ID: {ModelId})");
            }

            var content = response.Choices[0].Message?.Content;

            // Handle different content types
            string? textContent = null;

            if (content is string str)
            {
                textContent = str;
            }
            else if (content is System.Text.Json.JsonElement jsonElement)
            {
                Console.WriteLine($"[ADAPTER DEBUG] JsonElement ValueKind: {jsonElement.ValueKind}");
                
                // Handle JsonElement type - extract string value
                if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    textContent = jsonElement.GetString();
                }
                else if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Array ||
                         jsonElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    // For complex types, serialize to JSON string
                    textContent = jsonElement.GetRawText();
                }
                else
                {
                    // Try to get raw text for any other type
                    textContent = jsonElement.ToString();
                }
                
                Console.WriteLine($"[ADAPTER DEBUG] Extracted text length: {textContent?.Length ?? 0}");
            }
            else if (content != null)
            {
                // Fallback: try to convert to string
                textContent = content.ToString();
            }

            if (!string.IsNullOrWhiteSpace(textContent))
            {
                Console.WriteLine($"[ADAPTER SUCCESS] Returning response with {textContent.Length} characters");
                return textContent;
            }

            Console.WriteLine($"[ADAPTER ERROR] Empty or null content in response for model: {ModelId}");
            Console.WriteLine($"[ADAPTER ERROR] Content type: {content?.GetType().Name ?? "null"}");
            Console.WriteLine($"[ADAPTER ERROR] Content value: {content ?? "null"}");

            throw new InvalidOperationException($"Empty response from AI model {ModelName} (ID: {ModelId}). This may indicate an invalid model ID or the model returned no content.");
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
