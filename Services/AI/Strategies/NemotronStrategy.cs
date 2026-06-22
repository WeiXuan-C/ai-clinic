namespace ai_clinic.Services.AI.Strategies
{
    /// <summary>
    /// Concrete Strategy - NVIDIA Nemotron 3 Nano Omni Model
    ///
    /// A free, lightweight multimodal model with reasoning capabilities
    /// </summary>
    public class NemotronStrategy : BaseAiModelAdapter
    {
        public override string ModelId => "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free";
        public override string ModelName => "NVIDIA Nemotron 3 Nano (Free)";

        public NemotronStrategy(OpenRouterApiClient apiClient) : base(apiClient)
        {
        }

        /// <summary>
        /// Nemotron is optimized for efficiency, suitable for quick responses
        /// </summary>
        protected override string PreprocessPrompt(string prompt)
        {
            // Optimize for concise responses
            return base.PreprocessPrompt(prompt);
        }

        /// <summary>
        /// Override streaming response to use model-specific efficient processing
        /// </summary>
        public override async Task<IAsyncEnumerable<string>> GenerateStreamingResponseAsync(
            string prompt,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000)
        {
            Console.WriteLine($"[NEMOTRON] Preprocessing prompt for efficient nano model");
            
            // Apply model-specific preprocessing for concise responses
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

            Console.WriteLine($"[NEMOTRON] Starting streaming for model: {ModelId}");
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
            Console.WriteLine($"[NEMOTRON] Streaming completed");
        }
    }
}
