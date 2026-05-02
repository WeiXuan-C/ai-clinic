# 设计模式应用技能

这个技能帮助你在代码中识别和应用四种核心设计模式。

## 技能概述

当需要重构代码或设计新功能时，使用此技能来：
1. 识别适合使用设计模式的场景
2. 选择最合适的设计模式
3. 正确实现设计模式

## 模式选择决策树

### 何时使用 Adapter Pattern?
**问题信号**:
- "我需要使用这个旧的/第三方的类，但它的接口不匹配"
- "我想让不兼容的接口一起工作"
- "我需要包装遗留代码"

**解决方案**:
```csharp
// 创建适配器类继承目标接口，内部使用被适配者
class MyAdapter : ITargetInterface
{
    private LegacyClass adaptee = new LegacyClass();
    
    public void TargetMethod()
    {
        // 转换调用到 adaptee 的方法
        adaptee.LegacyMethod();
    }
}
```

### 何时使用 Facade Pattern?
**问题信号**:
- "这个功能需要调用太多不同的类"
- "客户端代码太复杂，需要了解太多子系统细节"
- "我想简化复杂系统的使用"

**解决方案**:
```csharp
// 创建外观类，提供简化的高层接口
class SimpleFacade
{
    private SubsystemA a = new SubsystemA();
    private SubsystemB b = new SubsystemB();
    private SubsystemC c = new SubsystemC();
    
    public void SimpleOperation()
    {
        // 协调多个子系统完成复杂操作
        a.OperationA();
        b.OperationB();
        c.OperationC();
    }
}
```

### 何时使用 Singleton Pattern?
**问题信号**:
- "这个类应该只有一个实例"
- "我需要全局访问点"
- "多个实例会导致问题（如配置冲突、资源浪费）"

**解决方案**:
```csharp
// 创建线程安全的单例
sealed class MySingleton
{
    private static readonly MySingleton instance = new MySingleton();
    
    private MySingleton() { }
    
    public static MySingleton GetInstance()
    {
        return instance;
    }
}
```

**警告**: 不要过度使用 Singleton，它可能导致：
- 测试困难
- 隐藏依赖关系
- 违反单一职责原则

### 何时使用 Strategy Pattern?
**问题信号**:
- "我有多个算法可以完成同一任务"
- "我需要在运行时切换算法"
- "我的代码中有大量的 if-else 或 switch 语句来选择行为"

**解决方案**:
```csharp
// 1. 定义策略接口
interface IStrategy
{
    void Execute();
}

// 2. 实现具体策略
class ConcreteStrategyA : IStrategy
{
    public void Execute() { /* 算法 A */ }
}

class ConcreteStrategyB : IStrategy
{
    public void Execute() { /* 算法 B */ }
}

// 3. 上下文类使用策略
class Context
{
    private IStrategy strategy;
    
    public IStrategy Strategy
    {
        set { strategy = value; }
    }
    
    public void DoWork()
    {
        strategy.Execute();
    }
}
```

## 实现检查清单

### Adapter Pattern 检查清单
- [ ] 确定目标接口（客户端期望的接口）
- [ ] 确定被适配者（需要适配的现有类）
- [ ] 创建适配器类继承/实现目标接口
- [ ] 在适配器中持有被适配者的实例
- [ ] 实现接口方法，内部调用被适配者的方法
- [ ] 测试适配器是否正确转换了接口

### Facade Pattern 检查清单
- [ ] 识别需要简化的复杂子系统
- [ ] 设计简洁的高层接口
- [ ] 创建 Facade 类
- [ ] 在 Facade 中初始化所需的子系统对象
- [ ] 实现高层方法，协调子系统完成任务
- [ ] 确保客户端只需要与 Facade 交互

### Singleton Pattern 检查清单
- [ ] 将构造函数设为 private
- [ ] 创建 static readonly 实例字段
- [ ] 提供 public static 访问方法
- [ ] 考虑使用 sealed 防止继承
- [ ] 确认线程安全性（.NET 静态初始化是线程安全的）
- [ ] 考虑是否真的需要 Singleton（依赖注入可能是更好的选择）

### Strategy Pattern 检查清单
- [ ] 定义策略接口，声明算法方法
- [ ] 为每个算法创建具体策略类
- [ ] 创建上下文类，持有策略引用
- [ ] 提供设置策略的方法（属性或构造函数）
- [ ] 在上下文中调用策略方法
- [ ] 测试策略切换是否正常工作

## 常见错误和陷阱

### Adapter Pattern
❌ **错误**: 适配器做了太多事情，不仅转换接口还添加新功能
✅ **正确**: 适配器只负责接口转换，保持简单

### Facade Pattern
❌ **错误**: Facade 变成了"上帝对象"，包含业务逻辑
✅ **正确**: Facade 只是协调者，业务逻辑在子系统中

### Singleton Pattern
❌ **错误**: 在单例中存储可变的全局状态
✅ **正确**: 考虑使用依赖注入容器管理生命周期

### Strategy Pattern
❌ **错误**: 策略类之间有依赖关系
✅ **正确**: 每个策略应该是独立的，可以单独使用

## 重构建议

### 从条件语句重构到 Strategy
**重构前**:
```csharp
public void ProcessPayment(string method)
{
    if (method == "CreditCard")
    {
        // 信用卡处理逻辑
    }
    else if (method == "PayPal")
    {
        // PayPal 处理逻辑
    }
    else if (method == "Bitcoin")
    {
        // Bitcoin 处理逻辑
    }
}
```

**重构后**:
```csharp
interface IPaymentStrategy
{
    void ProcessPayment(decimal amount);
}

class CreditCardPayment : IPaymentStrategy { }
class PayPalPayment : IPaymentStrategy { }
class BitcoinPayment : IPaymentStrategy { }

class PaymentProcessor
{
    public IPaymentStrategy Strategy { get; set; }
    
    public void Process(decimal amount)
    {
        Strategy.ProcessPayment(amount);
    }
}
```

### 从复杂调用重构到 Facade
**重构前**:
```csharp
// 客户端需要了解太多细节
var validator = new Validator();
var processor = new Processor();
var logger = new Logger();
var notifier = new Notifier();

if (validator.Validate(data))
{
    var result = processor.Process(data);
    logger.Log(result);
    notifier.Notify(result);
}
```

**重构后**:
```csharp
// 使用 Facade 简化
class OrderFacade
{
    public void ProcessOrder(Order order)
    {
        // 内部协调所有子系统
    }
}

// 客户端代码变得简单
var facade = new OrderFacade();
facade.ProcessOrder(order);
```

## 与依赖注入的结合

在现代 C# 应用中，这些模式通常与依赖注入一起使用：

```csharp
// 在 DependencyInjection.cs 中注册
services.AddSingleton<IMyService, MyService>();  // Singleton 生命周期
services.AddScoped<IStrategy, DefaultStrategy>(); // Strategy 注入
services.AddTransient<IFacade, MyFacade>();      // Facade 注入
```

## 参考代码示例

- Adapter: `codeExample/adapterExampleCode.cs`
- Facade: `codeExample/facadeExampleCode.cs`
- Singleton: `codeExample/singletonExampleCode.cs`
- Strategy: `codeExample/strategyExampleCode.cs`
