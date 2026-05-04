namespace ai_clinic.Services.AI.Strategies
{
    /// <summary>
    /// Concrete Strategy - MiniMax M2.5 Model
    /// 具体策略 - MiniMax M2.5 模型
    /// 
    /// MiniMax's latest model with strong multilingual and reasoning capabilities
    /// MiniMax的最新模型,具有强大的多语言和推理能力
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
        /// MiniMax擅长多语言任务和对话AI
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
    }
}
