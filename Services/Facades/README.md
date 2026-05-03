# Facade Pattern 实现说明

## 🎭 什么是 Facade Pattern（外观模式）？

Facade Pattern 是 Gang of Four 设计模式之一，属于**结构型模式**。它为复杂的子系统提供一个统一的高层接口，使子系统更容易使用。

### 核心思想
- **简化接口**：客户端不需要了解子系统的复杂性
- **降低耦合**：客户端与子系统之间的依赖关系减少
- **提高可维护性**：子系统的变化不会影响客户端代码

## 📁 项目中的 Facade 实现

### ConsultationFacade（咨询外观类）

#### 协调的子系统
```
ConsultationFacade
├── ConversationService    (管理对话)
├── MessageService         (管理消息)
├── DoctorProfileService   (管理医生信息)
└── ActivityLogService     (记录活动日志)
```

#### 提供的简化接口

##### 1. 创建咨询会话
```csharp
// ❌ 不使用 Facade - 客户端需要协调多个服务
var conversation = await conversationService.CreateAiConversationAsync(patientId);
await activityLogService.LogAsync(new ActivityLog { ... });
var messages = await messageService.GetByConversationIdAsync(conversation.Id);

// ✅ 使用 Facade - 一行代码完成所有操作
var session = await consultationFacade.StartAiConsultationAsync(patientId);
```

##### 2. 发送消息并获取 AI 响应
```csharp
// ❌ 不使用 Facade - 需要手动处理 AI 响应逻辑
var patientMessage = await messageService.CreatePatientMessageAsync(...);
if (conversation.AssignedDoctorId == null) {
    var aiResponse = await GenerateAiResponse(...);
    var aiMessage = await messageService.CreateAiMessageAsync(...);
}

// ✅ 使用 Facade - 自动处理 AI 响应
var result = await consultationFacade.SendPatientMessageAsync(conversationId, patientId, content);
// result 包含患者消息和 AI 响应（如果适用）
```

##### 3. 获取完整会话信息
```csharp
// ❌ 不使用 Facade - 需要多次调用
var conversation = await conversationService.GetByIdAsync(conversationId);
var messages = await messageService.GetByConversationIdAsync(conversationId);
await messageService.MarkConversationAsReadAsync(conversationId, ...);
var doctorProfile = await doctorProfileService.GetByUserIdAsync(doctorId);

// ✅ 使用 Facade - 一次调用获取所有数据
var session = await consultationFacade.GetConsultationSessionAsync(conversationId, userId, userRole);
// session 包含对话、消息、医生信息，并自动标记已读
```

## 🎯 使用场景

### 适合使用 Facade 的情况
1. **复杂子系统**：需要协调多个服务完成一个业务操作
2. **频繁操作**：同样的操作序列在多处重复
3. **业务流程**：有明确的业务流程需要封装
4. **降低耦合**：希望客户端代码与底层实现解耦

### 本项目中的应用
- ✅ **ConsultationFacade**：协调对话、消息、医生、日志等服务
- ✅ **AuthFacade**：协调用户认证、权限验证、会话管理
- ✅ **PatientFacade**：协调患者相关的所有操作
- ✅ **DoctorFacade**：协调医生相关的所有操作
- ✅ **AdminFacade**：协调管理员相关的所有操作

## 📊 Facade vs Service 的区别

| 特性 | Service | Facade |
|------|---------|--------|
| **职责** | 单一领域的数据操作 | 协调多个服务完成业务流程 |
| **依赖** | 只依赖 DbClient | 依赖多个 Service |
| **粒度** | 细粒度（CRUD） | 粗粒度（业务流程） |
| **示例** | `CreateMessageAsync()` | `SendPatientMessageAsync()` (创建消息 + 生成 AI 响应 + 记录日志) |

## 🔧 如何添加新的 Facade 方法

### 步骤 1: 识别业务流程
```
业务需求：患者关闭咨询会话
涉及操作：
1. 更新对话状态为 Closed
2. 记录活动日志
3. 发送通知给医生（如果有）
```

### 步骤 2: 在 Facade 中实现
```csharp
public async Task CloseConsultationAsync(Guid conversationId, Guid userId)
{
    // 1. 更新对话状态
    await _conversationService.UpdateStatusAsync(conversationId, ConversationStatus.Closed);

    // 2. 记录活动日志
    await _activityLogService.LogAsync(new ActivityLog
    {
        UserId = userId,
        Action = "close_consultation",
        EntityType = "conversation",
        EntityId = conversationId
    });

    // 3. 发送通知（如果需要）
    // await _notificationService.NotifyDoctorAsync(...);
}
```

### 步骤 3: 在客户端使用
```csharp
// 简单调用，无需关心内部实现
await consultationFacade.CloseConsultationAsync(conversationId, userId);
```

## 🎓 设计模式最佳实践

### ✅ 好的做法
1. **单一职责**：每个 Facade 负责一个业务领域
2. **清晰命名**：方法名应该反映业务意图（如 `StartAiConsultation` 而不是 `CreateConversation`）
3. **返回 DTO**：返回专门的数据传输对象，而不是直接返回实体
4. **异常处理**：在 Facade 层统一处理异常
5. **日志记录**：在 Facade 层统一记录业务日志

### ❌ 避免的做法
1. **过度封装**：不要把简单的单一操作也包装成 Facade
2. **业务逻辑泄漏**：Facade 应该协调，而不是实现业务逻辑
3. **循环依赖**：Facade 之间不应该相互依赖
4. **状态管理**：Facade 应该是无状态的

## 📚 参考资料

- [Gang of Four - Facade Pattern](https://refactoring.guru/design-patterns/facade)
- [C# Design Patterns](https://www.dofactory.com/net/facade-design-pattern)
- 项目示例代码：`codeExample/facadeExampleCode.cs`
