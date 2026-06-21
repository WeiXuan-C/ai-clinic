# AI Clinic 系统调用流程文档

本文档详细说明了 AI Clinic 系统中**注册**、**登录**和**使用Chat聊天**的完整调用流程。

---

## 目录

1. [注册流程 (Registration Flow)](#1️⃣-注册流程-registration-flow)
2. [登录流程 (Sign In Flow)](#2️⃣-登录流程-sign-in-flow)
3. [使用Chat聊天流程 (Chat Consultation Flow)](#3️⃣-使用chat聊天流程-chat-consultation-flow)
4. [关键文件列表](#📂-关键文件列表)
5. [设计模式使用](#🎯-设计模式使用)

---

## 1️⃣ 注册流程 (Registration Flow)

### 调用链路

```
📱 前端页面
UI/Pages/Auth/Signup.razor
    ↓ 用户填写表单并点击 "Create Account"
    ↓
    HandleSignUp() 方法
    ↓
    ↓ 调用 AuthFacade
    ↓
⚙️ 业务逻辑层 - Facade 模式
Services/Facades/AuthFacade.cs
    ↓ RegisterAsync(email, password, role)
    ↓
    ├─→ 📝 步骤1: 创建用户账户
    │   Services/UserService.cs
    │   └─→ CreateAsync(user, password)
    │        ├─→ 使用 BCrypt 哈希密码
    │        ├─→ 保存到数据库 (Users 表)
    │        └─→ 返回创建的 User 对象
    │
    ├─→ 📋 步骤2: 创建用户Profile (根据角色)
    │   如果 role = Patient:
    │       Services/PatientProfileService.cs
    │       └─→ CreateAsync(patientProfile)
    │            └─→ 保存到 PatientProfiles 表
    │   如果 role = Doctor:
    │       Services/DoctorProfileService.cs
    │       └─→ CreateAsync(doctorProfile)
    │            └─→ 保存到 DoctorProfiles 表
    │
    ├─→ 📊 步骤3: 记录活动日志
    │   Services/ActivityLogService.cs
    │   └─→ LogActivityAsync(userId, "user_registered", details)
    │        └─→ 保存到 ActivityLogs 表
    │
    └─→ 🔐 步骤4: 设置认证状态
        Services/AuthStateService.cs
        └─→ SetCurrentUserAsync(user, services...)
             ├─→ 加载用户Profile (PatientProfile 或 DoctorProfile)
             ├─→ 设置 Cookie (userId, 30天过期)
             └─→ 触发 OnAuthStateChanged 事件
```

### 涉及的关键文件

| 文件路径 | 职责 |
|---------|------|
| `UI/Pages/Auth/Signup.razor` | 注册页面UI |
| `Services/Facades/AuthFacade.cs` | **认证门面** - 协调多个服务 |
| `Services/UserService.cs` | 用户数据库操作 (CRUD) |
| `Services/PatientProfileService.cs` | 患者资料管理 |
| `Services/DoctorProfileService.cs` | 医生资料管理 |
| `Services/ActivityLogService.cs` | 活动日志记录 |
| `Services/AuthStateService.cs` | Session 和 Cookie 管理 |

### 数据流

```
User Input (Email, Password, Role)
    ↓
AuthFacade.RegisterAsync()
    ↓
┌─────────────────────────────────────────────┐
│  事务性操作 (Transaction)                    │
│  1. 创建 User (BCrypt 哈希密码)              │
│  2. 创建 PatientProfile 或 DoctorProfile    │
│  3. 记录 ActivityLog                         │
│  4. 设置 Cookie (userId)                     │
└─────────────────────────────────────────────┘
    ↓
返回 AuthResult { IsSuccess, User }
    ↓
前端跳转到对应Dashboard
```

---

## 2️⃣ 登录流程 (Sign In Flow)

### 调用链路

```
📱 前端页面
UI/Pages/Auth/Signin.razor
    ↓ 用户输入邮箱密码并点击 "Sign In"
    ↓
    HandleSignIn() 方法
    ↓
    ↓ 调用 AuthFacade
    ↓
⚙️ 业务逻辑层 - Facade 模式
Services/Facades/AuthFacade.cs
    ↓ SignInAsync(email, password, ipAddress)
    ↓
    ├─→ 🔍 步骤1: 验证用户凭证
    │   Services/UserService.cs
    │   └─→ AuthenticateAsync(email, password)
    │        ├─→ GetByEmailAsync(email) - 查找用户
    │        ├─→ BCrypt.Verify(password, user.PasswordHash) - 验证密码
    │        ├─→ 更新 LastLoginAt 时间戳
    │        └─→ 返回 User 对象 (验证成功) 或 null (失败)
    │
    ├─→ ✅ 步骤2: 检查账户状态
    │   检查 user.IsActive 和 user.IsDeactivated
    │   Services/UserSuspensionService.cs
    │   └─→ IsUserSuspendedAsync(userId)
    │        └─→ 查询 UserSuspensions 表
    │             └─→ 返回是否被暂停
    │
    ├─→ 📊 步骤3: 记录登录活动
    │   Services/ActivityLogService.cs
    │   └─→ LogActivityAsync(userId, "user_login", details, ipAddress)
    │        └─→ 保存登录记录到 ActivityLogs 表
    │
    └─→ 🔐 步骤4: 设置认证状态
        Services/AuthStateService.cs
        └─→ SetCurrentUserAsync(user, services...)
             ├─→ 根据角色加载Profile:
             │   ├─→ Patient: PatientProfileService.GetByUserIdAsync()
             │   └─→ Doctor: DoctorProfileService.GetByUserIdAsync()
             ├─→ 设置 Cookie (userId, 30天过期)
             │    await SetCookieAsync("userId", user.Id.ToString(), 30)
             └─→ 触发 OnAuthStateChanged 事件
```

### 涉及的关键文件

| 文件路径 | 职责 |
|---------|------|
| `UI/Pages/Auth/Signin.razor` | 登录页面UI |
| `Services/Facades/AuthFacade.cs` | **认证门面** - 协调认证流程 |
| `Services/UserService.cs` | 用户认证和密码验证 |
| `Services/UserSuspensionService.cs` | 账户暂停状态检查 |
| `Services/ActivityLogService.cs` | 登录日志记录 |
| `Services/AuthStateService.cs` | Session 管理（Cookie读写） |

### 密码验证机制

```
用户输入密码 (plaintext)
    ↓
BCrypt.Net.BCrypt.Verify(plaintext, storedHash)
    ↓
比较结果:
    ✓ 匹配 → 返回 User 对象
    ✗ 不匹配 → 返回 null
```

### Cookie存储机制

```javascript
// 通过 JavaScript Interop 设置Cookie
document.cookie = 'userId={userId}; expires={30天后}; path=/; SameSite=Strict'
```

---

## 3️⃣ 使用Chat聊天流程 (Chat Consultation Flow)

### A. 初始化和加载对话列表

```
📱 前端页面初始化
UI/Pages/Patient/Consultation.razor.cs
    ↓ OnInitializedAsync()
    ↓
    ├─→ 🔌 步骤1: 初始化SignalR实时通信
    │   InitializeSignalR()
    │   └─→ Services/SignalRConsultationService.cs
    │        └─→ InitializeAsync(hubUrl)
    │             ├─→ 连接到 /consultationHub
    │             ├─→ 订阅事件:
    │             │   • OnMessageReceived
    │             │   • OnUserTyping
    │             │   • OnUserStoppedTyping
    │             │   • OnConnected / OnDisconnected
    │             └─→ Services/Hubs/ConsultationHub.cs
    │                  └─→ RegisterUser(userId) - 注册连接
    │
    ├─→ 📋 步骤2: 加载对话列表
    │   LoadConversations()
    │   └─→ Services/Facades/ConsultationFacade.cs
    │        └─→ GetPatientConsultationsAsync(userId)
    │             └─→ Services/ConversationService.cs
    │                  └─→ GetConversationListByPatientIdAsync(userId)
    │                       ├─→ 查询 Conversations 表
    │                       ├─→ Include AssignedDoctor
    │                       ├─→ Include Messages (最新1条)
    │                       └─→ 返回 List<ConversationListItem>
    │
    └─→ 💬 步骤3: 加载选中的对话
        LoadConversation(conversationId)
        └─→ ConsultationFacade.GetConsultationSessionAsync(conversationId, userId, role)
             ├─→ ConversationService.GetByIdAsync(conversationId)
             ├─→ MessageService.GetByConversationIdAsync(conversationId)
             ├─→ MessageService.MarkConversationAsReadAsync(conversationId)
             └─→ SignalRService.JoinConversationAsync(conversationId)
                  └─→ ConsultationHub.JoinConversation(conversationId)
                       └─→ Groups.AddToGroupAsync(connectionId, "conversation_{id}")
```

### B. 发送消息并获取AI响应 (流式传输)

```
📱 前端用户操作
UI/Pages/Patient/Consultation.razor.cs
    ↓ 用户输入消息并点击发送按钮
    ↓
    SendMessage() 方法
    ↓
    ↓ 调用 ConsultationFacade (异步流)
    ↓
⚙️ 业务逻辑层 - Facade 模式
Services/Facades/ConsultationFacade.cs
    ↓ SendPatientMessageWithStreamingAsync(conversationId, patientId, content)
    ↓ [返回 IAsyncEnumerable<StreamingMessageChunk>]
    ↓
    ├─→ 💾 步骤1: 保存患者消息
    │   using (var db = DbClient.Instance.GetDb())
    │   {
    │       var transaction = await db.Database.BeginTransactionAsync();
    │       
    │       // 创建患者消息
    │       var patientMessage = new Message
    │       {
    │           ConversationId = conversationId,
    │           SenderId = patientId,
    │           SenderType = MessageSenderType.Patient,
    │           Content = content,
    │           CreatedAt = DateTime.UtcNow
    │       };
    │       
    │       db.Messages.Add(patientMessage);
    │       conversation.TotalMessages++;
    │       
    │       await db.SaveChangesAsync();
    │       await transaction.CommitAsync();
    │   }
    │   
    │   ↓ 通过 SignalR 实时广播
    │   Services/Hubs/ConsultationHub.cs
    │   └─→ SendMessageToConversation(conversationId, patientMessage)
    │        └─→ Clients.Group("conversation_{id}").SendAsync("ReceiveMessage", data)
    │   
    │   ↓ 返回给前端
    │   yield return new StreamingMessageChunk { IsUserMessage = true, Message = patientMessage }
    │
    └─→ 🤖 步骤2: 生成AI响应 (如果是AI对话)
        if (conversation.AssignedDoctorId == null)
        {
            ├─→ GenerateAiResponseWithFallbackAsync(content)
            │   └─→ Services/AiAssistantService.cs
            │        └─→ GenerateStreamingMedicalResponseAsync(patientQuery, medicalContext, temperature)
            │             └─→ Services/AI/OpenRouterApiClient.cs
            │                  └─→ 调用外部 OpenRouter API
            │                       └─→ POST https://openrouter.ai/api/v1/chat/completions
            │                            {
            │                              "model": "mistralai/mistral-large-2411",
            │                              "messages": [...],
            │                              "stream": true  // 流式传输
            │                            }
            │   
            │   ↓ 接收流式响应
            │   await foreach (var chunk in stream)
            │   {
            │       fullResponse += chunk;
            │       
            │       ↓ 实时返回给前端
            │       yield return new StreamingMessageChunk 
            │       { 
            │           IsAiChunk = true, 
            │           Content = chunk 
            │       };
            │       
            │       ↓ 前端接收并实时更新UI
            │       aiMessage.Content += chunk;
            │       await InvokeAsync(() => StateHasChanged());
            │   }
            │   
            └─→ 💾 保存完整AI响应到数据库
                using (var db = DbClient.Instance.GetDb())
                {
                    var aiResponse = new Message
                    {
                        ConversationId = conversationId,
                        SenderId = null,  // AI 无 userId
                        SenderType = MessageSenderType.AI,
                        Content = fullResponse,
                        AiModelUsed = "Mistral Large",
                        AiConfidenceScore = 0.85m,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    db.Messages.Add(aiResponse);
                    conversation.AiMessagesCount++;
                    
                    await db.SaveChangesAsync();
                }
                
                ↓ 返回完整消息
                yield return new StreamingMessageChunk 
                { 
                    IsComplete = true, 
                    Message = aiResponse 
                };
        }
```

### C. SignalR 实时通信机制

```
🔌 SignalR Hub (服务器端)
Services/Hubs/ConsultationHub.cs
    ↓
    ├─→ 连接管理
    │   • OnConnectedAsync() - 客户端连接
    │   • OnDisconnectedAsync() - 客户端断开
    │   • RegisterUser(userId) - 注册用户连接
    │        └─→ _onlineUsers[connectionId] = userId
    │        └─→ _userConnections[userId].Add(connectionId)
    │
    ├─→ 群组管理
    │   • JoinConversation(conversationId) - 加入对话群组
    │        └─→ Groups.AddToGroupAsync(connectionId, "conversation_{id}")
    │   • LeaveConversation(conversationId) - 离开对话群组
    │
    ├─→ 消息广播
    │   • SendMessageToConversation(conversationId, message)
    │        └─→ Clients.Group("conversation_{id}").SendAsync("ReceiveMessage", data)
    │
    └─→ 输入状态通知
        • NotifyTyping(conversationId, userName, userRole)
             └─→ Clients.OthersInGroup(...).SendAsync("UserTyping", data)
        • NotifyStoppedTyping(conversationId, userName)
             └─→ Clients.OthersInGroup(...).SendAsync("UserStoppedTyping", data)

📱 SignalR Client (前端)
UI/Pages/Patient/Consultation.razor.cs
    ↓ 事件监听
    ├─→ HandleMessageReceived(MessageReceivedEventArgs args)
    │   └─→ 接收新消息并更新 messages 列表
    │   └─→ await InvokeAsync(StateHasChanged)
    │
    ├─→ HandleUserTyping(TypingEventArgs args)
    │   └─→ 显示 "对方正在输入..." 提示
    │   └─→ isTyping = true
    │
    └─→ HandleUserStoppedTyping(TypingEventArgs args)
        └─→ 隐藏输入提示
        └─→ isTyping = false
```

### 流式传输时序图

```
Patient                Frontend              Facade                AI Service           Database         SignalR Hub
  │                      │                     │                       │                   │                 │
  │  输入消息并发送        │                     │                       │                   │                 │
  │─────────────────────>│                     │                       │                   │                 │
  │                      │  SendMessage()      │                       │                   │                 │
  │                      │────────────────────>│                       │                   │                 │
  │                      │                     │  保存患者消息          │                   │                 │
  │                      │                     │──────────────────────────────────────────>│                 │
  │                      │                     │                       │                   │  Message saved  │
  │                      │                     │                       │                   │<────────────────│
  │                      │                     │  广播患者消息                             │                 │
  │                      │                     │───────────────────────────────────────────────────────────>│
  │                      │  Receive patient msg│                       │                   │                 │
  │                      │<────────────────────────────────────────────────────────────────────────────────│
  │  显示患者消息          │                     │                       │                   │                 │
  │<─────────────────────│                     │                       │                   │                 │
  │                      │                     │  调用AI (流式)         │                   │                 │
  │                      │                     │──────────────────────>│                   │                 │
  │                      │                     │                       │  OpenRouter API   │                 │
  │                      │                     │                       │  (stream=true)    │                 │
  │                      │                     │                       │────────>          │                 │
  │                      │  chunk #1           │                       │                   │                 │
  │                      │<────────────────────│<──────────────────────│<────────          │                 │
  │  实时显示第1段         │                     │                       │                   │                 │
  │<─────────────────────│                     │                       │                   │                 │
  │                      │  chunk #2           │                       │                   │                 │
  │                      │<────────────────────│<──────────────────────│<────────          │                 │
  │  实时显示第2段         │                     │                       │                   │                 │
  │<─────────────────────│                     │                       │                   │                 │
  │                      │  ...                │                       │                   │                 │
  │                      │  chunk #N (complete)│                       │                   │                 │
  │                      │<────────────────────│<──────────────────────│<────────          │                 │
  │                      │                     │  保存完整AI响应        │                   │                 │
  │                      │                     │──────────────────────────────────────────>│                 │
  │                      │                     │                       │                   │  Message saved  │
  │                      │                     │                       │                   │<────────────────│
  │                      │                     │  广播完整AI消息                           │                 │
  │                      │                     │───────────────────────────────────────────────────────────>│
  │                      │  AI message complete│                       │                   │                 │
  │                      │<────────────────────────────────────────────────────────────────────────────────│
  │  显示完整AI消息        │                     │                       │                   │                 │
  │<─────────────────────│                     │                       │                   │                 │
```

---

## 📂 关键文件列表

### 🔐 认证相关 (Authentication)

| 文件路径 | 职责 | 设计模式 |
|---------|------|---------|
| `UI/Pages/Auth/Signin.razor` | 登录页面UI | - |
| `UI/Pages/Auth/Signup.razor` | 注册页面UI | - |
| `Services/Facades/AuthFacade.cs` | **认证门面** - 协调多个认证服务 | **Facade Pattern** |
| `Services/UserService.cs` | 用户CRUD和密码验证 (BCrypt) | - |
| `Services/AuthStateService.cs` | Session管理（Cookie存储和读取） | - |
| `Services/PatientProfileService.cs` | 患者资料CRUD操作 | - |
| `Services/DoctorProfileService.cs` | 医生资料CRUD操作 | - |
| `Services/ActivityLogService.cs` | 用户活动日志记录 | - |
| `Services/UserSuspensionService.cs` | 账户暂停状态管理 | - |

### 💬 聊天咨询相关 (Chat/Consultation)

| 文件路径 | 职责 | 设计模式 |
|---------|------|---------|
| `UI/Pages/Patient/Consultation.razor` | 患者咨询页面UI | - |
| `UI/Pages/Patient/Consultation.razor.cs` | 患者咨询页面逻辑 | - |
| `Services/Facades/ConsultationFacade.cs` | **咨询门面** - 协调对话、消息、AI | **Facade Pattern** |
| `Services/ConversationService.cs` | 对话会话管理 (Conversation CRUD) | - |
| `Services/MessageService.cs` | 消息管理 (Message CRUD) | - |
| `Services/AiAssistantService.cs` | AI助手服务 (模型切换、响应生成) | **Strategy Pattern** |
| `Services/AI/OpenRouterApiClient.cs` | 外部API适配器 (OpenRouter API) | **Adapter Pattern** |
| `Services/Hubs/ConsultationHub.cs` | **SignalR Hub** - 实时通信中心 | - |
| `Services/SignalRConsultationService.cs` | SignalR客户端服务 | - |
| `Services/DoctorRecommendation/DoctorRecommendationService.cs` | 医生推荐服务 | - |
| `Services/DocumentService.cs` | 文档/附件管理 | - |

### 🗄️ 数据访问相关 (Data Access)

| 文件路径 | 职责 | 设计模式 |
|---------|------|---------|
| `Data/DbClient.cs` | 数据库连接管理 | **Singleton Pattern** |
| `Models/User.cs` | 用户实体模型 | - |
| `Models/PatientProfile.cs` | 患者资料实体 | - |
| `Models/DoctorProfile.cs` | 医生资料实体 | - |
| `Models/Conversation.cs` | 对话实体 | - |
| `Models/Message.cs` | 消息实体 | - |
| `Models/Document.cs` | 文档实体 | - |

---

## 🎯 设计模式使用

### 1. **Facade Pattern (门面模式)**

**使用位置:**
- `Services/Facades/AuthFacade.cs`
- `Services/Facades/ConsultationFacade.cs`
- `Services/Facades/AiFacade.cs`

**作用:**
- 简化客户端代码，隐藏子系统复杂性
- 提供统一的高层接口
- 协调多个服务之间的交互

**示例:**

```csharp
// 不使用Facade - 客户端需要调用多个服务
var user = await _userService.CreateAsync(user, password);
var profile = await _patientProfileService.CreateAsync(profile);
await _activityLogService.LogActivityAsync(user.Id, "user_registered", details);
await _authStateService.SetCurrentUserAsync(user, ...);

// 使用Facade - 一行代码完成所有操作
var result = await _authFacade.RegisterAsync(email, password, role);
```

### 2. **Strategy Pattern (策略模式)**

**使用位置:**
- `Services/AiAssistantService.cs` - AI模型切换
- `Services/AI/AiModelContext.cs` - 策略上下文
- `Services/AI/IAiModelStrategy.cs` - 策略接口

**作用:**
- 运行时动态切换AI模型
- 封装不同模型的算法实现
- 使模型选择与业务逻辑解耦

**示例:**

```csharp
// 切换到不同的AI模型策略
_aiAssistantService.SwitchModel("owl-alpha");      // Mistral Large (推理)
_aiAssistantService.SwitchModel("gemma-4");        // Google Gemma (通用)
_aiAssistantService.SwitchModel("qianfan-ocr");    // Baidu Qianfan (OCR)
```

### 3. **Adapter Pattern (适配器模式)**

**使用位置:**
- `Services/AI/OpenRouterApiClient.cs`

**作用:**
- 将外部OpenRouter API适配到系统接口
- 隔离外部API变化对系统的影响
- 提供统一的API调用接口

**示例:**

```csharp
// 适配器将不同的API格式转换为统一接口
public async Task<string> GenerateResponseAsync(...)
{
    // 适配 OpenRouter API 格式
    var request = new 
    {
        model = _modelId,
        messages = messages,
        stream = false
    };
    
    // 调用外部API并转换响应
    var response = await _httpClient.PostAsJsonAsync("...", request);
    return ParseResponse(response);
}
```

### 4. **Singleton Pattern (单例模式)**

**使用位置:**
- `Data/DbClient.cs` - 数据库连接管理

**作用:**
- 确保整个应用只有一个数据库连接实例
- 提供全局访问点
- 管理数据库上下文生命周期

**示例:**

```csharp
public class DbClient
{
    private static DbClient? _instance;
    private static readonly object _lock = new object();
    
    public static DbClient Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new DbClient();
                    }
                }
            }
            return _instance;
        }
    }
    
    public AppDbContext GetDb() => new AppDbContext();
}
```

---

## 🔒 安全机制

### 密码哈希

```csharp
// 注册时哈希密码
user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

// 登录时验证密码
bool isValid = BCrypt.Net.BCrypt.Verify(plainPassword, user.PasswordHash);
```

### Cookie 安全设置

```javascript
// SameSite=Strict 防止CSRF攻击
document.cookie = 'userId={id}; expires={date}; path=/; SameSite=Strict'
```

### 账户暂停检查

```csharp
// 登录时检查暂停状态
var isSuspended = await _suspensionService.IsUserSuspendedAsync(user.Id);
if (isSuspended)
{
    return AuthResult.Failure("Your account has been suspended.");
}
```

---

## 📊 数据库表关系

```
Users (用户表)
  ├─→ PatientProfiles (1:1) - 患者资料
  ├─→ DoctorProfiles (1:1) - 医生资料
  ├─→ AdminProfiles (1:1) - 管理员资料
  ├─→ ActivityLogs (1:N) - 活动日志
  └─→ UserSuspensions (1:N) - 暂停记录

Conversations (对话表)
  ├─→ Messages (1:N) - 消息列表
  ├─→ Documents (1:N) - 附件列表
  ├─→ Patient (N:1) → Users
  └─→ AssignedDoctor (N:1) → Users

Messages (消息表)
  ├─→ Conversation (N:1) → Conversations
  └─→ Sender (N:1) → Users (可为null，AI消息无sender)

Documents (文档表)
  ├─→ Conversation (N:1) → Conversations
  └─→ UploadedByUser (N:1) → Users
```

---

## 🚀 流式传输机制

### 为什么使用流式传输？

1. **实时反馈**: 用户可以立即看到AI开始生成响应
2. **更好的用户体验**: 类似ChatGPT的逐字显示效果
3. **降低感知延迟**: 即使总响应时间相同，用户感觉更快

### 技术实现

```csharp
// 后端：使用 IAsyncEnumerable 实现流式返回
public async IAsyncEnumerable<StreamingMessageChunk> SendPatientMessageWithStreamingAsync(...)
{
    // Step 1: 保存患者消息
    yield return new StreamingMessageChunk { IsUserMessage = true, Message = patientMessage };
    
    // Step 2: 流式生成AI响应
    await foreach (var chunk in GenerateAiResponseAsync(...))
    {
        yield return new StreamingMessageChunk { IsAiChunk = true, Content = chunk };
    }
    
    // Step 3: 保存完整响应
    yield return new StreamingMessageChunk { IsComplete = true, Message = aiResponse };
}

// 前端：逐个接收并更新UI
await foreach (var chunk in ConsultationFacade.SendPatientMessageWithStreamingAsync(...))
{
    if (chunk.IsAiChunk)
    {
        aiMessage.Content += chunk.Content;
        await InvokeAsync(() => StateHasChanged());  // 实时刷新UI
    }
}
```

---

## 🔌 SignalR 实时通信

### 群组机制

```
每个对话 (Conversation) 对应一个 SignalR 群组:
- 群组名: "conversation_{conversationId}"
- 群组成员: 患者、医生 (如果有)
- 消息广播: 发送给群组内所有成员

示例:
conversationId = "123e4567-e89b-12d3-a456-426614174000"
groupName = "conversation_123e4567-e89b-12d3-a456-426614174000"
```

### 事件类型

| 事件名称 | 触发时机 | 数据内容 |
|---------|---------|---------|
| `ReceiveMessage` | 收到新消息 | MessageId, Content, SenderId, SenderType |
| `UserTyping` | 用户正在输入 | ConversationId, UserName, UserRole |
| `UserStoppedTyping` | 用户停止输入 | ConversationId, UserName |
| `AiStatusUpdate` | AI状态变化 | Status ("thinking", "ready", "error") |
| `ConversationStatusChanged` | 对话状态变化 | Status (Active, Closed, etc.) |

---

## 📝 活动日志记录

系统会记录所有关键操作到 `ActivityLogs` 表：

| 操作 | Action | Details |
|------|--------|---------|
| 用户注册 | `user_registered` | role, conversationId |
| 用户登录 | `user_login` | ipAddress |
| 登录失败 | `login_failed` | email, ipAddress |
| 用户登出 | `user_logout` | ipAddress |
| 开始AI咨询 | `start_ai_consultation` | conversationId, initialMessage |
| 开始医生咨询 | `start_doctor_consultation` | conversationId, doctorId, doctorName |
| 发送消息 | `send_message` | conversationId, messageId, senderType |
| AI响应生成 | `ai_medical_consultation` | model, queryLength |
| 切换AI模型 | `switch_ai_model` | previousModel, newModel |

---

## 🔄 错误处理和容错机制

### AI响应失败处理

```csharp
// 自动切换到备用模型
await foreach (var chunk in GenerateAiResponseWithFallbackAsync(content))
{
    // 主模型失败时自动尝试其他模型
    if (primaryModelFailed)
    {
        _aiAssistantService.SwitchModel("fallback-model");
        // 重试
    }
}

// 所有模型都失败时返回友好提示
if (!success)
{
    fullResponse = "I apologize, but I'm experiencing technical difficulties...";
}
```

### SignalR 重连机制

```csharp
// 断线重连
SignalRService.OnReconnected += async () =>
{
    await SignalRService.RegisterUserAsync(currentPatientId);
    if (currentConversation != null)
    {
        await SignalRService.JoinConversationAsync(currentConversation.Id);
    }
};
```

---

## 📈 性能优化

### 1. 数据库查询优化

```csharp
// 使用 Include 预加载关联数据
await db.Conversations
    .Include(c => c.AssignedDoctor)
        .ThenInclude(d => d.DoctorProfile)
    .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
    .Where(c => c.PatientId == patientId)
    .ToListAsync();
```

### 2. 使用事务保证数据一致性

```csharp
using var transaction = await db.Database.BeginTransactionAsync();
try
{
    // 多个数据库操作
    db.Messages.Add(message);
    conversation.TotalMessages++;
    await db.SaveChangesAsync();
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### 3. 流式传输减少等待时间

```csharp
// 立即返回患者消息，不等待AI响应完成
yield return new StreamingMessageChunk { IsUserMessage = true, Message = patientMessage };

// 逐块返回AI响应，用户可以边生成边阅读
await foreach (var chunk in aiStream)
{
    yield return new StreamingMessageChunk { IsAiChunk = true, Content = chunk };
}
```

---

## 📚 相关文档

- [Blazor Server 官方文档](https://docs.microsoft.com/en-us/aspnet/core/blazor/)
- [SignalR 实时通信](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [BCrypt 密码哈希](https://github.com/BcryptNet/bcrypt.net)
- [OpenRouter API](https://openrouter.ai/docs)

---

## 🙋 常见问题 (FAQ)

### Q1: Cookie 过期后会发生什么？

**A:** Cookie过期后，`AuthStateService.InitializeAsync()` 无法读取到 `userId`，用户会被自动重定向到登录页面。

### Q2: 如果AI响应生成失败怎么办？

**A:** 系统会自动尝试其他备用AI模型，如果所有模型都失败，会返回友好的错误提示消息。

### Q3: SignalR 断线后如何恢复？

**A:** SignalR客户端会自动重连，重连成功后会重新注册用户并加入之前的对话群组。

### Q4: 如何添加新的AI模型？

**A:** 在 `AiAssistantService` 中添加新的策略实现，并在构造函数中注册即可：

```csharp
_strategies.Add("new-model", new NewModelStrategy(apiKey));
```

### Q5: 消息是如何实时同步的？

**A:** 使用SignalR的群组机制，每个对话对应一个群组，新消息会通过 `SendMessageToConversation` 广播给群组内所有在线成员。

---

## 📌 总结

本系统采用**分层架构**和**门面模式**，将复杂的业务逻辑封装在Facade层，使前端代码简洁易维护。主要特点：


✅ **安全性**: BCrypt密码哈希、Cookie存储、账户暂停机制  
✅ **实时性**: SignalR实时通信、流式AI响应  
✅ **可扩展性**: 策略模式支持动态切换AI模型  
✅ **可维护性**: 门面模式简化客户端调用  
✅ **容错性**: 自动模型切换、重连机制

---

**文档版本**: 1.0  
**最后更新**: 2026-06-16  
**作者**: AI Clinic Development Team
