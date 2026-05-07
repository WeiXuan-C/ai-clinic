
using ai_clinic.Services.AI.Strategies;

namespace ai_clinic.Services.AI
{
    /// <summary>
    /// Context - Manages AI model strategy selection and execution
    /// 
    /// This is the Context in the Strategy pattern that:
    /// 1. Maintains a reference to a Strategy object
    /// 2. Allows dynamic strategy switching
    /// 3. Delegates work to the current strategy
    /// </summary>
    public class AiModelContext
    {
        private IAiModelStrategy _currentStrategy;
        private readonly Dictionary<string, IAiModelStrategy> _availableStrategies;
        private readonly OpenRouterApiClient _apiClient;

        public AiModelContext(OpenRouterApiClient apiClient)
        {
            _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));

            // Initialize all available strategies
            _availableStrategies = new Dictionary<string, IAiModelStrategy>
            {
                ["owl-alpha"] = new OwlAlphaStrategy(_apiClient),
                ["gemma-4"] = new Gemma4Strategy(_apiClient),
                ["minimax"] = new MiniMaxStrategy(_apiClient),
                ["nemotron"] = new NemotronStrategy(_apiClient)
            };

            // Set default strategy
            _currentStrategy = _availableStrategies["owl-alpha"];
        }

        /// <summary>
        /// Gets the current active strategy
        /// </summary>
        public IAiModelStrategy CurrentStrategy => _currentStrategy;

        /// <summary>
        /// Gets all available model strategies
        /// </summary>
        public IEnumerable<IAiModelStrategy> AvailableStrategies => _availableStrategies.Values;

        /// <summary>
        /// Switches to a different AI model strategy
        /// </summary>
        /// <param name="strategyKey">The key identifying the strategy (e.g., "owl-alpha", "gemma-4")</param>
        public void SetStrategy(string strategyKey)
        {
            if (string.IsNullOrWhiteSpace(strategyKey))
                throw new ArgumentException("Strategy key cannot be empty", nameof(strategyKey));

            if (!_availableStrategies.ContainsKey(strategyKey))
            {
                var availableKeys = string.Join(", ", _availableStrategies.Keys);
                throw new ArgumentException(
                    $"Unknown strategy: {strategyKey}. Available strategies: {availableKeys}",
                    nameof(strategyKey));
            }

            _currentStrategy = _availableStrategies[strategyKey];
        }

        /// <summary>
        /// Switches strategy by model ID (e.g., "openrouter/owl-alpha")
        /// </summary>
        public void SetStrategyByModelId(string modelId)
        {
            var strategy = _availableStrategies.Values
                .FirstOrDefault(s => s.ModelId.Equals(modelId, StringComparison.OrdinalIgnoreCase));

            if (strategy == null)
            {
                throw new ArgumentException($"No strategy found for model ID: {modelId}", nameof(modelId));
            }

            _currentStrategy = strategy;
        }

        /// <summary>
        /// Generates a response using the current strategy
        /// </summary>
        public async Task<string> GenerateResponseAsync(
            string prompt,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000)
        {
            Console.WriteLine("=== [AI MODEL CONTEXT DEBUG] GenerateResponseAsync Started ===");
            Console.WriteLine($"[AI CONTEXT] Current Strategy: {_currentStrategy.ModelName}");
            Console.WriteLine($"[AI CONTEXT] Model ID: {_currentStrategy.ModelId}");
            Console.WriteLine($"[AI CONTEXT] Prompt Length: {prompt.Length} chars");
            Console.WriteLine($"[AI CONTEXT] Temperature: {temperature}");
            Console.WriteLine($"[AI CONTEXT] Max Tokens: {maxTokens}");
            Console.WriteLine($"[AI CONTEXT] System Instructions: {systemInstructions?.Substring(0, Math.Min(100, systemInstructions?.Length ?? 0)) ?? "null"}...");

            Console.WriteLine("[AI CONTEXT] Delegating to strategy...");
            var response = await _currentStrategy.GenerateResponseAsync(
                prompt,
                systemInstructions,
                temperature,
                maxTokens);

            Console.WriteLine($"[AI CONTEXT] Strategy returned response - Length: {response.Length} chars");
            Console.WriteLine("=== [AI MODEL CONTEXT DEBUG] GenerateResponseAsync Completed ===\n");

            return response;
        }

        /// <summary>
        /// Generates a streaming response using the current strategy
        /// </summary>
        public async Task<IAsyncEnumerable<string>> GenerateStreamingResponseAsync(
            string prompt,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000)
        {
            return await _currentStrategy.GenerateStreamingResponseAsync(
                prompt,
                systemInstructions,
                temperature,
                maxTokens);
        }

        /// <summary>
        /// Gets information about all available models
        /// </summary>
        public List<ModelInfo> GetAvailableModels()
        {
            return _availableStrategies.Select(kvp => new ModelInfo
            {
                Key = kvp.Key,
                ModelId = kvp.Value.ModelId,
                DisplayName = kvp.Value.ModelName
            }).ToList();
        }

        /// <summary>
        /// Model information DTO
        /// </summary>
        public class ModelInfo
        {
            public string Key { get; set; } = string.Empty;
            public string ModelId { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
        }
    }
}
