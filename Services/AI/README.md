# AI Services Architecture
# AI服务架构

## 设计模式实现 (Design Pattern Implementation)

本目录包含使用 **Strategy Pattern** 和 **Adapter Pattern** 实现的AI模型切换系统。

### 核心组件 (Core Components)

#### 1. Strategy Pattern Components

- **IAiModelStrategy.cs** - 策略接口
  - 定义所有AI模型策略必须实现的契约
  - 包含 `GenerateResponseAsync()` 和 `GenerateStreamingResponseAsync()` 方法

- **AiModelContext.cs** - 策略上下文
  - 管理当前活动的策略
  - 提供策略切换功能
  - 维护所有可用策略的字典

- **Strategies/** - 具体策略实现
  - `OwlAlphaStrategy.cs` - OpenRouter Owl Alpha (推理模型)
  - `NemotronStrategy.cs` - NVIDIA Nemotron (快速响应)
  - `QianfanOcrStrategy.cs` - Baidu Qianfan (OCR专用)
  - `Gemma4Strategy.cs` - Google Gemma 4 (通用模型)

#### 2. Adapter Pattern Components

- **BaseAiModelAdapter.cs** - 适配器基类
  - 实现 `IAiModelStrategy` 接口 (Target)
  - 包装 `OpenRouterApiClient` (Adaptee)
  - 转换OpenRouter API格式到统一接口

- **OpenRouterApiClient.cs** - 被适配者
  - 封装OpenRouter REST API调用
  - 处理HTTP请求/响应
  - 管理认证和错误处理

## 类图 (Class Diagram)

```
┌─────────────────────────────────┐
│   <<interface>>                 │
│   IAiModelStrategy              │
├─────────────────────────────────┤
│ + ModelId: string               │
│ + ModelName: string             │
│ + GenerateResponseAsync()       │
│ + GenerateStreamingResponseAsync()│
└────────────┬────────────────────┘
             │
             │ implements
             │
┌────────────▼────────────────────┐
│   BaseAiModelAdapter            │
│   (Adapter)                     │
├─────────────────────────────────┤
│ # _apiClient: OpenRouterApiClient│
│ + GenerateResponseAsync()       │
│ # PreprocessPrompt()            │
│ # PostprocessResponse()         │
└────────────┬────────────────────┘
             │
             │ extends
             │
    ┌────────┼────────┬────────┐
    │        │        │        │
┌───▼───┐ ┌─▼────┐ ┌─▼────┐ ┌─▼────┐
│ Owl   │ │Nemot-│ │Qianfan│ │Gemma4│
│Alpha  │ │ron   │ │OCR   │ │      │
└───────┘ └──────┘ └──────┘ └──────┘

┌─────────────────────────────────┐
│   AiModelContext                │
│   (Strategy Context)            │
├─────────────────────────────────┤
│ - _currentStrategy              │
│ - _availableStrategies          │
│ + SetStrategy()                 │
│ + GenerateResponseAsync()       │
└─────────────────────────────────┘

┌─────────────────────────────────┐
│   OpenRouterApiClient           │
│   (Adaptee)                     │
├─────────────────────────────────┤
│ - _httpClient                   │
│ - _apiKey                       │
│ + CallApiAsync()                │
│ + GetModelInfoAsync()           │
└─────────────────────────────────┘
```

## 工作流程 (Workflow)

### 1. 初始化流程

```
Application Startup
    │
    ├─> Register HttpClient
    ├─> Register OpenRouterApiClient (Adaptee)
    ├─> Register AiModelContext (Context)
    │       │
    │       ├─> Create OwlAlphaStrategy
    │       ├─> Create NemotronStrategy
    │       ├─> Create QianfanOcrStrategy
    │       └─> Create Gemma4Strategy
    │
    └─> Register AiAssistantService
```

### 2. 请求处理流程

```
User Request
    │
    ▼
AiAssistantService
    │
    ├─> (Optional) SwitchModel("gemma-4")
    │
    ▼
AiModelContext
    │
    ├─> SetStrategy("gemma-4")
    │   └─> _currentStrategy = Gemma4Strategy
    │
    ▼
GenerateResponseAsync()
    │
    ▼
Gemma4Strategy (extends BaseAiModelAdapter)
    │
    ├─> PreprocessPrompt()
    │
    ▼
BaseAiModelAdapter
    │
    ├─> Build OpenRouterRequest
    │
    ▼
OpenRouterApiClient
    │
    ├─> POST https://openrouter.ai/api/v1/chat/completions
    │
    ▼
OpenRouter API
    │
    ▼
Response
    │
    ├─> Adapt to unified format
    │
    ▼
Return to User
```

## 添加新模型 (Adding New Models)

### 步骤 1: 创建策略类

在 `Strategies/` 目录创建新文件:

```csharp
namespace ai_clinic.Services.AI.Strategies
{
    public class NewModelStrategy : BaseAiModelAdapter
    {
        public override string ModelId => "provider/model-id";
        public override string ModelName => "Display Name";

        public NewModelStrategy(OpenRouterApiClient apiClient) 
            : base(apiClient)
        {
        }
    }
}
```

### 步骤 2: 注册到Context

在 `AiModelContext.cs` 构造函数中:

```csharp
_availableStrategies = new Dictionary<string, IAiModelStrategy>
{
    // ... existing models
    ["new-model"] = new NewModelStrategy(_apiClient)
};
```

## 设计原则 (Design Principles)

### SOLID Principles

- **Single Responsibility**: 每个策略类只负责一个模型
- **Open/Closed**: 可以添加新策略而不修改现有代码
- **Liskov Substitution**: 所有策略可以互换使用
- **Interface Segregation**: 接口只包含必要的方法
- **Dependency Inversion**: 依赖抽象(接口)而非具体实现

### Gang of Four Patterns

- **Strategy**: 封装算法族，使它们可以互换
- **Adapter**: 转换接口，使不兼容的类可以协作

## 测试建议 (Testing Recommendations)

### 单元测试

```csharp
[Fact]
public void SetStrategy_ValidKey_ChangesCurrentStrategy()
{
    // Arrange
    var mockClient = new Mock<OpenRouterApiClient>();
    var context = new AiModelContext(mockClient.Object);
    
    // Act
    context.SetStrategy("gemma-4");
    
    // Assert
    Assert.Equal("google/gemma-4-26b-a4b-it:free", 
        context.CurrentStrategy.ModelId);
}
```

### 集成测试

```csharp
[Fact]
public async Task GenerateResponse_WithRealApi_ReturnsValidResponse()
{
    // Arrange
    var service = GetConfiguredAiService();
    
    // Act
    var response = await service.GenerateResponseAsync("Hello");
    
    // Assert
    Assert.NotEmpty(response);
}
```

## 性能优化 (Performance Optimization)

- 使用 `HttpClient` 连接池
- 实现响应缓存
- 考虑异步流式响应
- 添加请求超时和重试逻辑

## 安全考虑 (Security Considerations)

- API密钥存储在配置中，不要提交到版本控制
- 验证和清理用户输入
- 实现速率限制
- 记录API使用情况

## 相关文档 (Related Documentation)

- **完整指南**: `/AI_MODEL_SWITCHING_GUIDE.md`
- **使用示例**: `/EXAMPLE_AI_MODEL_USAGE.md`
- **快速开始**: `/QUICK_START_AI_MODELS.md`
- **设计模式**: `/.kiro/steering/design-patterns-knowledge.md`

---

**维护者**: AI Clinic Team  
**最后更新**: 2026-05-03
