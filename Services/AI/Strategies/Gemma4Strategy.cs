namespace ai_clinic.Services.AI.Strategies
{
    /// <summary>
    /// Concrete Strategy - Google Gemma 4 26B Model
    /// 具体策略 - Google Gemma 4 26B 模型
    /// 
    /// A powerful open-source model from Google with strong general capabilities
    /// Google的强大开源模型,具有强大的通用能力
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
        /// Gemma 4经过指令调优,因此在清晰的指令下效果很好
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
    }
}
