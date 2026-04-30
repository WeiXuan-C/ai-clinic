# AI Clinic - 医疗咨询平台

## 项目概述

AI Clinic 是一个现代化的医疗咨询平台，采用 Blazor Server 架构，集成 Supabase 后端服务。系统支持患者与医生之间的实时沟通，提供完整的用户管理、咨询记录、文档管理和评价系统。

---

## 核心功能

### 1. 身份验证系统
- **OTP 邮箱验证登录**：无密码登录方式，通过邮箱接收 8 位验证码
- **角色分离注册**：
  - 患者（Patient）注册流程
  - 医生（Doctor）注册流程，需要执照号和专业信息
  - 管理员（Admin）权限管理
- **会话管理**：基于 Supabase Auth 的 JWT token 管理
- **本地数据库验证**：登录时检查本地用户表，确保用户已注册

### 2. 用户档案管理

#### 患者档案
- **基本信息**：姓名、出生日期、性别、地址
- **医疗信息**：血型、过敏史、慢性病、当前用药
- **紧急联系人**：姓名、电话
- **隐私设置**：
  - 数据共享开关
  - AI 分析开关
  - 活动跟踪开关

#### 医生档案
- **基本信息**：姓名、职称、执照号
- **专业信息**：
  - 主要专科（Primary Specialization）
  - 子专科（Sub-specializations）
  - 医疗专长标签（Medical Expertise Tags）
  - 症状专长（Symptoms Expertise）
  - 治疗疾病（Conditions Treated）
  - 执行手术（Procedures Performed）
  - 治疗年龄组（Age Groups Treated）
  - 语言能力（Languages Spoken）
- **可用性管理**：
  - 状态：available、busy、offline
  - 工作时间配置（JSON 格式）
  - 当前活跃会话数
- **绩效指标**：
  - 总咨询数
  - 平均评分（0-5 星）
  - 总评价数
- **认证状态**：
  - 是否已验证
  - 是否接受新患者

#### 管理员档案
- **权限管理**：
  - 用户管理权限
  - AI 系统管理权限
  - 医生管理权限
  - 工单管理权限
  - 权限分配权限

### 3. 咨询系统

#### 会话管理
- **会话创建**：患者发起与医生的会话
- **会话状态**：active、closed、archived、deactive
- **会话信息**：
  - 标题
  - 初始症状（数组）
  - AI 建议的专科
  - 分配的医生
  - 咨询状态（pending、in_progress、completed）
  - 诊断完成标志
  - 处方生成标志
  - 所需专科
  - AI 置信度分数
- **统计信息**：
  - 总消息数
  - AI 消息数
  - 医生消息数
  - 最后消息时间

#### 消息系统
- **消息类型**：patient、doctor、ai、system
- **消息内容**：
  - 文本内容
  - AI 模型信息（如使用 AI）
  - AI 置信度分数
  - 文档引用（UUID 数组）
- **已读状态**：
  - 是否已读
  - 阅读时间
- **实时更新**：通过 SignalR 实现消息实时推送

### 4. 医生目录与匹配

#### 医生搜索功能
- **筛选条件**：
  - 按专科筛选
  - 按可用性筛选
  - 关键词搜索
- **排序选项**：
  - 按评分排序
  - 按经验排序
  - 按可用性排序

#### 医生信息展示
- 医生头像
- 姓名与职称
- 专科与子专科
- 经验年限
- 评分与评价数
- 实时可用性状态
- 咨询费用
- 下次可用时间
- 认证徽章

#### 智能匹配
- 根据患者症状推荐专科
- 根据专科查找可用医生
- 考虑医生当前工作负载
- 优先推荐高评分医生

### 5. 文档管理

#### 文档上传
- **支持类型**：
  - 医疗记录（medical_record）
  - 化验结果（lab_result）
  - 处方（prescription）
  - 图像（image）
  - 其他（other）
- **文档信息**：
  - 文件名
  - 文件大小
  - MIME 类型
  - 上传时间
  - 描述
  - 标签

#### AI 处理
- 文档可被标记为"已处理"
- 提取文本内容用于 AI 分析
- 文档可在消息中被引用

### 6. 评价系统

#### 医生评分
- **总体评分**：1-5 星（必填）
- **分项评分**：
  - 专业性评分
  - 沟通能力评分
  - 知识水平评分
  - 响应时间评分
- **评价文本**：患者可撰写详细评价
- **关联会话**：每个评价关联一次咨询

#### 评分统计
- 自动计算医生平均评分
- 自动更新总评价数
- 触发器自动维护评分数据

### 7. 支持工单系统

#### 工单管理
- **工单创建**：
  - 主题（必填）
  - 详细描述（必填）
  - 分类：technical、billing、medical、account、other
  - 优先级：low、medium、high、urgent
- **工单状态**：
  - open：新建工单
  - in_progress：处理中
  - resolved：已解决
  - closed：已关闭
- **时间戳**：
  - 创建时间
  - 更新时间
  - 解决时间
  - 关闭时间

#### 工单附件
- 支持文件上传
- 记录文件名、URL、大小、MIME 类型

#### 工单响应
- 管理员可回复工单
- 支持内部备注（仅管理员可见）
- 记录响应时间和响应者

---

## 技术架构

### 后端技术栈
- **框架**：ASP.NET Core 8.0 (Blazor Server)
- **语言**：C# 12
- **数据库**：Supabase (PostgreSQL)
- **身份验证**：Supabase Auth (OTP 邮箱验证)
- **实时通信**：SignalR (Blazor Server 内置)
- **HTTP 客户端**：自定义 SupabaseHttpClient（直接 REST API 调用）
- **AI 集成**：Azure OpenAI（已配置，待完全实现）

### 前端技术栈
- **框架**：Blazor Server Components
- **渲染模式**：Interactive Server Render Mode（prerender: false）
- **UI 库**：
  - MudBlazor 9.3.0
  - 自定义 Stitch Design System
- **状态管理**：Scoped State Pattern（每个用户独立状态）
- **图标库**：Lucide Icons
- **样式**：自定义 CSS + Stitch Design System

### 依赖包
```xml
<PackageReference Include="MudBlazor" Version="9.3.0" />
<PackageReference Include="supabase-csharp" Version="0.16.2" />
<PackageReference Include="Microsoft.AspNetCore.SignalR.Client" Version="8.0.0" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="8.0.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.0.1" />
<PackageReference Include="Azure.AI.OpenAI" Version="2.1.0" />
```

### 架构模式

#### 分层架构
```
UI Layer (Blazor Pages/Components)
    ↓
Controller Layer (Facade Pattern)
    ↓
Service Layer (Business Logic)
    ↓
State Layer (State Management)
    ↓
DAO Layer (Adapter Pattern)
    ↓
Database (Supabase PostgreSQL)
```

#### 各层职责

**1. UI Layer（UI/Pages, UI/Components）**
- Blazor Razor 组件
- 用户交互处理
- 数据绑定与展示
- 路由管理

**2. Controller Layer（Controller/）**
- **设计模式**：Facade Pattern
- **职责**：简化业务逻辑调用，提供统一接口
- **示例**：
  - `AuthController`：身份验证操作
  - `ConversationController`：会话管理
  - `MessageController`：消息操作
  - `DoctorProfileController`：医生档案管理
  - `PatientProfileController`：患者档案管理

**3. Service Layer（Services/）**
- **职责**：业务逻辑处理，协调 DAO 和 State
- **示例**：
  - `AuthService`：身份验证逻辑
  - `ConversationService`：会话业务逻辑
  - `MessageService`：消息业务逻辑
  - `DoctorProfileService`：医生档案业务逻辑
  - `PatientProfileService`：患者档案业务逻辑

**4. State Layer（UI/State/）**
- **职责**：状态管理，缓存和 UI 更新通知
- **生命周期**：Scoped（每个用户独立）
- **示例**：
  - `AuthState`：当前用户状态
  - `ConversationState`：会话列表缓存
  - `MessageState`：消息列表缓存
  - `DoctorProfileState`：医生档案缓存
  - `PatientProfileState`：患者档案缓存

**5. DAO Layer（DAOs/）**
- **设计模式**：Adapter Pattern + Repository Pattern
- **职责**：封装 Supabase 数据访问
- **示例**：
  - `UserDAO`：用户数据访问
  - `ConversationDAO`：会话数据访问
  - `MessageDAO`：消息数据访问
  - `DoctorProfileDAO`：医生档案数据访问
  - `PatientProfileDAO`：患者档案数据访问

**6. Interface Layer（Interface/）**
- **职责**：定义实体接口和仓储接口
- **示例**：
  - `IUser`, `IUserRepository`
  - `IConversation`, `IConversationRepository`
  - `IMessage`, `IMessageRepository`
  - `IDoctorProfile`, `IDoctorRepository`
  - `IPatientProfile`, `IPatientProfileRepository`

### 设计模式应用

#### 1. Facade Pattern（外观模式）
- **应用位置**：Controller 层
- **目的**：简化复杂的业务逻辑调用
- **示例**：
```csharp
public class AuthController
{
    public async Task<(bool Success, string Message)> InitiateLoginAsync(string email)
    {
        // 简化的登录流程，隐藏内部复杂性
    }
}
```

#### 2. Adapter Pattern（适配器模式）
- **应用位置**：DAO 层
- **目的**：将 Supabase API 适配为应用程序接口
- **示例**：
```csharp
public class UserDAO : IUserRepository
{
    // 将 Supabase REST API 适配为 Repository 接口
}
```

#### 3. Repository Pattern（仓储模式）
- **应用位置**：Interface 层定义，DAO 层实现
- **目的**：抽象数据访问逻辑
- **示例**：
```csharp
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User> AddAsync(User entity);
    Task<User> UpdateAsync(User entity);
    Task<bool> DeleteAsync(Guid id);
}
```

#### 4. Dependency Injection（依赖注入）
- **应用位置**：DependencyInjection.cs
- **生命周期**：
  - Scoped：所有服务、DAO、Controller、State
  - 原因：Blazor Server 需要每个用户独立状态，防止数据泄露

---

## 数据库设计

### 核心表结构

#### users（用户表）
- 存储所有用户的基本信息
- 角色：patient、doctor、admin
- 隐私设置：数据共享、AI 分析、活动跟踪
- 账户状态：激活、停用

#### patient_profiles（患者档案表）
- 关联 users 表（user_id）
- 医疗信息：血型、过敏、慢性病、用药
- 紧急联系人信息

#### doctor_profiles（医生档案表）
- 关联 users 表（user_id）
- 执照信息
- 专业信息（多个数组字段）
- 可用性状态
- 绩效指标

#### admin_profiles（管理员档案表）
- 关联 users 表（user_id）
- 权限配置

#### conversations（会话表）
- 患者与医生的会话记录
- 状态跟踪
- 统计信息

#### messages（消息表）
- 会话中的消息记录
- 发送者类型
- AI 相关信息

#### documents（文档表）
- 会话中上传的文档
- AI 处理状态

#### doctor_ratings（医生评分表）
- 患者对医生的评价
- 多维度评分

#### support_tickets（支持工单表）
- 用户提交的支持请求
- 状态跟踪

#### support_ticket_attachments（工单附件表）
- 工单相关文件

#### support_ticket_responses（工单响应表）
- 管理员回复记录

### 数据库触发器

#### 1. 自动更新 updated_at
- 所有主要表都有 `updated_at` 触发器
- 在 UPDATE 操作时自动更新时间戳

#### 2. 更新会话最后消息时间
- 当插入新消息时，自动更新会话的 `last_message_at`
- 自动增加消息计数器

#### 3. 更新医生平均评分
- 当插入或更新评分时，自动重新计算医生的平均评分
- 自动更新总评价数

#### 4. 跟踪医生活跃会话数
- 当分配医生或关闭会话时，自动更新医生的活跃会话计数

---

## 页面结构

### 认证页面（UI/Pages/Auth/）
- **Signin.razor**：登录页面
  - OTP 发送
  - OTP 验证
  - 错误处理
- **Signup.razor**：注册页面
  - 角色选择
  - 信息填写
  - OTP 验证

### 患者页面（UI/Pages/Patient/）
- **Dashboard.razor**：患者仪表板
  - 欢迎信息
  - 最近咨询记录
  - 预约信息
  - 健康指标
  - AI 洞察卡片
- **Consultation.razor**：咨询页面
  - 会话历史侧边栏
  - 医生目录（可搜索、筛选）
  - 聊天界面
  - 消息输入区
  - 文档上传
  - 上下文侧边栏
- **Profile.razor**：个人资料
- **Records.razor**：医疗记录
- **Settings.razor**：设置
- **Support.razor**：支持工单

### 通用页面（UI/Pages/General/）
- **About.razor**：关于页面
- **Consultation.razor**：通用咨询页面
- **Doctors.razor**：医生列表页面

### 布局组件（UI/Components/Layout/）
- **MainLayout.razor**：主布局
- **SidebarLayout.razor**：侧边栏布局
- **EmptyLayout.razor**：空布局（用于登录页）

---

## 配置文件

### appsettings.json
```json
{
  "Supabase": {
    "Url": "https://your-project.supabase.co",
    "Key": "your-anon-key"
  },
  "Jwt": {
    "Key": "your-secret-key-min-32-characters-long",
    "Issuer": "ai-clinic",
    "Audience": "ai-clinic-users"
  },
  "AzureOpenAI": {
    "Endpoint": "https://your-resource.openai.azure.com/",
    "ApiKey": "your-api-key",
    "DeploymentName": "gpt-4"
  }
}
```

### .env.example
提供环境变量模板

---

## 开发指南

### 环境要求
- .NET 8.0 SDK
- Visual Studio 2022 或 VS Code
- Supabase 账户
- Azure OpenAI 账户（可选）

### 本地运行
1. 克隆仓库
2. 配置 `appsettings.json` 中的 Supabase 连接信息
3. 运行数据库迁移脚本（`Database/database-setup.sql`）
4. 运行项目：
   ```bash
   dotnet run
   ```
5. 访问 `https://localhost:5001`

### 项目结构
```
ai-clinic/
├── Controller/          # 控制器层（Facade）
├── Services/           # 服务层（业务逻辑）
├── DAOs/              # 数据访问层（Adapter）
├── Interface/         # 接口定义
├── UI/
│   ├── Components/    # Blazor 组件
│   ├── Pages/        # 页面
│   └── State/        # 状态管理
├── Database/         # 数据库脚本
├── wwwroot/         # 静态资源
│   └── css/         # 样式文件
├── Program.cs       # 应用程序入口
├── DependencyInjection.cs  # DI 配置
└── appsettings.json # 配置文件
```

---

## 待实现功能

### 1. AI 助手集成
- AI 聊天机器人（Azure OpenAI）
- 文档分析与提取
- 症状评估
- 专科推荐

### 2. 实时通知
- Supabase Realtime 集成
- 新消息通知
- 会话状态变更通知

### 3. 视频通话
- WebRTC 集成
- 医生-患者视频咨询

### 4. 医疗记录导出
- PDF 生成
- 数据导出功能

### 5. 预约系统
- 医生日程管理
- 预约创建与管理
- 提醒通知

### 6. 支付集成
- 咨询费用支付
- 支付历史记录

---

## 安全考虑

### 身份验证
- OTP 邮箱验证（无密码）
- JWT token 管理
- 会话过期处理

### 数据隐私
- 用户隐私设置
- 数据共享控制
- HIPAA 合规考虑（待完善）

### 访问控制
- 基于角色的访问控制（RBAC）
- 患者只能访问自己的数据
- 医生只能访问分配的会话
- 管理员权限分级

---

## 许可证

[待定]

---

## 联系方式

[待定]
