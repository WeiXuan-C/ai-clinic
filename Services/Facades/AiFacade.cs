using ai_clinic.Services.AI;

namespace ai_clinic.Services.Facades;

/// <summary>
/// FACADE PATTERN - AI Model Facade
/// Provides a unified high-level interface for the complex AI model switching system
///
/// Subsystems include:
/// - AiModelContext: Manages strategy selection and switching (Strategy Pattern Context)
/// - AiAssistantService: Provides advanced AI functionality
/// - OpenRouterApiClient: Handles external API calls (Adapter Pattern Adaptee)
///
/// Design pattern combination:
/// - Facade Pattern: Simplifies AI service usage
/// - Strategy Pattern: Dynamically switches AI models
/// - Adapter Pattern: Adapts OpenRouter API
///
/// Use cases:
/// - Simplifies client code, hides AI model switching complexity
/// - Provides one-stop AI functionality interface
/// - Coordinates interactions between multiple AI services
/// </summary>
public class AiFacade
{
    // 子系统服务
    private readonly AiAssistantService _aiAssistantService;
    private readonly AiModelContext _modelContext;
    private readonly ActivityLogService _activityLogService;
    private readonly AnonymousConsultationService _anonymousConsultationService;

    public AiFacade(
        AiAssistantService aiAssistantService,
        AiModelContext modelContext,
        ActivityLogService activityLogService,
        AnonymousConsultationService anonymousConsultationService)
    {
        _aiAssistantService = aiAssistantService;
        _modelContext = modelContext;
        _activityLogService = activityLogService;
        _anonymousConsultationService = anonymousConsultationService;
    }

    #region Model Management

    /// <summary>
    /// Gets all available AI models
    /// Simplified interface: Hides internal implementation details
    /// </summary>
    public List<AiModelInfo> GetAvailableModels()
    {
        var models = _aiAssistantService.GetAvailableModels();
        var currentModelName = _aiAssistantService.CurrentModelName;

        return models.Select(m => new AiModelInfo
        {
            Key = m.Key,
            ModelId = m.ModelId,
            DisplayName = m.DisplayName,
            IsCurrent = m.DisplayName == currentModelName,
            Description = GetModelDescription(m.Key)
        }).ToList();
    }

    /// <summary>
    /// Gets the current active AI model information
    /// </summary>
    public CurrentAiModelInfo GetCurrentModel()
    {
        var strategy = _modelContext.CurrentStrategy;
        return new CurrentAiModelInfo
        {
            ModelName = strategy.ModelName,
            ModelId = strategy.ModelId
        };
    }

    /// <summary>
    /// Switches to the specified AI model
    /// Internal coordination: Switch strategy + Log activity
    /// </summary>
    public async Task<ModelSwitchResult> SwitchModelAsync(string modelKey, Guid? userId = null)
    {
        try
        {
            var previousModel = _aiAssistantService.CurrentModelName;

            // 切换模型
            _aiAssistantService.SwitchModel(modelKey);

            var newModel = _aiAssistantService.CurrentModelName;

            // 记录活动日志
            if (userId.HasValue)
            {
                await _activityLogService.LogActivityAsync(
                    userId.Value,
                    "switch_ai_model",
                    $"{{\"previous_model\": \"{previousModel}\", \"new_model\": \"{newModel}\", \"model_key\": \"{modelKey}\"}}"
                );
            }

            return new ModelSwitchResult
            {
                Success = true,
                PreviousModel = previousModel,
                CurrentModel = newModel,
                Message = $"Successfully switched from {previousModel} to {newModel}"
            };
        }
        catch (ArgumentException ex)
        {
            return new ModelSwitchResult
            {
                Success = false,
                Message = ex.Message,
                Error = "Invalid model key"
            };
        }
    }

    /// <summary>
    /// Intelligently selects the best model based on task type
    /// Advanced feature: Automatic strategy selection
    /// </summary>
    public async Task<ModelSwitchResult> SwitchToOptimalModelAsync(AiTaskType taskType, Guid? userId = null)
    {
        var modelKey = taskType switch
        {
            AiTaskType.Reasoning => "owl-alpha",
            AiTaskType.QuickResponse => "nemotron",
            AiTaskType.DocumentAnalysis => "qianfan-ocr",
            AiTaskType.General => "gemma-4",
            _ => "gemma-4"
        };

        return await SwitchModelAsync(modelKey, userId);
    }

    #endregion

    #region AI Response Generation

    /// <summary>
    /// Generates general AI response
    /// Simplified interface: Uses current model to generate response
    /// </summary>
    public async Task<AiResponseResult> GenerateResponseAsync(
        string prompt,
        string? systemInstructions = null,
        double temperature = 0.7,
        int maxTokens = 1000,
        Guid? userId = null)
    {
        try
        {
            var modelName = _aiAssistantService.CurrentModelName;

            var response = await _aiAssistantService.GenerateResponseAsync(
                prompt,
                systemInstructions,
                temperature,
                maxTokens
            );

            // 记录活动日志
            if (userId.HasValue)
            {
                await _activityLogService.LogActivityAsync(
                    userId.Value,
                    "ai_generate_response",
                    $"{{\"model\": \"{modelName}\", \"prompt_length\": {prompt.Length}, \"response_length\": {response.Length}}}"
                );
            }

            return new AiResponseResult
            {
                Success = true,
                Response = response,
                ModelUsed = modelName
            };
        }
        catch (Exception ex)
        {
            return new AiResponseResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Generates medical consultation response
    /// Internal coordination: Select appropriate model + Generate response + Log activity
    /// </summary>
    public async Task<AiResponseResult> GenerateMedicalConsultationAsync(
        string patientQuery,
        string? medicalContext = null,
        Guid? patientId = null,
        double temperature = 0.7)
    {
        try
        {
            // 确保使用推理模型以获得更好的医疗建议
            var currentModel = _aiAssistantService.CurrentModelName;
            var shouldSwitchBack = !currentModel.Contains("Owl Alpha");

            if (shouldSwitchBack)
            {
                _aiAssistantService.SwitchModel("owl-alpha");
            }

            var response = await _aiAssistantService.GenerateMedicalResponseAsync(
                patientQuery,
                medicalContext,
                temperature
            );

            // 记录活动日志
            if (patientId.HasValue)
            {
                await _activityLogService.LogActivityAsync(
                    patientId.Value,
                    "ai_medical_consultation",
                    $"{{\"model\": \"{_aiAssistantService.CurrentModelName}\", \"query_length\": {patientQuery.Length}}}"
                );
            }

            // 切换回原来的模型
            if (shouldSwitchBack)
            {
                _aiAssistantService.SwitchModel("gemma-4");
            }

            return new AiResponseResult
            {
                Success = true,
                Response = response,
                ModelUsed = "Owl Alpha (Reasoning)"
            };
        }
        catch (Exception ex)
        {
            return new AiResponseResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Generates doctor consultation note
    /// Internal coordination: Use low temperature parameter + Generate structured note + Log activity
    /// </summary>
    public async Task<AiResponseResult> GenerateConsultationNoteAsync(
        string conversationSummary,
        string symptoms,
        string? diagnosis = null,
        Guid? doctorId = null)
    {
        try
        {
            var note = await _aiAssistantService.GenerateConsultationNoteAsync(
                conversationSummary,
                symptoms,
                diagnosis
            );

            // 记录活动日志
            if (doctorId.HasValue)
            {
                await _activityLogService.LogActivityAsync(
                    doctorId.Value,
                    "ai_generate_consultation_note",
                    $"{{\"model\": \"{_aiAssistantService.CurrentModelName}\", \"has_diagnosis\": {diagnosis != null}}}"
                );
            }

            return new AiResponseResult
            {
                Success = true,
                Response = note,
                ModelUsed = _aiAssistantService.CurrentModelName
            };
        }
        catch (Exception ex)
        {
            return new AiResponseResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    /// <summary>
    /// Analyzes medical document
    /// Internal coordination: Switch to OCR model + Analyze document + Switch back to original model + Log activity
    /// </summary>
    public async Task<AiResponseResult> AnalyzeMedicalDocumentAsync(
        string documentText,
        Guid? userId = null)
    {
        try
        {
            // 保存当前模型
            var currentModel = _aiAssistantService.CurrentModelName;
            var shouldSwitchBack = !currentModel.Contains("Qianfan");

            // 切换到OCR模型
            if (shouldSwitchBack)
            {
                _aiAssistantService.SwitchModel("qianfan-ocr");
            }

            var analysis = await _aiAssistantService.AnalyzeMedicalDocumentAsync(documentText);

            // 记录活动日志
            if (userId.HasValue)
            {
                await _activityLogService.LogActivityAsync(
                    userId.Value,
                    "ai_analyze_document",
                    $"{{\"model\": \"{_aiAssistantService.CurrentModelName}\", \"document_length\": {documentText.Length}}}"
                );
            }

            // 切换回原来的模型
            if (shouldSwitchBack)
            {
                _aiAssistantService.SwitchModel("gemma-4");
            }

            return new AiResponseResult
            {
                Success = true,
                Response = analysis,
                ModelUsed = "Baidu Qianfan OCR (Free)"
            };
        }
        catch (Exception ex)
        {
            return new AiResponseResult
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    #endregion

    #region Batch Processing

    /// <summary>
    /// Processes multiple AI tasks in batch
    /// Advanced feature: Selects best model for each task and processes in parallel
    /// </summary>
    public async Task<BatchAiResult> ProcessBatchTasksAsync(
        List<AiTask> tasks,
        Guid? userId = null)
    {
        var results = new List<AiTaskResult>();

        foreach (var task in tasks)
        {
            // 为每个任务切换到最合适的模型
            await SwitchToOptimalModelAsync(task.TaskType, userId);

            try
            {
                var response = await _aiAssistantService.GenerateResponseAsync(
                    task.Prompt,
                    task.SystemInstructions,
                    task.Temperature,
                    task.MaxTokens
                );

                results.Add(new AiTaskResult
                {
                    TaskId = task.Id,
                    Success = true,
                    Response = response,
                    ModelUsed = _aiAssistantService.CurrentModelName
                });
            }
            catch (Exception ex)
            {
                results.Add(new AiTaskResult
                {
                    TaskId = task.Id,
                    Success = false,
                    Error = ex.Message
                });
            }
        }

        // 记录批量处理日志
        if (userId.HasValue)
        {
            await _activityLogService.LogActivityAsync(
                userId.Value,
                "ai_batch_processing",
                $"{{\"total_tasks\": {tasks.Count}, \"successful\": {results.Count(r => r.Success)}, \"failed\": {results.Count(r => !r.Success)}}}"
            );
        }

        return new BatchAiResult
        {
            TotalTasks = tasks.Count,
            SuccessfulTasks = results.Count(r => r.Success),
            FailedTasks = results.Count(r => !r.Success),
            Results = results
        };
    }

    #endregion

    #region Anonymous Consultation

    /// <summary>
    /// Gets remaining queries for anonymous user
    /// Facade method to expose anonymous consultation limits
    /// </summary>
    public int GetAnonymousRemainingQueries(string sessionId)
    {
        return _anonymousConsultationService.GetRemainingQueries(sessionId);
    }

    /// <summary>
    /// Sends anonymous query to AI
    /// Facade method to handle anonymous user consultations
    /// </summary>
    public async Task<AnonymousQueryResult> SendAnonymousQueryAsync(string sessionId, string message)
    {
        return await _anonymousConsultationService.SendQueryAsync(sessionId, message);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Gets model description
    /// </summary>
    private static string GetModelDescription(string modelKey)
    {
        return modelKey switch
        {
            "owl-alpha" => "High-performance reasoning model, suitable for complex medical consultations and diagnostic recommendations",
            "nemotron" => "Lightweight fast response model, suitable for simple queries and quick replies",
            "qianfan-ocr" => "OCR specialized model, suitable for medical document recognition and analysis",
            "gemma-4" => "General powerful model, suitable for various medical scenarios",
            _ => "AI model"
        };
    }

    #endregion
}

#region DTOs (Data Transfer Objects)

/// <summary>
/// AI model information
/// </summary>
public class AiModelInfo
{
    public string Key { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsCurrent { get; set; }
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Current AI model information
/// </summary>
public class CurrentAiModelInfo
{
    public string ModelName { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
}

/// <summary>
/// Model switch result
/// </summary>
public class ModelSwitchResult
{
    public bool Success { get; set; }
    public string? PreviousModel { get; set; }
    public string? CurrentModel { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
}

/// <summary>
/// AI response result
/// </summary>
public class AiResponseResult
{
    public bool Success { get; set; }
    public string? Response { get; set; }
    public string? ModelUsed { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// AI task type
/// </summary>
public enum AiTaskType
{
    General,
    Reasoning,
    QuickResponse,
    DocumentAnalysis
}

/// <summary>
/// AI task
/// </summary>
public class AiTask
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Prompt { get; set; } = string.Empty;
    public string? SystemInstructions { get; set; }
    public AiTaskType TaskType { get; set; } = AiTaskType.General;
    public double Temperature { get; set; } = 0.7;
    public int MaxTokens { get; set; } = 1000;
}

/// <summary>
/// AI task result
/// </summary>
public class AiTaskResult
{
    public string TaskId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Response { get; set; }
    public string? ModelUsed { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Batch AI processing result
/// </summary>
public class BatchAiResult
{
    public int TotalTasks { get; set; }
    public int SuccessfulTasks { get; set; }
    public int FailedTasks { get; set; }
    public List<AiTaskResult> Results { get; set; } = new();
}

#endregion
