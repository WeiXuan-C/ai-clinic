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
    }
}
