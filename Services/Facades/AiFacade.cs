using ai_clinic.Services.AI;

namespace ai_clinic.Services.Facades;

/// <summary>
/// 🎭 FACADE PATTERN - AI模型外观类
/// 为复杂的AI模型切换系统提供统一的高层接口
///
/// 子系统包括:
/// - AiModelContext: 管理策略选择和切换 (Strategy Pattern Context)
/// - AiAssistantService: 提供高级AI功能
/// - OpenRouterApiClient: 处理外部API调用 (Adapter Pattern Adaptee)
///
/// 设计模式组合:
/// - Facade Pattern: 简化AI服务的使用
/// - Strategy Pattern: 动态切换AI模型
/// - Adapter Pattern: 适配OpenRouter API
///
/// 使用场景:
/// - 简化客户端代码，隐藏AI模型切换的复杂性
/// - 提供一站式的AI功能接口
/// - 协调多个AI服务之间的交互
/// </summary>
public class AiFacade
{
    // 子系统服务
    private readonly AiAssistantService _aiAssistantService;
    private readonly AiModelContext _modelContext;
    private readonly ActivityLogService _activityLogService;

    public AiFacade(
        AiAssistantService aiAssistantService,
        AiModelContext modelContext,
        ActivityLogService activityLogService)
    {
        _aiAssistantService = aiAssistantService;
        _modelContext = modelContext;
        _activityLogService = activityLogService;
    }

    #region 模型管理 (Model Management)

    /// <summary>
    /// 获取所有可用的AI模型
    /// 简化接口: 隐藏内部实现细节
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
    /// 获取当前活动的AI模型信息
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
    /// 切换到指定的AI模型
    /// 内部协调: 切换策略 + 记录日志
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
    /// 根据任务类型智能选择最佳模型
    /// 高级功能: 自动策略选择
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

    #region AI响应生成 (AI Response Generation)

    /// <summary>
    /// 生成通用AI响应
    /// 简化接口: 使用当前模型生成响应
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
    /// 生成医疗咨询响应
    /// 内部协调: 选择合适的模型 + 生成响应 + 记录日志
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
    /// 生成医生咨询笔记
    /// 内部协调: 使用低温度参数 + 生成结构化笔记 + 记录日志
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
    /// 分析医疗文档
    /// 内部协调: 切换到OCR模型 + 分析文档 + 切换回原模型 + 记录日志
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

    #region 批量处理 (Batch Processing)

    /// <summary>
    /// 批量处理多个AI任务
    /// 高级功能: 为每个任务选择最佳模型并并行处理
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

    #region 私有辅助方法 (Private Helper Methods)

    /// <summary>
    /// 获取模型描述
    /// </summary>
    private static string GetModelDescription(string modelKey)
    {
        return modelKey switch
        {
            "owl-alpha" => "高性能推理模型，适合复杂医疗咨询和诊断建议",
            "nemotron" => "轻量级快速响应模型，适合简单查询和快速回复",
            "qianfan-ocr" => "OCR专用模型，适合医疗文档识别和分析",
            "gemma-4" => "通用强大模型，适合各种医疗场景",
            _ => "AI模型"
        };
    }

    #endregion
}

#region DTOs (Data Transfer Objects)

/// <summary>
/// AI模型信息
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
/// 当前AI模型信息
/// </summary>
public class CurrentAiModelInfo
{
    public string ModelName { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
}

/// <summary>
/// 模型切换结果
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
/// AI响应结果
/// </summary>
public class AiResponseResult
{
    public bool Success { get; set; }
    public string? Response { get; set; }
    public string? ModelUsed { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// AI任务类型
/// </summary>
public enum AiTaskType
{
    General,
    Reasoning,
    QuickResponse,
    DocumentAnalysis
}

/// <summary>
/// AI任务
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
/// AI任务结果
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
/// 批量AI处理结果
/// </summary>
public class BatchAiResult
{
    public int TotalTasks { get; set; }
    public int SuccessfulTasks { get; set; }
    public int FailedTasks { get; set; }
    public List<AiTaskResult> Results { get; set; } = new();
}

#endregion
