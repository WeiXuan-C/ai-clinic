namespace ai_clinic.Services.AI.Strategies
{
    /// <summary>
    /// Concrete Strategy - Baidu Qianfan OCR Model
    /// 具体策略 - 百度千帆OCR模型
    ///
    /// Specialized for optical character recognition and document processing
    /// 专门用于光学字符识别和文档处理
    /// </summary>
    public class QianfanOcrStrategy : BaseAiModelAdapter
    {
        public override string ModelId => "baidu/qianfan-ocr-fast:free";
        public override string ModelName => "Baidu Qianfan OCR (Free)";
        public override bool SupportsVision => true; // OCR model supports image input

        public QianfanOcrStrategy(OpenRouterApiClient apiClient) : base(apiClient)
        {
        }

        /// <summary>
        /// OCR models work best with structured prompts about text extraction
        /// OCR模型在结构化的文本提取提示下效果最佳
        /// </summary>
        protected override string PreprocessPrompt(string prompt)
        {
            // Add OCR-specific context if the prompt doesn't already mention it
            if (!prompt.Contains("ocr", StringComparison.OrdinalIgnoreCase) &&
                !prompt.Contains("extract", StringComparison.OrdinalIgnoreCase) &&
                !prompt.Contains("read", StringComparison.OrdinalIgnoreCase))
            {
                return $"[OCR Task] {prompt}";
            }
            return base.PreprocessPrompt(prompt);
        }
    }
}
