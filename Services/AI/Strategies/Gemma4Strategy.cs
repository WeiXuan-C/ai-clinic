namespace ai_clinic.Services.AI.Strategies
{
    /// <summary>
    /// Concrete Strategy - Google Gemma 4 26B Model
    ///
    /// A powerful open-source model from Google with strong general capabilities
    /// </summary>
    public class Gemma4Strategy : BaseAiModelAdapter
    {
        public override string ModelId => "google/gemma-4-26b-a4b-it:free";
        public override string ModelName => "Google Gemma 4 26B (Free)";

        public Gemma4Strategy(OpenRouterApiClient apiClient) : base(apiClient)
        {
        }

        /// <summary>
        /// Gemma 4 is instruction-tuned, so it works well with clear instructions
        /// </summary>
        protected override string PreprocessPrompt(string prompt)
        {
            // Gemma models respond well to structured prompts
            return base.PreprocessPrompt(prompt);
        }

        protected override string PostprocessResponse(string response)
        {
            // Clean up any model-specific artifacts if needed
            return base.PostprocessResponse(response);
        }

        /// <summary>
        /// Override streaming response to use model-specific pre/post processing
        /// </summary>
        public override async Task<IAsyncEnumerable<string>> GenerateStreamingResponseAsync(
            string prompt,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000)
        {
            Console.WriteLine($"[GEMMA 4] Preprocessing prompt for instruction-tuned model");
            
            // Apply model-specific preprocessing for structured prompts
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

            Console.WriteLine($"[GEMMA 4] Starting streaming for model: {ModelId}");
            return StreamChunks(request);
        }

        private async IAsyncEnumerable<string> StreamChunks(OpenRouterRequest request)
        {
            await foreach (var chunk in _apiClient.CallApiStreamingAsync(request))
            {
                if (!string.IsNullOrEmpty(chunk))
                {
                    // Apply model-specific postprocessing to clean up artifacts
                    var processedChunk = PostprocessResponse(chunk);
                    yield return processedChunk;
                }
            }
            Console.WriteLine($"[GEMMA 4] Streaming completed");
        }
    }
}
