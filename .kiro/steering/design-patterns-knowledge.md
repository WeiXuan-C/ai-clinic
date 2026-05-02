---
inclusion: auto
---

# 设计模式知识库

本项目使用了四种经典的 Gang of Four 设计模式。在编写代码时，请参考这些模式的实现方式和最佳实践。

## 认证系统说明

**重要**: 本项目使用**密码认证**系统，不使用 OTP (一次性密码)。

### 密码存储规范
- 用户密码必须使用 **BCrypt** 或 **PBKDF2** 进行哈希处理
- 永远不要存储明文密码
- `User` 模型中的 `PasswordHash` 字段存储哈希后的密码
- 推荐使用 `BCrypt.Net-Next` NuGet 包进行密码哈希

### 密码验证流程
1. 用户提交邮箱和密码
2. 根据邮箱查询用户
3. 使用 BCrypt.Verify() 验证密码
4. 验证成功后更新 `LastLoginAt` 时间戳
5. 返回用户信息或生成 JWT token

### 示例代码模式
```csharp
// 注册时哈希密码
string passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

// 登入时验证密码
bool isValid = BCrypt.Net.BCrypt.Verify(plainPassword, user.PasswordHash);
```

---

## 1. Adapter Pattern (适配器模式)

**目的**: 将一个类的接口转换成客户期望的另一个接口，使原本接口不兼容的类可以一起工作。

**实现要点**:
- **Target (目标类)**: 定义客户端使用的接口 (如 `Compound` 类)
- **Adapter (适配器)**: 继承 Target 并包装 Adaptee，转换接口 (如 `RichCompound` 类)
- **Adaptee (被适配者)**: 已存在的类，需要被适配 (如 `ChemicalDatabank` 类)

**使用场景**:
- 需要使用现有类，但其接口不符合需求
- 想创建可复用的类，与不相关或不可预见的类协同工作
- 需要整合遗留系统 (legacy API) 到新系统中

**代码示例参考**: `#[[file:codeExample/adapterExampleCode.cs]]`

---

## 2. Facade Pattern (外观模式)

**目的**: 为子系统中的一组接口提供一个统一的高层接口，使子系统更容易使用。

**实现要点**:
- **Facade (外观类)**: 提供简化的接口，内部协调多个子系统 (如 `Mortgage` 类)
- **Subsystems (子系统)**: 实现具体功能的类 (如 `Bank`, `Credit`, `Loan` 类)
- Facade 知道哪些子系统负责处理请求，将客户请求委派给适当的子系统

**使用场景**:
- 需要为复杂子系统提供简单接口
- 客户程序与抽象类的实现部分之间存在很多依赖关系
- 需要构建分层系统，使用 Facade 定义每层的入口点

**代码示例参考**: `#[[file:codeExample/facadeExampleCode.cs]]`

---

## 3. Singleton Pattern (单例模式)

**目的**: 确保一个类只有一个实例，并提供全局访问点。

**实现要点**:
- **私有构造函数**: 防止外部实例化
- **静态实例**: 使用 `static readonly` 字段存储唯一实例
- **公共访问方法**: 提供 `GetInstance()` 方法返回唯一实例
- **线程安全**: .NET 保证静态初始化的线程安全性

**使用场景**:
- 类只能有一个实例，且必须从一个众所周知的访问点访问
- 唯一实例应该可以通过子类化扩展
- 需要严格控制全局变量 (如配置管理器、连接池、负载均衡器)

**代码示例参考**: `#[[file:codeExample/singletonExampleCode.cs]]`

---

## 4. Strategy Pattern (策略模式)

**目的**: 定义一系列算法，把它们封装起来，并使它们可以互相替换。策略模式让算法独立于使用它的客户而变化。

**实现要点**:
- **Strategy Interface (策略接口)**: 定义算法的公共接口 (如 `ISortStrategy`)
- **Concrete Strategies (具体策略)**: 实现不同的算法 (如 `QuickSort`, `ShellSort`, `MergeSort`)
- **Context (上下文)**: 维护对策略对象的引用，可以动态切换策略 (如 `SortedList`)

**使用场景**:
- 需要在不同情况下使用不同的算法变体
- 算法使用客户不应该知道的数据
- 一个类定义了多种行为，这些行为在类的操作中以多个条件语句的形式出现
- 需要避免使用多重条件语句

**代码示例参考**: `#[[file:codeExample/strategyExampleCode.cs]]`

---

## 在本项目中应用设计模式的指导原则

1. **识别变化点**: 找出代码中可能变化的部分，使用适当的设计模式封装变化
2. **优先组合而非继承**: 使用 Strategy 和 Adapter 等模式实现灵活的对象组合
3. **面向接口编程**: 定义清晰的接口，降低耦合度
4. **保持简单**: 不要过度设计，只在真正需要时引入设计模式
5. **代码复用**: 使用 Facade 简化复杂子系统的使用
6. **单一职责**: 每个类应该只有一个改变的理由

## C# 实现注意事项

- 使用 `sealed` 关键字防止 Singleton 被继承
- 利用 .NET 的静态初始化保证线程安全
- 使用泛型 (`List<T>`) 提供类型安全
- 考虑使用属性 (Properties) 而非公共字段
- 遵循 C# 命名约定 (PascalCase for public members)
