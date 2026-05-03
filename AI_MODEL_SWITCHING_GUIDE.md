# AI Model Switching Implementation Guide
# AI模型切换实现指南

## 概述 (Overview)

本项目实现了一个灵活的AI模型切换系统，使用了两种经典的设计模式：
- **Strategy Pattern (策略模式)**: 允许在运行时动态切换不同的AI模型
- **Adapter Pattern (适配器模式)**: 将OpenRouter API适配到统一的接口

This project implements a flexible AI model switching system using two classic design patterns:
- **Strategy Pattern**: Allows dynamic switching between different AI models at runtime
- **Adapter Pattern**: Adapts the OpenRouter API to a unified interface

## 架构设计 (Architecture Design)

```
┌─────────────────────────────────────────────────────────────┐
│                    AiAssistantService                        │
│              (High-level Application Service)                │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                      AiModelContext                          │
│                  (Strategy Pattern Context)                  │
│  - Manages strategy selection                                │
│  - Delegates work to current strategy                        │
└───────────────────────────┬─────────────────────────────────┘
                            │
                            ▼
┌─────────────────────────────────────────────────────────────┐
│                    IAiModelStrategy                          │
│                  (Strategy Interface)                        │
└───────────────────────────┬─────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        ▼                   ▼                   ▼
┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│ OwlAlpha     │   │ Nemotron     │   │ Gemma4       │
│ Strategy     │   │ Strategy     │   │ Strategy     │
└──────┬───────┘   └──────┬───────┘   └──────┬───────┘
       │                  │                  │
       └──────────────────┼──────────────────┘
                          ▼
              ┌────────────────────────┐
              │  BaseAiModelAdapter    │
              │  (Adapter Pattern)     │
              └───────────┬────────────┘
                          │
                          ▼
              ┌────────────────────────┐
              │ OpenRouterApiClient    │
              │    (Adaptee)           │
              └────────────────────────┘
```

## 设计模式详解 (Design Pattern Details)

### 1. Strategy Pattern (策略模式)

**目的**: 定义一系列算法(AI模型)，把它们封装起来，并使它们可以互相替换。

**组件**:
- **Strategy Interface** (`IAiModelStrategy`): 定义所有策略的公共接口
- **Concrete Strategies**: 
  - `OwlAlphaStrategy` - 高性能推理模型
  - `NemotronStrategy` - 轻量级多模态模型
  - `QianfanOcrStrategy` - OCR专用模型
  - `Gemma4Strategy` - 通用强大模型
- **Context** (`AiModelContext`): 维护对策略对象的引用，可以动态切换

**优势**:
- ✅ 运行时动态切换模型
- ✅ 易于添加新模型
- ✅ 符合开闭原则(对扩展开放，对修改关闭)

### 2. Adapter Pattern (适配器模式)

**目的**: 将OpenRouter API的接口转换成我们期望的统一接口。

**组件**:
- **Target** (`IAiModelStrategy`): 我们期望的接口
- **Adapter** (`BaseAiModelAdapter`): 继承Target并包装Adaptee
- **Adaptee** (`OpenRouterApiClient`): 外部OpenRouter API

**优势**:
- ✅ 隔离外部API变化
- ✅ 统一不同模型的调用方式
- ✅ 易于测试和维护

## 支持的模型 (Supported Models)

| 模型键 (Key) | 模型ID | 显示名称 | 特点 |
|-------------|--------|---------|------|
| `owl-alpha` | `openrouter/owl-alpha` | Owl Alpha (Reasoning) | 高性能推理 |
| `nemotron` | `nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free` | NVIDIA Nemotron 3 Nano | 免费、轻量级 |
| `qianfan-ocr` | `baidu/qianfan-ocr-fast:free` | Baidu Qianfan OCR | OCR专用 |
| `gemma-4` | `google/gemma-4-26b-a4b-it:free` | Google Gemma 4 26B | 通用强大 |

## 配置 (Configuration)

### 1. 添加API密钥

在 `appsettings.json` 中:
```json
{
  "OpenRouter": {
    "ApiKey": "your-openrouter-api-key-here",
    "DefaultModel": "owl-alpha"
  }
}
```

或使用环境变量 `.env`:
```bash
OPENROUTER_API_KEY=your-openrouter-api-key-here
```

### 2. 获取API密钥

访问 [OpenRouter](https://openrouter.ai/) 注册并获取API密钥。

## 使用示例 (Usage Examples)

### 基础使用 - 通过 Facade

```csharp
// 注入 AiFacade
public class MyService
{
    private readonly AiFacade _aiFacade;

    public MyService(AiFacade aiFacade)
    {
        _aiFacade = aiFacade;
    }

    // 使用默认模型生成响应
    public async Task<string> GetResponse(string prompt)
    {
        var result = await _aiFacade.GenerateResponseAsync(prompt);
        return result.Success ? result.Response! : result.Error!;
    }
}
```

### 在 Blazor 组件中使用

```csharp
@page "/ai-assistant"
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
</div>

<div>
    <textarea @bind="userInput" placeholder="Ask a question..."></textarea>
    <button @onclick="GenerateResponse">Send</button>
</div>

<div>
    <p><strong>Response:</strong></p>
    <p>@aiResponse</p>
</div>

@code {
    private List<AiModelInfo> availableModels = new();
    private string userInput = "";
    private string aiResponse = "";

    protected override void OnInitialized()
    {
        availableModels = AiFacade.GetAvailableModels();
    }

    private async Task OnModelChanged(ChangeEventArgs e)
    {
        var modelKey = e.Value?.ToString();
        if (!string.IsNullOrEmpty(modelKey))
        {
            await AiFacade.SwitchModelAsync(modelKey);
        }
    }

    private async Task GenerateResponse()
    {
        if (string.IsNullOrWhiteSpace(userInput))
            return;

        aiResponse = "Generating...";
        
        var result = await AiFacade.GenerateResponseAsync(userInput);
        aiResponse = result.Success ? result.Response! : $"Error: {result.Error}";
    }
}
```

### 切换模型

```csharp
// 切换到不同的模型
await _aiFacade.SwitchModelAsync("gemma-4");

// 生成响应(使用新模型)
var result = await _aiFacade.GenerateResponseAsync("Hello!");

// 切换到OCR模型
await _aiFacade.SwitchModelAsync("qianfan-ocr");
```

### 获取可用模型列表

```csharp
var models = _aiFacade.GetAvailableModels();
foreach (var model in models)
{
    Console.WriteLine($"{model.Key}: {model.DisplayName} - {model.Description}");
    Console.WriteLine($"  Current: {model.IsCurrent}");
}
```

### 医疗场景专用方法

```csharp
// 生成医疗咨询响应 (自动使用推理模型)
var result = await _aiFacade.GenerateMedicalConsultationAsync(
    patientQuery: "I have a headache",
    medicalContext: "Patient history: No allergies",
    patientId: patientGuid
);

// 生成咨询笔记
var noteResult = await _aiFacade.GenerateConsultationNoteAsync(
    conversationSummary: "Patient complained of headache for 3 days",
    symptoms: "Headache, mild fever",
    diagnosis: "Tension headache",
    doctorId: doctorGuid
);

// 分析医疗文档 (自动切换到OCR模型)
var analysisResult = await _aiFacade.AnalyzeMedicalDocumentAsync(
    documentText,
    userId: userGuid
);
```

### 智能模型选择

```csharp
// 根据任务类型自动选择最佳模型
await _aiFacade.SwitchToOptimalModelAsync(AiTaskType.Reasoning);
await _aiFacade.SwitchToOptimalModelAsync(AiTaskType.DocumentAnalysis);
```

## 添加新模型 (Adding New Models)

### 步骤 1: 创建新策略类

```csharp
namespace ai_clinic.Services.AI.Strategies
{
    public class NewModelStrategy : BaseAiModelAdapter
    {
        public override string ModelId => "provider/model-name";
        public override string ModelName => "Display Name";

        public NewModelStrategy(OpenRouterApiClient apiClient) 
            : base(apiClient)
        {
        }

        // 可选: 重写预处理方法
        protected override string PreprocessPrompt(string prompt)
        {
            // 添加模型特定的预处理逻辑
            return base.PreprocessPrompt(prompt);
        }
    }
}
```

### 步骤 2: 在Context中注册

在 `AiModelContext.cs` 的构造函数中添加:

```csharp
_availableStrategies = new Dictionary<string, IAiModelStrategy>
{
    // ... 现有模型
    ["new-model"] = new NewModelStrategy(_apiClient)
};
```

### 步骤 3: 使用新模型

```csharp
_aiService.SwitchModel("new-model");
```

## API参考 (API Reference)

### AiFacade (推荐使用)

| 方法 | 描述 |
|------|------|
| `GetAvailableModels()` | 获取所有可用模型列表 |
| `GetCurrentModel()` | 获取当前活动模型信息 |
| `SwitchModelAsync(modelKey, userId)` | 切换到指定模型 |
| `SwitchToOptimalModelAsync(taskType, userId)` | 智能选择最佳模型 |
| `GenerateResponseAsync(...)` | 生成通用响应 |
| `GenerateMedicalConsultationAsync(...)` | 生成医疗咨询响应 |
| `GenerateConsultationNoteAsync(...)` | 生成咨询笔记 |
| `AnalyzeMedicalDocumentAsync(...)` | 分析医疗文档 |
| `ProcessBatchTasksAsync(...)` | 批量处理AI任务 |

### AiAssistantService (底层服务)

| 方法 | 描述 |
|------|------|
| `GetAvailableModels()` | 获取所有可用模型列表 |
| `SwitchModel(string modelKey)` | 切换到指定模型 |
| `GenerateResponseAsync(...)` | 生成通用响应 |
| `GenerateMedicalResponseAsync(...)` | 生成医疗咨询响应 |
| `GenerateConsultationNoteAsync(...)` | 生成咨询笔记 |
| `AnalyzeMedicalDocumentAsync(...)` | 分析医疗文档 |

### 参数说明

- **prompt**: 用户输入的提示词
- **systemInstructions**: 系统指令，指导模型行为
- **temperature**: 控制随机性 (0.0-2.0)
  - 0.0: 确定性输出
  - 0.7: 平衡(默认)
  - 2.0: 高创造性
- **maxTokens**: 最大生成令牌数

## 最佳实践 (Best Practices)

### 1. 模型选择建议

- **推理任务**: 使用 `owl-alpha`
- **快速响应**: 使用 `nemotron`
- **文档处理**: 使用 `qianfan-ocr`
- **通用任务**: 使用 `gemma-4`

### 2. Temperature设置

- **事实性任务** (如医疗文档): `temperature = 0.3`
- **平衡任务** (如咨询): `temperature = 0.7`
- **创造性任务** (如建议): `temperature = 1.0`

### 3. 错误处理

```csharp
try
{
    var response = await _aiService.GenerateResponseAsync(prompt);
}
catch (HttpRequestException ex)
{
    // 处理网络错误
    _logger.LogError(ex, "Failed to call OpenRouter API");
}
catch (InvalidOperationException ex)
{
    // 处理配置错误
    _logger.LogError(ex, "OpenRouter not configured properly");
}
```

## 测试 (Testing)

### 单元测试示例

```csharp
[Fact]
public void SwitchModel_ValidKey_ChangesStrategy()
{
    // Arrange
    var context = new AiModelContext(mockApiClient);
    
    // Act
    context.SetStrategy("gemma-4");
    
    // Assert
    Assert.Equal("google/gemma-4-26b-a4b-it:free", 
        context.CurrentStrategy.ModelId);
}
```

## 性能考虑 (Performance Considerations)

- 所有AI服务都注册为 `Scoped` 生命周期
- OpenRouter API客户端使用 `HttpClient` 连接池
- 考虑实现响应缓存以减少API调用

## 安全性 (Security)

- ✅ API密钥存储在配置文件中，不要提交到版本控制
- ✅ 使用环境变量或Azure Key Vault存储敏感信息
- ✅ 实现速率限制以防止滥用
- ✅ 验证和清理用户输入

## 故障排除 (Troubleshooting)

### 问题: "OpenRouter API key not configured"

**解决方案**: 确保在 `appsettings.json` 或环境变量中设置了API密钥。

### 问题: "Unknown strategy: xxx"

**解决方案**: 使用 `GetAvailableModels()` 查看可用的模型键。

### 问题: API调用失败

**解决方案**: 
1. 检查API密钥是否有效
2. 检查网络连接
3. 查看OpenRouter服务状态

## 扩展建议 (Extension Suggestions)

1. **添加缓存层**: 缓存常见查询的响应
2. **实现重试逻辑**: 处理临时网络故障
3. **添加监控**: 跟踪API使用情况和成本
4. **实现流式响应**: 提供实时响应体验
5. **添加模型性能指标**: 跟踪响应时间和质量

## 参考资料 (References)

- [OpenRouter API Documentation](https://openrouter.ai/docs)
- [Strategy Pattern - Gang of Four](https://refactoring.guru/design-patterns/strategy)
- [Adapter Pattern - Gang of Four](https://refactoring.guru/design-patterns/adapter)
- [Design Patterns in C#](https://www.dofactory.com/net/design-patterns)

## 贡献 (Contributing)

欢迎贡献新的模型策略或改进现有实现！请遵循项目的设计模式原则。

---

**版本**: 1.0.0  
**最后更新**: 2026-05-03
