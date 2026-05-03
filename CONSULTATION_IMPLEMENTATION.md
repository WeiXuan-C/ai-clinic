# 患者咨询系统实现文档

## 📋 概述

本文档描述了患者咨询系统的完整实现，该系统允许患者创建与 AI 助手或真实医生的咨询会话，并保存所有聊天记录到数据库。

## 🎭 设计模式应用

### Facade Pattern（外观模式）

根据项目的设计模式规范，我们创建了 `ConsultationFacade` 来简化复杂的咨询系统交互。

#### 协调的子系统
```
ConsultationFacade
├── ConversationService    (管理对话)
├── MessageService         (管理消息)
├── DoctorProfileService   (管理医生信息)
└── ActivityLogService     (记录活动日志)
```

#### 优势
- ✅ **简化客户端代码**：一次调用完成多个操作
- ✅ **降低耦合**：UI 层不需要直接依赖多个服务
- ✅ **统一业务逻辑**：所有咨询相关的业务流程集中管理
- ✅ **易于维护**：修改业务流程只需更新 Facade

## 🏗️ 架构层次

```
┌─────────────────────────────────────────┐
│   UI Layer (Blazor Components)         │
│   - Consultation.razor                  │
│   - Consultation.razor.cs               │
└─────────────────┬───────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│   Facade Layer                          │
│   - ConsultationFacade                  │  ◄── 🎭 Facade Pattern
│     (协调多个服务)                       │
└─────────────────┬───────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│   Service Layer                         │
│   - ConversationService                 │
│   - MessageService                      │
│   - DoctorProfileService                │
│   - ActivityLogService                  │
└─────────────────┬───────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────┐
│   Data Layer                            │
│   - DbClient (Singleton)                │  ◄── 🎯 Singleton Pattern
│   - AiClinicDbContext                   │
└─────────────────────────────────────────┘
```

## 📁 文件结构

### 新增文件
```
Services/
├── Facades/
│   ├── ConsultationFacade.cs          # 咨询外观类（核心）
│   └── README.md                      # Facade Pattern 说明文档
├── ConversationService.cs             # 增强版（新增方法）
└── MessageService.cs                  # 增强版（新增方法）

UI/Pages/Patient/
├── Consultation.razor                 # 患者咨询页面 UI
└── Consultation.razor.cs              # 患者咨询页面逻辑

CONSULTATION_IMPLEMENTATION.md         # 本文档
```

### 修改文件
```
DependencyInjection.cs                 # 注册 ConsultationFacade
```

## 🔧 核心功能

### 1. 创建咨询会话

#### AI 咨询
```csharp
// 使用 Facade 一行代码完成：创建对话 + 记录日志 + 返回会话信息
var session = await consultationFacade.StartAiConsultationAsync(patientId);
```

**内部流程：**
1. 创建 Conversation 记录（AssignedDoctorId = null）
2. 记录活动日志
3. 获取消息列表
4. 返回 ConsultationSession DTO

#### 医生咨询
```csharp
// 使用 Facade 一行代码完成：验证医生 + 创建对话 + 记录日志
var session = await consultationFacade.StartDoctorConsultationAsync(patientId, doctorId);
```

**内部流程：**
1. 验证医生存在且可用
2. 创建 Conversation 记录（AssignedDoctorId = doctorId）
3. 记录活动日志
4. 获取消息列表
5. 获取医生信息
6. 返回 ConsultationSession DTO

### 2. 发送消息

```csharp
// 使用 Facade 自动处理 AI 响应
var result = await consultationFacade.SendPatientMessageAsync(
    conversationId, 
    patientId, 
    content
);

// result.PatientMessage: 患者消息
// result.AiResponse: AI 响应（如果是 AI 对话）
```

**内部流程：**
1. 创建患者消息
2. 更新对话的 LastMessageAt 和 TotalMessages
3. 记录活动日志
4. 如果是 AI 对话，自动生成 AI 响应
5. 返回 MessageResult DTO

### 3. 获取咨询会话

```csharp
// 使用 Facade 一次性获取所有需要的数据
var session = await consultationFacade.GetConsultationSessionAsync(
    conversationId, 
    userId, 
    userRole
);

// session 包含：
// - Conversation: 对话信息
// - Messages: 所有消息
// - DoctorInfo: 医生信息（如果有）
// - IsAiConsultation: 是否为 AI 对话
```

**内部流程：**
1. 获取对话信息
2. 获取所有消息
3. 标记消息为已读
4. 获取医生信息（如果有）
5. 返回 ConsultationSession DTO

## 💾 数据库设计

### Conversations 表
```sql
CREATE TABLE conversations (
    id UUID PRIMARY KEY,
    patient_id UUID NOT NULL,
    assigned_doctor_id UUID,              -- NULL = AI 对话
    title VARCHAR(255),
    status conversation_status,
    started_at TIMESTAMP,
    last_message_at TIMESTAMP,
    total_messages INTEGER DEFAULT 0,
    ai_messages_count INTEGER DEFAULT 0,
    doctor_messages_count INTEGER DEFAULT 0,
    ...
);
```

### Messages 表
```sql
CREATE TABLE messages (
    id UUID PRIMARY KEY,
    conversation_id UUID NOT NULL,
    sender_id UUID,                       -- NULL for AI messages
    sender_type message_sender_type,      -- 'patient', 'doctor', 'ai'
    content TEXT NOT NULL,
    ai_model_used VARCHAR(100),
    ai_confidence_score DECIMAL(5,4),
    is_read BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMP,
    ...
);
```

## 🎨 UI 功能

### 左侧边栏：对话列表
- 显示所有咨询会话
- 显示未读消息数量
- 区分 AI 对话和医生对话
- 点击切换当前对话
- "+ New" 按钮创建新对话

### 中间区域：聊天界面
- 显示当前对话的所有消息
- 区分患者、AI、医生消息（不同颜色）
- 输入框发送消息
- AI 对话自动生成响应
- Typing indicator 显示 AI 正在思考

### 右侧边栏：对话信息
- 显示对话基本信息
- 显示医生信息（如果有）
- 显示消息统计

### 新建对话模态框
- 选择 AI 助手或真实医生
- 如果选择医生，显示可用医生列表
- 显示医生评分、专业、经验等信息

## 🔄 数据流

### 创建 AI 咨询
```
用户点击 "+ New" 
  → 选择 "AI Assistant"
  → consultationFacade.StartAiConsultationAsync()
  → 创建 Conversation (assigned_doctor_id = NULL)
  → 记录 ActivityLog
  → 返回 ConsultationSession
  → UI 更新显示新对话
```

### 发送消息并获取 AI 响应
```
用户输入消息并发送
  → consultationFacade.SendPatientMessageAsync()
  → 创建 Message (sender_type = 'patient')
  → 更新 Conversation.last_message_at
  → 检测到 AI 对话
  → 生成 AI 响应
  → 创建 Message (sender_type = 'ai')
  → 返回 MessageResult (包含两条消息)
  → UI 显示患者消息和 AI 响应
```

## 🧪 AI 响应生成

当前实现了简单的规则引擎：

```csharp
private string GenerateSimpleAiResponse(string userMessage)
{
    if (userMessage.Contains("pain"))
        return "关于疼痛的详细问题...";
    else if (userMessage.Contains("fever"))
        return "关于发烧的详细问题...";
    else
        return "通用响应...";
}
```

**生产环境建议：**
- 集成真实的 AI API（OpenAI GPT-4、Claude、Gemini 等）
- 使用 RAG（检索增强生成）技术
- 结合患者病历和医疗知识库
- 实现多轮对话上下文管理

## 📊 DTO（数据传输对象）

### ConsultationSession
```csharp
public class ConsultationSession
{
    public Conversation Conversation { get; set; }
    public List<Message> Messages { get; set; }
    public bool IsAiConsultation { get; set; }
    public DoctorInfo? DoctorInfo { get; set; }
}
```

### MessageResult
```csharp
public class MessageResult
{
    public Message PatientMessage { get; set; }
    public Message? AiResponse { get; set; }
    public bool IsAiConsultation { get; set; }
}
```

### ConversationListItem
```csharp
public class ConversationListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public DateTime LastMessageAt { get; set; }
    public ConversationStatus Status { get; set; }
    public bool IsAiConversation { get; set; }
    public string? DoctorName { get; set; }
    public int UnreadCount { get; set; }
    public string? LastMessagePreview { get; set; }
}
```

## 🔐 安全性考虑

1. **认证检查**：页面加载时验证用户身份和角色
2. **权限验证**：只能访问自己的对话
3. **数据验证**：输入内容不能为空
4. **SQL 注入防护**：使用 EF Core 参数化查询
5. **活动日志**：记录所有关键操作

## 🚀 未来扩展

### 短期
- [ ] 添加文件上传功能（医疗文档、图片）
- [ ] 实现消息编辑和删除
- [ ] 添加消息搜索功能
- [ ] 实现对话导出（PDF）

### 中期
- [ ] 集成真实 AI API
- [ ] 实现实时通知（SignalR）
- [ ] 添加语音输入功能
- [ ] 实现视频通话功能

### 长期
- [ ] 多语言支持
- [ ] AI 辅助诊断
- [ ] 处方生成
- [ ] 医疗报告自动生成

## 📝 使用示例

### 患者创建 AI 咨询
```csharp
// 在 Consultation.razor.cs 中
private async Task CreateNewConversation(bool withAi)
{
    if (withAi)
    {
        var session = await ConsultationFacade.StartAiConsultationAsync(
            AuthState.CurrentUser!.Id
        );
        await LoadConversation(session.Conversation.Id);
    }
}
```

### 患者发送消息
```csharp
private async Task SendMessage()
{
    var result = await ConsultationFacade.SendPatientMessageAsync(
        currentConversation.Id,
        AuthState.CurrentUser!.Id,
        newMessage
    );
    
    messages.Add(result.PatientMessage);
    if (result.AiResponse != null)
    {
        messages.Add(result.AiResponse);
    }
}
```

## 🎓 设计模式学习要点

### Facade Pattern 的价值
1. **简化复杂性**：客户端不需要了解子系统的复杂交互
2. **降低耦合**：UI 层只依赖 Facade，不直接依赖多个服务
3. **提高可维护性**：业务逻辑变化只需修改 Facade
4. **统一接口**：提供一致的业务操作接口

### 何时使用 Facade
- ✅ 需要协调多个服务完成一个业务操作
- ✅ 同样的操作序列在多处重复
- ✅ 希望为复杂子系统提供简单接口
- ❌ 简单的单一操作不需要 Facade

## 📚 参考文档

- `Services/Facades/README.md` - Facade Pattern 详细说明
- `codeExample/facadeExampleCode.cs` - Facade Pattern 示例代码
- `.kiro/steering/design-patterns-knowledge.md` - 设计模式知识库

## ✅ 完成清单

- [x] 创建 ConsultationFacade
- [x] 增强 ConversationService
- [x] 增强 MessageService
- [x] 创建 Consultation.razor UI
- [x] 创建 Consultation.razor.cs 逻辑
- [x] 注册 DI 服务
- [x] 编写文档
- [x] 代码编译通过（无诊断错误）

## 🎉 总结

本实现完全遵循项目的设计模式规范，使用 **Facade Pattern** 简化了复杂的咨询系统交互。患者现在可以：

1. ✅ 创建与 AI 助手的咨询会话
2. ✅ 创建与真实医生的咨询会话
3. ✅ 发送消息并接收 AI 响应
4. ✅ 查看所有历史对话
5. ✅ 所有数据保存到数据库

系统架构清晰，代码可维护性高，为未来扩展奠定了良好基础。
