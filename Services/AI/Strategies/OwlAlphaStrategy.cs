namespace ai_clinic.Services.AI.Strategies
{
    /// <summary>
    /// Concrete Strategy - OpenRouter Owl Alpha Model
    /// 具体策略 - OpenRouter Owl Alpha 模型
    /// 
    /// This is a high-performance reasoning model optimized for complex tasks
    /// 这是一个针对复杂任务优化的高性能推理模型
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
        /// Owl Alpha擅长推理任务,因此我们可以添加特定的预处理
        /// </summary>
        protected override string PreprocessPrompt(string prompt)
        {
            // Add reasoning-specific context if needed
            return base.PreprocessPrompt(prompt);
        }
    }
}
