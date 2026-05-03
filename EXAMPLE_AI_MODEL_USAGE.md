# AI Model Usage Examples
# AI模型使用示例

## 通过 AiFacade 使用 (推荐方式)

AiFacade 提供了一个统一的高层接口，简化了AI模型的使用。

### 基础示例

```csharp
public class MyService
{
    private readonly AiFacade _aiFacade;

    public MyService(AiFacade aiFacade)
    {
        _aiFacade = aiFacade;
    }

    public async Task<string> GetAiResponse(string prompt)
    {
        var result = await _aiFacade.GenerateResponseAsync(prompt);
        return result.Success ? result.Response! : $"Error: {result.Error}";
    }
}
```

## C# 代码示例 (C# Code Examples)

### 示例 1: 在服务中使用 AiFacade (Using AiFacade in a Service)

```csharp
public class ConsultationService
{
    private readonly AiFacade _aiFacade;

    public ConsultationService(AiFacade aiFacade)
    {
        _aiFacade = aiFacade;
    }

    public async Task<string> GetAiSuggestionAsync(
        int consultationId,
        string patientQuery,
        Guid patientId)
    {
        // 获取咨询上下文
        var consultation = await GetConsultationAsync(consultationId);

        // 使用 Facade 生成医疗咨询响应
        // Facade 会自动选择推理模型并记录日志
        var result = await _aiFacade.GenerateMedicalConsultationAsync(
            patientQuery,
            medicalContext: consultation.MedicalHistory,
            patientId: patientId,
            temperature: 0.6
        );

        return result.Success ? result.Response! : "Unable to generate response";
    }
}
```

### 示例 2: 在Blazor组件中使用 (Using in Blazor Component)

```csharp
@page "/ai-chat"
@inject AiFacade AiFacade

<h3>AI Medical Assistant</h3>

<div>
    <label>Select Model:</label>
    <select @onchange="OnModelChanged">
        @foreach (var model in availableModels)
        {
            <option value="@model.Key" selected="@model.IsCurrent">
                @model.DisplayName
            </option>
        }
    </select>
    <p><small>@selectedModelDescription</small></p>
</div>

<div>
    <textarea @bind="userInput" placeholder="Ask a question..."></textarea>
    <button @onclick="GenerateResponse">Send</button>
</div>

<div>
    <p><strong>Response:</strong></p>
    <p>@aiResponse</p>
    <p><small>Model used: @modelUsed</small></p>
</div>

@code {
    private List<AiModelInfo> availableModels = new();
    private string userInput = "";
    private string aiResponse = "";
    private string modelUsed = "";
    private string selectedModelDescription = "";

    protected override void OnInitialized()
    {
        availableModels = AiFacade.GetAvailableModels();
        var current = availableModels.FirstOrDefault(m => m.IsCurrent);
        if (current != null)
        {
            selectedModelDescription = current.Description;
        }
    }

    private async Task OnModelChanged(ChangeEventArgs e)
    {
        var modelKey = e.Value?.ToString();
        if (!string.IsNullOrEmpty(modelKey))
        {
            var result = await AiFacade.SwitchModelAsync(modelKey);
            if (result.Success)
            {
                var model = availableModels.FirstOrDefault(m => m.Key == modelKey);
                selectedModelDescription = model?.Description ?? "";
            }
        }
    }

    private async Task GenerateResponse()
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return;

        aiResponse = "Generating...";
        modelUsed = "";

        var result = await AiFacade.GenerateResponseAsync(userInput);

        if (result.Success)
        {
            aiResponse = result.Response!;
            modelUsed = result.ModelUsed!;
        }
        else
        {
            aiResponse = $"Error: {result.Error}";
        }
    }
}
```

### 示例 3: 智能模型选择策略 (Smart Model Selection Strategy)

```csharp
public class SmartAiService
{
    private readonly AiFacade _aiFacade;

    public SmartAiService(AiFacade aiFacade)
    {
        _aiFacade = aiFacade;
    }

    /// <summary>
    /// 根据任务类型自动选择最佳模型
    /// Automatically select the best model based on task type
    /// </summary>
    public async Task<string> GenerateSmartResponseAsync(
        string prompt,
        AiTaskType taskType,
        Guid userId)
    {
        // Facade 自动选择最佳模型
        await _aiFacade.SwitchToOptimalModelAsync(taskType, userId);

        var result = await _aiFacade.GenerateResponseAsync(prompt, userId: userId);
        return result.Success ? result.Response! : result.Error!;
    }
}
```
```

### 示例 4: 批量处理不同任务 (Batch Processing Different Tasks)

```csharp
public class BatchAiProcessor
{
    private readonly AiFacade _aiFacade;

    public BatchAiProcessor(AiFacade aiFacade)
    {
        _aiFacade = aiFacade;
    }

    public async Task<BatchAiResult> ProcessBatchAsync(
        List<AiTask> tasks,
        Guid userId)
    {
        // 使用 Facade 的批量处理功能
        // Facade 会自动为每个任务选择最佳模型并记录日志
        return await _aiFacade.ProcessBatchTasksAsync(tasks, userId);
    }

    // 使用示例
    public async Task<string> ProcessMultipleQueriesAsync(Guid userId)
    {
        var tasks = new List<AiTask>
        {
            new AiTask
            {
                Prompt = "Analyze this complex medical case...",
                TaskType = AiTaskType.Reasoning,
                Temperature = 0.6
            },
            new AiTask
            {
                Prompt = "Quick question: What is aspirin?",
                TaskType = AiTaskType.QuickResponse,
                Temperature = 0.7
            },
            new AiTask
            {
                Prompt = "Extract information from this document...",
                TaskType = AiTaskType.DocumentAnalysis,
                Temperature = 0.3
            }
        };

        var result = await ProcessBatchAsync(tasks, userId);

        return $"Processed {result.TotalTasks} tasks: " +
               $"{result.SuccessfulTasks} successful, {result.FailedTasks} failed";
    }
}
```

## 测试场景 (Testing Scenarios)

### 场景 1: 医疗咨询对话

```csharp
// 使用 Facade 简化医疗咨询流程
var result = await _aiFacade.GenerateMedicalConsultationAsync(
    "I have chest pain when exercising",
    "Patient: 45 years old, history of high blood pressure",
    patientId: patientGuid
);

// Facade 自动使用推理模型并记录日志
if (result.Success)
{
    Console.WriteLine($"AI Response: {result.Response}");
    Console.WriteLine($"Model Used: {result.ModelUsed}");
}
```

### 场景 2: 文档处理流程

```csharp
// Facade 自动处理模型切换
// 步骤 1: 使用OCR模型提取文本
var extractResult = await _aiFacade.AnalyzeMedicalDocumentAsync(
    scannedDocumentText,
    userId: userGuid
);

// 步骤 2: 使用通用模型进行分析
await _aiFacade.SwitchModelAsync("gemma-4", userGuid);
var analysisResult = await _aiFacade.GenerateResponseAsync(
    $"Summarize the key medical information: {extractResult.Response}",
    userId: userGuid
);

// 步骤 3: 使用推理模型生成建议
await _aiFacade.SwitchToOptimalModelAsync(AiTaskType.Reasoning, userGuid);
var recommendResult = await _aiFacade.GenerateResponseAsync(
    $"Based on this analysis, what are the recommended next steps? {analysisResult.Response}",
    userId: userGuid
);
```

### 场景 3: 性能对比测试

```csharp
public async Task<ModelComparisonResult> CompareModelsAsync(string prompt, Guid userId)
{
    var results = new Dictionary<string, ModelPerformance>();
    var models = new[] { "owl-alpha", "nemotron", "gemma-4" };

    foreach (var model in models)
    {
        await _aiFacade.SwitchModelAsync(model, userId);

        var stopwatch = Stopwatch.StartNew();
        var result = await _aiFacade.GenerateResponseAsync(prompt, userId: userId);
        stopwatch.Stop();

        if (result.Success)
        {
            results[model] = new ModelPerformance
            {
                ModelName = result.ModelUsed!,
                Response = result.Response!,
                ResponseTime = stopwatch.ElapsedMilliseconds,
                ResponseLength = result.Response!.Length
            };
        }
    }

    return new ModelComparisonResult { Results = results };
}
```

## 最佳实践提示 (Best Practice Tips)

### 1. 模型选择决策树

```
任务类型?
├─ 需要深度推理? → owl-alpha
├─ 需要快速响应? → nemotron
├─ 处理文档/OCR? → qianfan-ocr
└─ 通用任务? → gemma-4
```

### 2. Temperature 设置指南

```csharp
// 事实性任务 (医疗记录、诊断)
temperature: 0.3

// 平衡任务 (咨询、建议)
temperature: 0.7

// 创造性任务 (教育内容、解释)
temperature: 1.0
```

### 3. 错误处理模板

```csharp
try
{
    var result = await _aiFacade.SwitchModelAsync(modelKey, userId);

    if (!result.Success)
    {
        _logger.LogWarning("Failed to switch model: {Error}", result.Error);
        // 使用默认模型
        await _aiFacade.SwitchModelAsync("gemma-4", userId);
    }

    var response = await _aiFacade.GenerateResponseAsync(prompt, userId: userId);

    if (response.Success)
    {
        return response.Response!;
    }
    else
    {
        _logger.LogError("AI generation failed: {Error}", response.Error);
        throw new ServiceException("AI service temporarily unavailable");
    }
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error in AI service");
    throw;
}
```

## 监控和日志 (Monitoring and Logging)

```csharp
public class AiUsageLogger
{
    private readonly ILogger<AiUsageLogger> _logger;

    public void LogModelUsage(
        string modelName, 
        string taskType, 
        long responseTime,
        int tokenCount)
    {
        _logger.LogInformation(
            "AI Model Usage - Model: {Model}, Task: {Task}, " +
            "ResponseTime: {Time}ms, Tokens: {Tokens}",
            modelName, taskType, responseTime, tokenCount
        );
    }
}
```

---

**提示**: 这些示例展示了如何在实际应用中使用策略模式和适配器模式来实现灵活的AI模型切换。根据您的具体需求调整参数和逻辑。
