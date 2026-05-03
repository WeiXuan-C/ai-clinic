using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ai_clinic.Services.AI.Strategies;

namespace ai_clinic.Services.AI
{
    /// <summary>
    /// Context - Manages AI model strategy selection and execution
    /// 上下文 - 管理AI模型策略选择和执行
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
                ["nemotron"] = new NemotronStrategy(_apiClient),
                ["qianfan-ocr"] = new QianfanOcrStrategy(_apiClient),
                ["gemma-4"] = new Gemma4Strategy(_apiClient)
            };

            // Set default strategy
            _currentStrategy = _availableStrategies["owl-alpha"];
        }

        /// <summary>
        /// Gets the current active strategy
        /// 获取当前活动策略
        /// </summary>
        public IAiModelStrategy CurrentStrategy => _currentStrategy;

        /// <summary>
        /// Gets all available model strategies
        /// 获取所有可用的模型策略
        /// </summary>
        public IEnumerable<IAiModelStrategy> AvailableStrategies => _availableStrategies.Values;

        /// <summary>
        /// Switches to a different AI model strategy
        /// 切换到不同的AI模型策略
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
        /// 通过模型ID切换策略
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
        /// 使用当前策略生成响应
        /// </summary>
        public async Task<string> GenerateResponseAsync(
            string prompt,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000)
        {
            return await _currentStrategy.GenerateResponseAsync(
                prompt, 
                systemInstructions, 
                temperature, 
                maxTokens);
        }

        /// <summary>
        /// Generates a streaming response using the current strategy
        /// 使用当前策略生成流式响应
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
        /// 获取所有可用模型的信息
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
        /// 模型信息数据传输对象
        /// </summary>
        public class ModelInfo
        {
            public string Key { get; set; } = string.Empty;
            public string ModelId { get; set; } = string.Empty;
            public string DisplayName { get; set; } = string.Empty;
        }
    }
}
