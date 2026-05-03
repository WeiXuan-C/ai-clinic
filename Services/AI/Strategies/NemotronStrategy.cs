namespace ai_clinic.Services.AI.Strategies
{
    /// <summary>
    /// Concrete Strategy - NVIDIA Nemotron 3 Nano Omni Model
    /// 具体策略 - NVIDIA Nemotron 3 Nano Omni 模型
    /// 
    /// A free, lightweight multimodal model with reasoning capabilities
    /// 免费的轻量级多模态推理模型
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
        /// Nemotron针对效率优化,适合快速响应
        /// </summary>
        protected override string PreprocessPrompt(string prompt)
        {
            // Optimize for concise responses
            return base.PreprocessPrompt(prompt);
        }
    }
}
