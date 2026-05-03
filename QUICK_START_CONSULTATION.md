# 咨询系统快速启动指南

## 🚀 快速开始

### 1. 确保应用程序已停止
```bash
# 如果应用正在运行，先停止它
# 在 Windows 上按 Ctrl+C 或关闭终端
```

### 2. 编译项目
```bash
dotnet build
```

### 3. 运行应用
```bash
dotnet run
```

### 4. 访问咨询页面
```
浏览器打开: https://localhost:5001/patient/consultation
```

## 📋 功能测试清单

### ✅ 测试 AI 咨询
1. 登录为患者账户
2. 访问 `/patient/consultation`
3. 点击 "+ New" 按钮
4. 选择 "AI Assistant"
5. 点击 "Create Conversation"
6. 在输入框输入消息，例如："I have a headache"
7. 按 Enter 或点击发送按钮
8. 等待 AI 响应（约 1.5 秒）
9. 验证消息已保存到数据库

### ✅ 测试医生咨询
1. 点击 "+ New" 按钮
2. 选择 "Human Doctor"
3. 从列表中选择一位医生
4. 点击 "Create Conversation"
5. 发送消息
6. 验证对话已创建

### ✅ 测试对话切换
1. 创建多个对话
2. 在左侧边栏点击不同的对话
3. 验证消息正确加载
4. 验证未读消息标记

## 🗄️ 数据库验证

### 查看对话记录
```bash
# 使用 SQLite 浏览器或命令行
sqlite3 ai-clinic.db

# 查询对话
SELECT id, patient_id, assigned_doctor_id, title, status, total_messages 
FROM conversations 
ORDER BY created_at DESC;

# 查询消息
SELECT id, conversation_id, sender_type, content, created_at 
FROM messages 
ORDER BY created_at DESC 
LIMIT 10;
```

## 🎭 Facade Pattern 使用示例

### 在其他页面使用 ConsultationFacade

```csharp
// 注入 Facade
[Inject] private ConsultationFacade ConsultationFacade { get; set; } = null!;

// 创建 AI 咨询
var session = await ConsultationFacade.StartAiConsultationAsync(patientId);

// 发送消息
var result = await ConsultationFacade.SendPatientMessageAsync(
    conversationId, 
    patientId, 
    "I have chest pain"
);

// 获取患者的所有咨询
var conversations = await ConsultationFacade.GetPatientConsultationsAsync(patientId);

// 关闭咨询
await ConsultationFacade.CloseConsultationAsync(conversationId, userId);
```

## 🔧 常见问题

### Q: 编译时提示文件被锁定
**A:** 应用程序正在运行，先停止它再编译。

### Q: AI 响应不显示
**A:** 检查浏览器控制台是否有 JavaScript 错误，确保 `isTyping` 状态正确更新。

### Q: 消息没有保存到数据库
**A:** 检查 `ai-clinic.db` 文件权限，确保应用有写入权限。

### Q: 医生列表为空
**A:** 需要先在数据库中创建医生账户和 doctor_profiles 记录。

## 📊 测试数据

### 创建测试医生（SQL）
```sql
-- 插入医生用户
INSERT INTO users (id, email, password_hash, role, is_active)
VALUES (
    '11111111-1111-1111-1111-111111111111',
    'doctor1@test.com',
    '$2a$11$hashed_password_here',
    'doctor',
    1
);

-- 插入医生档案
INSERT INTO doctor_profiles (
    id, user_id, full_name, license_number, 
    primary_specialization, years_of_experience,
    is_active, is_accepting_patients
)
VALUES (
    '22222222-2222-2222-2222-222222222222',
    '11111111-1111-1111-1111-111111111111',
    'Dr. John Smith',
    'MD12345',
    'Cardiology',
    10,
    1,
    1
);
```

## 🎯 下一步

1. **集成真实 AI API**
   - 替换 `GenerateSimpleAiResponse` 方法
   - 调用 OpenAI、Claude 或其他 AI 服务

2. **添加文件上传**
   - 实现医疗文档上传
   - 图片识别和分析

3. **实时通知**
   - 使用 SignalR 实现实时消息推送
   - 医生回复时通知患者

4. **移动端适配**
   - 优化响应式设计
   - 添加触摸手势支持

## 📚 相关文档

- `CONSULTATION_IMPLEMENTATION.md` - 完整实现文档
- `Services/Facades/README.md` - Facade Pattern 说明
- `.kiro/steering/design-patterns-knowledge.md` - 设计模式知识库

## 💡 提示

- 使用 Facade Pattern 可以大大简化客户端代码
- 所有业务逻辑都在 Facade 层，UI 层只负责展示
- 遵循单一职责原则，每个服务只负责一个领域
- 使用 DTO 传输数据，避免直接暴露实体类

## ✨ 享受编码！

如有问题，请参考完整实现文档或设计模式知识库。
