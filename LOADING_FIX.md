# Patient Dashboard Loading 问题修复

## 问题描述
Patient Dashboard 页面一直显示 loading 状态，无法正常加载内容。

## 根本原因
问题出在 `AuthGuard` 组件的认证检查逻辑：

1. **AuthGuard** 在每次检查时都调用 `RevalidateSessionAsync()`
2. `RevalidateSessionAsync()` 会强制重新初始化认证状态
3. 在服务器端渲染时，JavaScript interop 可能还未准备好
4. Cookie 读取失败或超时，导致认证检查一直失败
5. 页面陷入无限 loading 状态

## 修复方案

### 1. 修改 AuthGuard.razor 的认证检查逻辑

**修改前：**
```csharp
private async Task CheckAuthenticationAsync()
{
    // 每次都强制重新验证 session
    await AuthState.RevalidateSessionAsync(...);
    // ...
}
```

**修改后：**
```csharp
private async Task CheckAuthenticationAsync()
{
    // 只在未初始化时才初始化
    if (!AuthState.IsInitialized)
    {
        await AuthState.InitializeAsync(...);
    }
    // ...
}
```

### 2. 优化 OnAfterRenderAsync 逻辑

**修改前：**
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        // 总是重新检查
        await CheckAuthenticationAsync();
    }
}
```

**修改后：**
```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender && !AuthState.IsInitialized)
    {
        // 只在未初始化时才检查
        await CheckAuthenticationAsync();
    }
}
```

### 3. 改进 AuthStateService 的 Cookie 读取

添加了超时机制，防止 JavaScript interop 调用挂起：

```csharp
private async Task<string?> GetCookieAsync(string name)
{
    try
    {
        // 添加 2 秒超时
        var timeoutTask = Task.Delay(2000);
        var cookieTask = _jsRuntime.InvokeAsync<string?>("eval", ...).AsTask();
        
        var completedTask = await Task.WhenAny(cookieTask, timeoutTask);
        
        if (completedTask == cookieTask)
        {
            return await cookieTask;
        }
        
        return null; // 超时
    }
    catch
    {
        return null;
    }
}
```

### 4. 防止重复初始化

在 `InitializeAsync` 方法开头添加检查：

```csharp
public async Task InitializeAsync(...)
{
    // 防止多次同时初始化
    if (_isInitialized)
    {
        return;
    }
    // ...
}
```

## 测试步骤

1. 停止当前运行的应用程序
2. 重新编译项目：`dotnet build`
3. 运行应用程序：`dotnet run`
4. 登录后访问 `/patient/dashboard`
5. 页面应该能正常加载，不再一直显示 loading

## 预期结果

- ✅ 登录后能正确跳转到 Patient Dashboard
- ✅ Dashboard 页面能正常显示内容
- ✅ 不再出现无限 loading 状态
- ✅ 认证状态正确保持

## 相关文件

- `UI/Components/AuthGuard.razor` - 认证守卫组件
- `Services/AuthStateService.cs` - 认证状态服务
- `UI/Pages/Patient/Dashboard.razor` - Patient Dashboard 页面

## 注意事项

如果问题仍然存在，请检查：
1. 浏览器控制台是否有 JavaScript 错误
2. Cookie 是否正确设置（检查浏览器开发者工具 > Application > Cookies）
3. 数据库中是否有对应的用户和 profile 数据
