using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ai_clinic.Services.AI;

namespace ai_clinic.Services
{
    /// <summary>
    /// High-level AI Assistant Service
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
        /// </summary>
        public string CurrentModelName => _modelContext.CurrentStrategy.ModelName;

        /// <summary>
        /// Gets all available models
        /// </summary>
        public List<AiModelContext.ModelInfo> GetAvailableModels()
        {
            return _modelContext.GetAvailableModels();
        }

        /// <summary>
        /// Switches to a different AI model
        /// </summary>
        /// <param name="modelKey">Model key (e.g., "owl-alpha", "gemma-4")</param>
        public void SwitchModel(string modelKey)
        {
            _modelContext.SetStrategy(modelKey);
        }

        /// <summary>
        /// Generates a medical consultation response
        /// </summary>
        public async Task<string> GenerateMedicalResponseAsync(
            string patientQuery,
            string? medicalContext = null,
            double temperature = 0.7)
        {
            Console.WriteLine("=== [AI ASSISTANT SERVICE DEBUG] GenerateMedicalResponseAsync Started ===");
            Console.WriteLine($"[AI ASSISTANT] Current Model: {CurrentModelName}");
            Console.WriteLine($"[AI ASSISTANT] Patient Query: {patientQuery}");
            Console.WriteLine($"[AI ASSISTANT] Medical Context: {medicalContext ?? "null"}");
            Console.WriteLine($"[AI ASSISTANT] Temperature: {temperature}");

            var systemInstructions = @"You are a helpful medical AI assistant. 
Provide informative and empathetic responses to patient queries. 
Always remind users to consult with healthcare professionals for serious concerns.
Do not provide definitive diagnoses.";

            var prompt = string.IsNullOrWhiteSpace(medicalContext)
                ? patientQuery
                : $"Medical Context: {medicalContext}\n\nPatient Query: {patientQuery}";

            Console.WriteLine("[AI ASSISTANT] Calling AiModelContext.GenerateResponseAsync...");
            var response = await _modelContext.GenerateResponseAsync(
                prompt,
                systemInstructions,
                temperature,
                maxTokens: 1500);

            Console.WriteLine($"[AI ASSISTANT] Response received - Length: {response.Length} chars");
            Console.WriteLine($"[AI ASSISTANT] Response preview: {response.Substring(0, Math.Min(100, response.Length))}...");
            Console.WriteLine("=== [AI ASSISTANT SERVICE DEBUG] GenerateMedicalResponseAsync Completed ===\n");

            return response;
        }

        /// <summary>
        /// Generates a doctor's consultation note
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

        /// <summary>
        /// Generates a streaming medical consultation response
        /// 生成流式医疗咨询响应
        /// </summary>
        public async IAsyncEnumerable<string> GenerateStreamingMedicalResponseAsync(
            string patientQuery,
            string? medicalContext = null,
            double temperature = 0.7)
        {
            Console.WriteLine("=== [AI ASSISTANT SERVICE] GenerateStreamingMedicalResponseAsync Started ===");
            Console.WriteLine($"[AI ASSISTANT] Current Model: {CurrentModelName}");

            var systemInstructions = @"You are a helpful medical AI assistant. 
Provide informative and empathetic responses to patient queries. 
Always remind users to consult with healthcare professionals for serious concerns.
Do not provide definitive diagnoses.
Format your response using Markdown for better readability.";

            var prompt = string.IsNullOrWhiteSpace(medicalContext)
                ? patientQuery
                : $"Medical Context: {medicalContext}\n\nPatient Query: {patientQuery}";

            Console.WriteLine("[AI ASSISTANT] Calling AiModelContext.GenerateStreamingResponseAsync...");
            
            var streamingResponse = await _modelContext.GenerateStreamingResponseAsync(
                prompt,
                systemInstructions,
                temperature,
                maxTokens: 1500);

            await foreach (var chunk in streamingResponse)
            {
                yield return chunk;
            }

            Console.WriteLine("=== [AI ASSISTANT SERVICE] GenerateStreamingMedicalResponseAsync Completed ===\n");
        }
    }
}
