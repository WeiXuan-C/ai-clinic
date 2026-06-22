namespace ai_clinic.Services.AI.Strategies
{
    /// <summary>
    /// Concrete Strategy - MiniMax M2.5 Model
    ///
    /// MiniMax's latest model with strong multilingual and reasoning capabilities
    /// </summary>
    public class MiniMaxStrategy : BaseAiModelAdapter
    {
        public override string ModelId => "minimax/minimax-m2.5:free";
        public override string ModelName => "MiniMax M2.5 (Free)";

        public MiniMaxStrategy(OpenRouterApiClient apiClient) : base(apiClient)
        {
        }

        /// <summary>
        /// MiniMax excels at multilingual tasks and conversational AI
        /// </summary>
        protected override string PreprocessPrompt(string prompt)
        {
            // MiniMax handles natural conversation well
            return base.PreprocessPrompt(prompt);
        }

        protected override string PostprocessResponse(string response)
        {
            // Clean up any model-specific formatting
            return base.PostprocessResponse(response);
        }

        /// <summary>
        /// Override streaming response to use model-specific multilingual processing
        /// </summary>
        public override async Task<IAsyncEnumerable<string>> GenerateStreamingResponseAsync(
            string prompt,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000)
        {
            Console.WriteLine($"[MINIMAX] Preprocessing prompt for multilingual model");
            
            // Apply model-specific preprocessing for conversational style
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

            Console.WriteLine($"[MINIMAX] Starting streaming for model: {ModelId}");
            return StreamChunks(request);
        }

        private async IAsyncEnumerable<string> StreamChunks(OpenRouterRequest request)
        {
            await foreach (var chunk in _apiClient.CallApiStreamingAsync(request))
            {
                if (!string.IsNullOrEmpty(chunk))
                {
                    // Apply model-specific formatting cleanup
                    var processedChunk = PostprocessResponse(chunk);
                    yield return processedChunk;
                }
            }
            Console.WriteLine($"[MINIMAX] Streaming completed");
        }
    }
}
