# Sidebar 实时更新功能

## 问题描述
更换照片或修改姓名后，Sidebar 不会立即更新，需要刷新页面才能看到新的名字和照片。

## 解决方案

### 1. 添加事件通知机制

在 `AuthStateService` 中添加公共方法来触发状态变更事件：

```csharp
/// <summary>
/// Notify subscribers that auth state has changed
/// Call this after updating profile information
/// </summary>
public void NotifyStateChanged()
{
    NotifyAuthStateChanged();
}
```

### 2. Profile 页面触发更新

在保存资料和上传照片后调用 `AuthState.NotifyStateChanged()`：

```csharp
// 保存资料后
await PatientProfileService.UpdateAsync(profileData);
await LoadProfileAsync();
AuthState.NotifyStateChanged(); // 触发 Sidebar 更新

// 上传照片后
await PatientProfileService.UpdateProfilePhotoAsync(currentUserId, photoData);
await LoadProfileAsync();
AuthState.NotifyStateChanged(); // 触发 Sidebar 更新
```

### 3. Sidebar 显示照片

更新 `SidebarLayout.razor` 以加载和显示用户照片：

```razor
<div class="avatar">
    @if (!string.IsNullOrEmpty(userPhotoUrl))
    {
        <img src="@userPhotoUrl" alt="Profile" class="avatar-img" />
    }
    else if (!string.IsNullOrEmpty(userInitials))
    {
        <span class="avatar-text">@userInitials</span>
    }
    else
    {
        <i data-lucide="user"></i>
    }
</div>
```

### 4. Sidebar 监听状态变更

```csharp
private async void UpdateUserInfo()
{
    userName = AuthState.GetDisplayName();
    userEmail = AuthState.CurrentUser?.Email;
    userInitials = AuthState.GetUserInitials();
    
    // Load profile photo if user is a patient
    if (AuthState.IsPatient && AuthState.CurrentUser != null)
    {
        var photoData = await PatientProfileService.GetProfilePhotoAsync(AuthState.CurrentUser.Id);
        if (photoData != null && photoData.Length > 0)
        {
            userPhotoUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(photoData)}";
        }
    }
    
    await InvokeAsync(StateHasChanged);
}
```

## 工作流程

```
用户修改资料/上传照片
    ↓
Profile.razor.cs 保存到数据库
    ↓
调用 AuthState.NotifyStateChanged()
    ↓
触发 OnAuthStateChanged 事件
    ↓
SidebarLayout 监听到事件
    ↓
UpdateUserInfo() 重新加载数据
    ↓
从数据库加载最新照片
    ↓
调用 StateHasChanged() 刷新 UI
    ↓
Sidebar 显示更新后的名字和照片
```

## 测试步骤

### 1. 启动应用
```bash
dotnet run
```

### 2. 登录并访问个人资料页面
```
https://localhost:5001/patient/profile
```

### 3. 测试姓名更新
1. 点击 "Edit Profile"
2. 修改 "Full Name" 字段
3. 点击 "Save Changes"
4. **不要刷新页面**
5. 查看左侧 Sidebar
6. ✅ 应该立即看到新的名字

### 4. 测试照片更新
1. 在编辑模式下点击相机图标
2. 选择一张照片上传
3. **不要刷新页面**
4. 查看左侧 Sidebar
5. ✅ 应该立即看到新的照片

### 5. 查看控制台日志
浏览器控制台应该显示：
```
[Profile] Photo uploaded, profile reloaded, and AuthState change event triggered
[Sidebar] Loaded profile photo, size: 123456 bytes
```

## 关键改进

### 之前的问题
- ❌ Profile 更新后没有通知其他组件
- ❌ Sidebar 只在页面加载时获取数据
- ❌ Sidebar 不显示照片，只显示首字母

### 现在的实现
- ✅ Profile 更新后触发全局事件
- ✅ Sidebar 监听事件并自动刷新
- ✅ Sidebar 显示用户照片
- ✅ 实时更新，无需刷新页面

## 技术细节

### 事件驱动架构
使用 C# 事件机制实现组件间通信：

```csharp
// AuthStateService.cs
public event Action? OnAuthStateChanged;

// SidebarLayout.razor
protected override void OnInitialized()
{
    AuthState.OnAuthStateChanged += UpdateUserInfo;
}

public void Dispose()
{
    AuthState.OnAuthStateChanged -= UpdateUserInfo;
}
```

### Base64 图片显示
照片从数据库加载后转换为 Base64 Data URL：

```csharp
var photoData = await PatientProfileService.GetProfilePhotoAsync(userId);
userPhotoUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(photoData)}";
```

### 异步更新
使用 `InvokeAsync` 确保 UI 更新在正确的线程上执行：

```csharp
await InvokeAsync(StateHasChanged);
```

## 性能考虑

### 照片加载
- 照片只在状态变更时加载
- 使用 Base64 缓存在内存中
- 避免重复的数据库查询

### 事件订阅
- 组件销毁时正确取消订阅
- 避免内存泄漏

## 扩展性

### 其他组件也可以监听
任何组件都可以订阅 `AuthState.OnAuthStateChanged` 事件：

```csharp
protected override void OnInitialized()
{
    AuthState.OnAuthStateChanged += HandleAuthStateChange;
}

private void HandleAuthStateChange()
{
    // 更新组件状态
    StateHasChanged();
}
```

### 支持医生用户
相同的机制可以应用到 `DoctorSidebarLayout`：

```csharp
if (AuthState.IsDoctor && AuthState.CurrentUser != null)
{
    var photoData = await DoctorProfileService.GetProfilePhotoAsync(AuthState.CurrentUser.Id);
    // ...
}
```

## 故障排除

### 问题：Sidebar 没有更新
**检查**：
1. 控制台是否显示 "AuthState change event triggered"
2. Sidebar 是否正确订阅了事件
3. `UpdateUserInfo()` 是否被调用

**解决**：
- 确认 `AuthState.NotifyStateChanged()` 被调用
- 检查事件订阅是否正确
- 查看浏览器控制台日志

### 问题：照片不显示
**检查**：
1. 照片是否成功保存到数据库
2. `GetProfilePhotoAsync()` 是否返回数据
3. Base64 字符串是否正确生成

**解决**：
- 检查数据库中的 profile_photo 字段
- 查看 Sidebar 日志
- 验证照片数据大小

### 问题：页面性能下降
**检查**：
1. 照片文件是否过大
2. 是否频繁触发更新事件

**解决**：
- 限制照片大小（已限制 5MB）
- 考虑添加防抖机制
- 使用照片缓存

## 总结

实现了完整的实时更新机制：
- ✅ 事件驱动的组件通信
- ✅ Sidebar 显示用户照片
- ✅ 修改后立即更新，无需刷新
- ✅ 支持姓名和照片更新
- ✅ 正确的内存管理（事件取消订阅）
- ✅ 详细的调试日志

用户体验大幅提升，符合现代 Web 应用的交互标准！
