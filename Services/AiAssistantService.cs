using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ai_clinic.Services.AI;

namespace ai_clinic.Services
{
    /// <summary>
    /// High-level AI Assistant Service
    /// 高级AI助手服务
    /// 
    /// This service provides a clean interface for the application to use AI features
    /// without needing to know about the Strategy/Adapter pattern implementation
    /// </summary>
    public class AiAssistantService
    {
        private readonly AiModelContext _modelContext;

        public AiAssistantService(AiModelContext modelContext)
        {
            _modelContext = modelContext ?? throw new ArgumentNullException(nameof(modelContext));
        }

        /// <summary>
        /// Gets the current model name
        /// 获取当前模型名称
        /// </summary>
        public string CurrentModelName => _modelContext.CurrentStrategy.ModelName;

        /// <summary>
        /// Gets all available models
        /// 获取所有可用模型
        /// </summary>
        public List<AiModelContext.ModelInfo> GetAvailableModels()
        {
            return _modelContext.GetAvailableModels();
        }

        /// <summary>
        /// Switches to a different AI model
        /// 切换到不同的AI模型
        /// </summary>
        /// <param name="modelKey">Model key (e.g., "owl-alpha", "gemma-4")</param>
        public void SwitchModel(string modelKey)
        {
            _modelContext.SetStrategy(modelKey);
        }

        /// <summary>
        /// Generates a medical consultation response
        /// 生成医疗咨询响应
        /// </summary>
        public async Task<string> GenerateMedicalResponseAsync(
            string patientQuery,
            string? medicalContext = null,
            double temperature = 0.7)
        {
            var systemInstructions = @"You are a helpful medical AI assistant. 
Provide informative and empathetic responses to patient queries. 
Always remind users to consult with healthcare professionals for serious concerns.
Do not provide definitive diagnoses.";

            var prompt = string.IsNullOrWhiteSpace(medicalContext)
                ? patientQuery
                : $"Medical Context: {medicalContext}\n\nPatient Query: {patientQuery}";

            return await _modelContext.GenerateResponseAsync(
                prompt,
                systemInstructions,
                temperature,
                maxTokens: 1500);
        }

        /// <summary>
        /// Generates a doctor's consultation note
        /// 生成医生的咨询笔记
        /// </summary>
        public async Task<string> GenerateConsultationNoteAsync(
            string conversationSummary,
            string symptoms,
            string? diagnosis = null)
        {
            var systemInstructions = @"You are a medical documentation assistant. 
Generate professional, concise consultation notes based on the provided information.
Use medical terminology appropriately and maintain a professional tone.";

            var prompt = $@"Generate a consultation note based on:

Conversation Summary: {conversationSummary}
Symptoms: {symptoms}
{(diagnosis != null ? $"Diagnosis: {diagnosis}" : "")}

Format the note professionally with sections for Chief Complaint, History, Assessment, and Plan.";

            return await _modelContext.GenerateResponseAsync(
                prompt,
                systemInstructions,
                temperature: 0.5, // Lower temperature for more consistent medical documentation
                maxTokens: 2000);
        }

        /// <summary>
        /// Analyzes medical document text (useful with OCR models)
        /// 分析医疗文档文本(对OCR模型有用)
        /// </summary>
        public async Task<string> AnalyzeMedicalDocumentAsync(string documentText)
        {
            var systemInstructions = @"You are a medical document analysis assistant.
Extract and summarize key medical information from the provided document text.
Identify important dates, medications, diagnoses, and test results.";

            return await _modelContext.GenerateResponseAsync(
                documentText,
                systemInstructions,
                temperature: 0.3, // Very low temperature for factual extraction
                maxTokens: 1500);
        }

        /// <summary>
        /// Generates a general AI response
        /// 生成通用AI响应
        /// </summary>
        public async Task<string> GenerateResponseAsync(
            string prompt,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000)
        {
            return await _modelContext.GenerateResponseAsync(
                prompt,
                systemInstructions,
                temperature,
                maxTokens);
        }

        /// <summary>
        /// Generates a streaming response
        /// 生成流式响应
        /// </summary>
        public async Task<IAsyncEnumerable<string>> GenerateStreamingResponseAsync(
            string prompt,
            string? systemInstructions = null,
            double temperature = 0.7,
            int maxTokens = 1000)
        {
            return await _modelContext.GenerateStreamingResponseAsync(
                prompt,
                systemInstructions,
                temperature,
                maxTokens);
        }
    }
}
