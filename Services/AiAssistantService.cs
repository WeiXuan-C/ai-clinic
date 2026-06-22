using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ai_clinic.Services.AI;
using ai_clinic.Models;

namespace ai_clinic.Services
{
    /// <summary>
    /// High-level AI Assistant Service
    /// 
    /// This service provides a clean interface for the application to use AI features
    /// without needing to know about the Strategy/Adapter pattern implementation
    /// Integrates with AiAssistantSettings for admin-controlled model selection
    /// </summary>
    public class AiAssistantService
    {
        private readonly AiModelContext _modelContext;
        private readonly AiAssistantSettingsService _settingsService;
        private readonly ILogger<AiAssistantService> _logger;

        public AiAssistantService(
            AiModelContext modelContext,
            AiAssistantSettingsService settingsService,
            ILogger<AiAssistantService> logger)
        {
            _modelContext = modelContext ?? throw new ArgumentNullException(nameof(modelContext));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets the current model name
        /// </summary>
        public string CurrentModelName => _modelContext.CurrentStrategy.ModelName;

        /// <summary>
        /// Initialize service with admin-configured model
        /// Should be called at startup or when settings change
        /// </summary>
        public async Task InitializeFromSettingsAsync()
        {
            try
            {
                var activeSetting = await _settingsService.GetActiveSettingAsync();
                if (activeSetting != null)
                {
                    _logger.LogInformation("[AI SERVICE] Initializing with admin-configured model: {ModelName}", activeSetting.ModelName);
                    
                    // Try to match the model name to available models
                    var availableModels = _modelContext.GetAvailableModels();
                    var matchingModel = availableModels.FirstOrDefault(m => 
                        m.DisplayName.Equals(activeSetting.ModelName, StringComparison.OrdinalIgnoreCase) ||
                        m.Key.Equals(activeSetting.ModelName, StringComparison.OrdinalIgnoreCase));
                    
                    if (matchingModel != null)
                    {
                        _modelContext.SetStrategy(matchingModel.Key);
                        _logger.LogInformation("[AI SERVICE] Successfully set model to: {ModelName}", matchingModel.DisplayName);
                    }
                    else
                    {
                        _logger.LogWarning("[AI SERVICE] Admin-configured model '{ModelName}' not found in available models. Using default.", activeSetting.ModelName);
                    }
                }
                else
                {
                    _logger.LogInformation("[AI SERVICE] No active admin settings found. Using default model.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI SERVICE] Failed to initialize from settings. Using default model.");
            }
        }

        /// <summary>
        /// Sync current model with admin settings
        /// Useful when admin changes the active model
        /// </summary>
        public async Task<bool> SyncWithAdminSettingsAsync()
        {
            try
            {
                await InitializeFromSettingsAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI SERVICE] Failed to sync with admin settings");
                return false;
            }
        }

        /// <summary>
        /// Gets whether a specific feature is enabled in admin settings
        /// </summary>
        public async Task<bool> IsFeatureEnabledAsync(string featureName)
        {
            try
            {
                var activeSetting = await _settingsService.GetActiveSettingAsync();
                if (activeSetting == null)
                    return true; // Default to enabled if no settings

                return featureName.ToLower() switch
                {
                    "document_analysis" or "documentanalysis" => activeSetting.EnableDocumentAnalysis,
                    "symptom_checker" or "symptomchecker" => activeSetting.EnableSymptomChecker,
                    "doctor_recommendation" or "doctorrecommendation" => activeSetting.EnableDoctorRecommendation,
                    _ => true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AI SERVICE] Failed to check feature enabled status for {FeatureName}", featureName);
                return true; // Default to enabled on error
            }
        }

        /// <summary>
        /// Gets the active admin settings
        /// </summary>
        public async Task<AiAssistantSetting?> GetActiveSettingsAsync()
        {
            return await _settingsService.GetActiveSettingAsync();
        }

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
        /// </summary>
        public async IAsyncEnumerable<string> GenerateStreamingMedicalResponseAsync(
            string patientQuery,
            string? medicalContext = null,
            double temperature = 0.7)
        {
            Console.WriteLine("=== [AI ASSISTANT SERVICE] GenerateStreamingMedicalResponseAsync Started ===");
            Console.WriteLine($"[AI ASSISTANT] Current Model: {CurrentModelName}");

            var systemInstructions = @"You are an AI-powered medical screening assistant.

Your role is to help users understand possible causes of their symptoms and guide them toward appropriate next steps.

Guidelines:

1. When a user describes symptoms, identify the most likely possible conditions based on the available information.
2. Do not immediately provide a diagnosis if the information is insufficient. Instead, ask relevant follow-up questions to gather additional details such as:

   * Age and sex
   * Duration of symptoms
   * Severity of symptoms
   * Associated symptoms
   * Medical history
   * Current medications
   * Recent illnesses, travel, injuries, or exposures
3. Continue asking targeted questions until you have enough information to make a reasonable assessment.
4. After gathering sufficient information, provide:

   * Most likely diagnosis or diagnoses (ranked by likelihood)
   * Brief explanation for each possibility
   * Recommended next steps
   * Warning signs that require urgent medical attention
5. Clearly distinguish between:

   * Likely conditions
   * Possible but less likely conditions
   * Medical emergencies that must not be ignored
6. Never claim certainty. Use phrases such as:

   * Based on the information provided...
   * The most likely explanation is...
   * Other possibilities include...
7. If symptoms suggest a potentially serious or life-threatening condition, immediately advise the user to seek emergency medical care.
8. Maintain a professional, clear, and empathetic tone.
9. Format all responses using Markdown for readability.

Important:

* You are not a substitute for a licensed healthcare professional.
* Your assessments are informational only and should not be considered a definitive medical diagnosis.
* Encourage users to consult a qualified healthcare provider for confirmation, testing, treatment, or any serious health concern.
";

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
