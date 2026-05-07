# SignalR Real-Time Messaging Debug Guide

## 🔍 调试日志说明

现在 SignalR Hub 已经添加了详细的调试日志，可以帮助你追踪消息流向。

## 📊 日志格式

所有 SignalR 相关日志都以 `[SignalR Hub]` 开头，并使用表情符号标识不同类型的事件：

- 🔌 **连接事件**：客户端连接/断开
- ✅ **成功操作**：用户注册、加入群组等
- 👥 **群组操作**：加入/离开对话群组
- 📤 **消息发送**：向群组发送消息
- ⌨️ **输入状态**：用户正在输入/停止输入
- ⚠️ **警告**：无效的参数等
- ❌ **错误**：连接失败等

## 🔄 完整的消息流程

### Patient 发送消息给 Doctor

```
1. Patient 端
   [DEBUG] SendMessage Started
   [DEBUG] Message: Hello doctor
   [DEBUG] Is AI Conversation: False
   ↓
2. ConsultationFacade
   [FACADE] Patient message created - ID: xxx
   ↓
3. SignalR Hub
   [SignalR Hub] 📤 Sending message to group: conversation_xxx
   [SignalR Hub] Message ID: xxx
   [SignalR Hub] Sender Type: Patient
   [SignalR Hub] Content Length: 12
   [SignalR Hub] ✅ Message sent to group: conversation_xxx
   ↓
4. Doctor 端 (如果已连接并加入群组)
   [SignalR] Message received: xxx in conversation xxx
   [SignalR] Adding message to UI
```

### Doctor 发送消息给 Patient

```
1. Doctor 端
   [DEBUG] Sending message...
   ↓
2. DoctorFacade
   [FACADE] Doctor message created - ID: xxx
   ↓
3. SignalR Hub
   [SignalR Hub] 📤 Sending message to group: conversation_xxx
   [SignalR Hub] Message ID: xxx
   [SignalR Hub] Sender Type: Doctor
   [SignalR Hub] Content Length: 15
   [SignalR Hub] ✅ Message sent to group: conversation_xxx
   ↓
4. Patient 端 (如果已连接并加入群组)
   [SignalR] Message received: xxx in conversation xxx
   [SignalR] Adding message to UI
```

## 🔍 如何调试

### 步骤 1：检查连接状态

打开浏览器控制台，查找：

```
[SignalR] Initializing connection to: http://localhost:5269/consultationHub
[SignalR Hub] 🔌 Client connected: xxx
[SignalR Hub] ✅ User {userId} registered with connection xxx
```

**如果看不到这些日志：**
- ❌ SignalR 连接失败
- 检查 `Program.cs` 中的 hub 映射
- 检查浏览器网络面板是否有 404 错误

### 步骤 2：检查是否加入群组

当加载对话时，应该看到：

```
[SignalR] Joined conversation: {conversationId}
[SignalR Hub] 👥 Connection xxx joined group: conversation_{conversationId}
[SignalR Hub] User {userId} is now in conversation {conversationId}
```

**如果看不到这些日志：**
- ❌ 没有加入对话群组
- 消息会发送，但你收不到
- 检查 `LoadConversation()` 方法是否调用了 `JoinConversationAsync()`

### 步骤 3：发送消息

当发送消息时，应该看到：

**发送方（Patient 或 Doctor）：**
```
[DEBUG] SendMessage Started
[DEBUG] Message: test message
[FACADE] Patient/Doctor message created - ID: xxx
[SignalR Hub] 📤 Sending message to group: conversation_xxx
[SignalR Hub] Message ID: xxx
[SignalR Hub] Sender Type: Patient/Doctor
[SignalR Hub] Content Length: 12
[SignalR Hub] ✅ Message sent to group: conversation_xxx
```

**接收方（另一端）：**
```
[SignalR] Message received: xxx in conversation xxx
[SignalR] Adding message to UI
```

**如果发送方看到日志，但接收方没有：**
- ❌ 接收方没有加入群组
- ❌ 接收方的 SignalR 事件处理器没有正确注册
- ❌ 接收方的连接已断开

### 步骤 4：检查消息是否添加到 UI

接收方应该看到：

```
[SignalR] Message received: xxx in conversation xxx
[SignalR] Adding message to UI
```

然后 UI 应该更新显示新消息。

**如果看到 "Message received" 但 UI 没有更新：**
- ❌ `StateHasChanged()` 没有被调用
- ❌ `InvokeAsync()` 没有正确使用
- ❌ 消息被过滤掉了（例如：发送者 ID 匹配检查）

## 🐛 常见问题排查

### 问题 1：SignalR 连接失败 (404)

**症状：**
```
fail: ai_clinic.Services.SignalRConsultationService[0]
Failed to initialize SignalR connection
System.Net.Http.HttpRequestException: Response status code does not indicate success: 404 (Not Found).
```

**原因：**
- Hub URL 不匹配

**解决方案：**
1. 检查 `Program.cs`：
   ```csharp
   app.MapHub<ConsultationHub>("/consultationHub");
   ```

2. 检查客户端连接 URL：
   ```csharp
   var hubUrl = $"{baseUrl}/consultationHub";  // 必须匹配
   ```

### 问题 2：消息发送了但收不到

**症状：**
```
[SignalR Hub] ✅ Message sent to group: conversation_xxx
```
但接收方没有收到。

**可能原因：**

1. **接收方没有加入群组**
   - 检查是否调用了 `JoinConversation()`
   - 查找日志：`[SignalR Hub] 👥 Connection xxx joined group`

2. **接收方连接已断开**
   - 检查是否有断开日志：`[SignalR Hub] ❌ Client disconnected`
   - 检查浏览器网络面板

3. **事件处理器没有注册**
   - 检查 `InitializeSignalR()` 是否被调用
   - 检查 `OnMessageReceived` 事件是否订阅

### 问题 3：Patient 和 Doctor 都看不到对方的消息

**症状：**
- Patient 发送消息，Doctor 看不到
- Doctor 发送消息，Patient 看不到

**调试步骤：**

1. **打开两个浏览器窗口**
   - 窗口 1：Patient 登录
   - 窗口 2：Doctor 登录

2. **两个窗口都打开控制台**

3. **Patient 窗口发送消息，检查日志：**
   ```
   Patient 窗口：
   [DEBUG] SendMessage Started
   [SignalR Hub] 📤 Sending message to group: conversation_xxx
   
   Doctor 窗口：
   [SignalR] Message received: xxx  ← 应该看到这个
   ```

4. **如果 Doctor 窗口没有收到：**
   - 检查 Doctor 是否加入了群组
   - 检查 Doctor 的 SignalR 连接状态
   - 检查两个窗口的 conversationId 是否相同

### 问题 4：AI 消息没有实时显示

**重要：** AI 消息不使用 SignalR！

AI 消息使用 **IAsyncEnumerable 流式传输**，不是 SignalR。

**正确的流程：**
```
Patient 发送消息
  ↓
AI 服务流式返回响应
  ↓
每个 chunk 触发 StateHasChanged()
  ↓
UI 实时更新
```

**调试 AI 流式传输：**
```
[DEBUG] Received chunk #1: IsAiChunk=True
[DEBUG] AI content updated, length: 50
[DEBUG] Received chunk #2: IsAiChunk=True
[DEBUG] AI content updated, length: 120
...
```

如果看不到多个 chunks，说明流式传输有问题。

## 📝 完整的调试检查清单

### Patient 端

- [ ] SignalR 连接成功
  ```
  [SignalR] Initializing connection to: http://localhost:5269/consultationHub
  [SignalR Hub] 🔌 Client connected: xxx
  ```

- [ ] 用户注册成功
  ```
  [SignalR Hub] ✅ User {userId} registered with connection xxx
  ```

- [ ] 加入对话群组
  ```
  [SignalR Hub] 👥 Connection xxx joined group: conversation_xxx
  ```

- [ ] 发送消息成功
  ```
  [DEBUG] SendMessage Started
  [SignalR Hub] 📤 Sending message to group: conversation_xxx
  [SignalR Hub] ✅ Message sent to group: conversation_xxx
  ```

- [ ] 接收 Doctor 消息
  ```
  [SignalR] Message received: xxx in conversation xxx
  ```

### Doctor 端

- [ ] SignalR 连接成功
  ```
  [SignalR] Initializing connection to: http://localhost:5269/consultationHub
  [SignalR Hub] 🔌 Client connected: xxx
  ```

- [ ] 用户注册成功
  ```
  [SignalR Hub] ✅ User {userId} registered with connection xxx
  ```

- [ ] 加入对话群组
  ```
  [SignalR Hub] 👥 Connection xxx joined group: conversation_xxx
  ```

- [ ] 发送消息成功
  ```
  [SignalR Hub] 📤 Sending message to group: conversation_xxx
  [SignalR Hub] ✅ Message sent to group: conversation_xxx
  ```

- [ ] 接收 Patient 消息
  ```
  [SignalR] Message received: xxx in conversation xxx
  ```

## 🔧 测试步骤

### 完整的端到端测试

1. **启动应用**
   ```bash
   dotnet watch run
   ```

2. **打开两个浏览器窗口**
   - 窗口 1：http://localhost:5269/auth/signin (Patient)
   - 窗口 2：http://localhost:5269/auth/signin (Doctor)

3. **两个窗口都打开开发者工具控制台**

4. **Patient 窗口：创建 Doctor 咨询**
   - 点击 "New" → 选择 Doctor → 创建对话
   - 检查控制台日志

5. **Doctor 窗口：打开对话**
   - 导航到 /doctor/consultation
   - 选择刚创建的对话
   - 检查控制台日志

6. **Patient 发送消息**
   - 输入 "Hello doctor"
   - 点击发送
   - **Patient 窗口**应该看到：
     ```
     [DEBUG] SendMessage Started
     [SignalR Hub] 📤 Sending message to group: conversation_xxx
     ```
   - **Doctor 窗口**应该看到：
     ```
     [SignalR] Message received: xxx
     ```
   - **Doctor UI** 应该显示新消息

7. **Doctor 回复消息**
   - 输入 "Hello patient"
   - 点击发送
   - **Doctor 窗口**应该看到：
     ```
     [SignalR Hub] 📤 Sending message to group: conversation_xxx
     ```
   - **Patient 窗口**应该看到：
     ```
     [SignalR] Message received: xxx
     ```
   - **Patient UI** 应该显示新消息

## 📊 预期的完整日志流

### Patient 发送 "Hello" 给 Doctor

```
=== Patient 窗口 ===
[DEBUG] SendMessage Started
[DEBUG] Message: Hello
[DEBUG] Is AI Conversation: False
[FACADE] Patient message created - ID: abc123
[SignalR Hub] 📤 Sending message to group: conversation_xyz
[SignalR Hub] Message ID: abc123
[SignalR Hub] Sender Type: Patient
[SignalR Hub] Content Length: 5
[SignalR Hub] ✅ Message sent to group: conversation_xyz
[DEBUG] Streaming completed, total chunks: 1

=== Doctor 窗口 ===
[SignalR] Message received: abc123 in conversation xyz
[SignalR] Adding message to UI
```

### Doctor 回复 "Hi there" 给 Patient

```
=== Doctor 窗口 ===
[DEBUG] Sending message...
[FACADE] Doctor message created - ID: def456
[SignalR Hub] 📤 Sending message to group: conversation_xyz
[SignalR Hub] Message ID: def456
[SignalR Hub] Sender Type: Doctor
[SignalR Hub] Content Length: 8
[SignalR Hub] ✅ Message sent to group: conversation_xyz

=== Patient 窗口 ===
[SignalR] Message received: def456 in conversation xyz
[SignalR] Adding message to UI
```

## 🎯 下一步

如果按照这个指南检查后仍然有问题：

1. **复制完整的控制台日志**
2. **注明哪一步失败了**
3. **提供两端（Patient 和 Doctor）的日志**

这样可以精确定位问题所在！
