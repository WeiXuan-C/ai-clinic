# Quick Start: AI Model Switching
# 快速开始: AI模型切换

## 🚀 快速设置 (Quick Setup)

### 1. 配置API密钥 (Configure API Key)

在 `appsettings.json` 中添加:
```json
{
  "OpenRouter": {
    "ApiKey": "sk-or-v1-your-api-key-here",
    "DefaultModel": "owl-alpha"
  }
}
```

### 2. 获取API密钥 (Get API Key)

访问 https://openrouter.ai/ 注册并获取免费API密钥

### 3. 使用服务 (Use the Service)

```csharp
// 在控制器或服务中注入
public class MyService
{
    private readonly AiAssistantService _aiService;

    public MyService(AiAssistantService aiService)
    {
        _aiService = aiService;
    }

    public async Task<string> GetAiResponse(string prompt)
    {
        // 使用默认模型
        return await _aiService.GenerateResponseAsync(prompt);
    }
}
```

## 📋 可用模型 (Available Models)

| 模型键 | 名称 | 用途 |
|--------|------|------|
| `owl-alpha` | Owl Alpha | 复杂推理任务 |
| `nemotron` | NVIDIA Nemotron | 快速响应 |
| `qianfan-ocr` | Baidu Qianfan | 文档OCR |
| `gemma-4` | Google Gemma 4 | 通用任务 |

## 💡 常用操作 (Common Operations)

### 切换模型
```csharp
await _aiFacade.SwitchModelAsync("gemma-4");
```

### 生成响应
```csharp
var result = await _aiFacade.GenerateResponseAsync(
    "What is a headache?",
    systemInstructions: "You are a medical assistant",
    temperature: 0.7
);

if (result.Success)
{
    Console.WriteLine(result.Response);
}
```

### 医疗咨询
```csharp
var result = await _aiFacade.GenerateMedicalConsultationAsync(
    patientQuery: "I have a headache",
    medicalContext: "Patient history: No allergies",
    patientId: patientGuid
);
```

### 生成咨询笔记
```csharp
var result = await _aiFacade.GenerateConsultationNoteAsync(
    conversationSummary: "Patient complained of headache",
    symptoms: "Headache, mild fever",
    diagnosis: "Tension headache",
    doctorId: doctorGuid
);
```

### 智能模型选择
```csharp
// 自动选择最佳模型
await _aiFacade.SwitchToOptimalModelAsync(AiTaskType.Reasoning);
await _aiFacade.SwitchToOptimalModelAsync(AiTaskType.DocumentAnalysis);
```

## 🎯 设计模式说明 (Design Patterns)

### Strategy Pattern (策略模式)
- **接口**: `IAiModelStrategy`
- **具体策略**: `OwlAlphaStrategy`, `NemotronStrategy`, 等
- **上下文**: `AiModelContext`

### Adapter Pattern (适配器模式)
- **目标接口**: `IAiModelStrategy`
- **适配器**: `BaseAiModelAdapter`
- **被适配者**: `OpenRouterApiClient`

## 📁 文件结构 (File Structure)

```
Services/
├── AI/
│   ├── IAiModelStrategy.cs          # 策略接口
│   ├── BaseAiModelAdapter.cs        # 适配器基类
│   ├── OpenRouterApiClient.cs       # 外部API客户端
│   ├── AiModelContext.cs            # 策略上下文
│   └── Strategies/
│       ├── OwlAlphaStrategy.cs      # Owl Alpha策略
│       ├── NemotronStrategy.cs      # Nemotron策略
│       ├── QianfanOcrStrategy.cs    # Qianfan OCR策略
│       └── Gemma4Strategy.cs        # Gemma 4策略
├── Facades/
│   └── AiFacade.cs                  # 🎭 AI外观类 (推荐使用)
├── AiAssistantService.cs            # 高级服务
└── ...
```

## 🔧 使用方式 (Usage)

### 通过 Facade (推荐)

```csharp
// 注入 AiFacade
@inject AiFacade AiFacade

// 获取可用模型
var models = AiFacade.GetAvailableModels();

// 切换模型
await AiFacade.SwitchModelAsync("gemma-4");

// 生成响应
var result = await AiFacade.GenerateResponseAsync("Hello!");
```

### 直接使用服务 (高级)

```csharp
// 注入 AiAssistantService
@inject AiAssistantService AiService

// 切换模型
AiService.SwitchModel("gemma-4");

// 生成响应
var response = await AiService.GenerateResponseAsync("Hello!");
```

## ✅ 验证安装 (Verify Installation)

在 Blazor 组件或服务中测试:
```csharp
@inject AiFacade AiFacade

@code {
    protected override void OnInitialized()
    {
        var models = AiFacade.GetAvailableModels();
        Console.WriteLine($"Available models: {models.Count}");
    }
}
```

## 📚 更多文档 (More Documentation)

- **完整指南**: `AI_MODEL_SWITCHING_GUIDE.md`
- **使用示例**: `EXAMPLE_AI_MODEL_USAGE.md`
- **设计模式**: `design-patterns-knowledge.md`

## 🎓 学习资源 (Learning Resources)

- Strategy Pattern: `codeExample/strategyExampleCode.cs`
- Adapter Pattern: `codeExample/adapterExampleCode.cs`

---

**版本**: 1.0.0  
**创建日期**: 2026-05-03
