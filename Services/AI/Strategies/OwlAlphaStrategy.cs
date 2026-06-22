namespace ai_clinic.Services.AI.Strategies
{
    /// <summary>
    /// Concrete Strategy - OpenRouter Owl Alpha Model
    ///
    /// This is a high-performance reasoning model optimized for complex tasks
    /// </summary>
    public class OwlAlphaStrategy : BaseAiModelAdapter
    {
        public override string ModelId => "openrouter/owl-alpha";
        public override string ModelName => "Owl Alpha (Reasoning)";

        public OwlAlphaStrategy(OpenRouterApiClient apiClient) : base(apiClient)
        {
        }

        /// <summary>
        /// Owl Alpha excels at reasoning tasks, so we can add specific preprocessing
        /// </summary>
        protected override string PreprocessPrompt(string prompt)
        {
            // Add reasoning-specific context if needed
            return base.PreprocessPrompt(prompt);
        }

        /// <summary>
        /// Override streaming response to use model-specific preprocessing
        /// </summary>
        public override async Task<IAsyncEnumerable<string>> GenerateStreamingResponseAsync(
            string prompt,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000)
        {
            Console.WriteLine($"[OWL ALPHA] Preprocessing prompt for reasoning model");
            
            // Apply model-specific preprocessing
            var preprocessedPrompt = PreprocessPrompt(prompt);
            
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
                Content = preprocessedPrompt
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

            Console.WriteLine($"[OWL ALPHA] Starting streaming for model: {ModelId}");
            return StreamChunks(request);
        }

        private async IAsyncEnumerable<string> StreamChunks(OpenRouterRequest request)
        {
            await foreach (var chunk in _apiClient.CallApiStreamingAsync(request))
            {
                if (!string.IsNullOrEmpty(chunk))
                {
                    // Apply postprocessing if needed
                    var processedChunk = PostprocessResponse(chunk);
                    yield return processedChunk;
                }
            }
            Console.WriteLine($"[OWL ALPHA] Streaming completed");
        }
    }
}
