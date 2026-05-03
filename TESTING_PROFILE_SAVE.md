# 测试个人资料保存功能

## 问题描述
更换照片后点击 "Save Changes" 没有反应。

## 已修复的问题

### 1. 缺少服务注入
**问题**：Profile.razor.cs 中没有正确访问注入的服务
**解决**：Profile.razor 中已经有 `@inject` 声明，.cs 文件会自动识别

### 2. 添加调试日志
在以下方法中添加了 Console.WriteLine 日志：
- `SaveProfileAsync()` - 保存资料时的详细日志
- `HandlePhotoUpload()` - 上传照片时的详细日志

### 3. 照片上传后重新加载
**改进**：照片上传成功后会自动调用 `LoadProfileAsync()` 重新加载完整的资料数据

### 4. 添加 StateHasChanged()
在保存完成后调用 `StateHasChanged()` 确保 UI 更新

## 测试步骤

### 1. 启动应用
```bash
dotnet run
```

### 2. 登录并访问个人资料页面
```
https://localhost:5001/patient/profile
```

### 3. 测试照片上传
1. 点击 "Edit Profile" 按钮
2. 点击相机图标上传照片
3. 选择一张图片（JPEG/PNG/GIF，小于 5MB）
4. 查看控制台日志：
   ```
   [Profile] Uploading photo: photo.jpg, Size: 123456
   [Profile] Photo data size: 123456 bytes
   [Profile] Photo uploaded and profile reloaded
   ```
5. 应该看到 "Photo uploaded successfully!" 消息
6. 照片应该立即显示

### 4. 测试资料保存
1. 在编辑模式下修改个人信息（如姓名、地址等）
2. 点击 "Save Changes" 按钮
3. 查看控制台日志：
   ```
   [Profile] Saving profile for user {userId}
   [Profile] Profile ID: {profileId}
   [Profile] Full Name: {name}
   [Profile] Updating existing profile
   [Profile] Profile saved successfully
   ```
4. 应该看到 "Profile saved successfully!" 消息
5. 编辑模式应该自动关闭
6. 修改的信息应该保存并显示

### 5. 测试照片删除
1. 在编辑模式下点击 "Remove Photo" 按钮
2. 应该看到 "Photo deleted successfully!" 消息
3. 照片应该消失，显示默认头像

## 查看日志

### 浏览器控制台
打开浏览器开发者工具（F12），查看 Console 标签页

### 应用程序输出
在运行 `dotnet run` 的终端窗口中查看输出

## 可能的问题和解决方案

### 问题 1：点击按钮没有反应
**检查**：
1. 浏览器控制台是否有 JavaScript 错误
2. 应用程序输出是否有异常
3. 按钮是否被禁用（`disabled` 属性）

**解决**：
- 刷新页面重试
- 检查是否在编辑模式
- 查看日志确认方法是否被调用

### 问题 2：保存后数据没有更新
**检查**：
1. 控制台日志是否显示 "Profile saved successfully"
2. 数据库中的数据是否已更新
3. `LoadProfileAsync()` 是否被调用

**解决**：
- 检查数据库连接
- 查看是否有异常日志
- 确认 `StateHasChanged()` 被调用

### 问题 3：照片上传后不显示
**检查**：
1. 照片是否成功保存到数据库
2. `photoDataUrl` 是否正确生成
3. Base64 字符串是否有效

**解决**：
- 检查文件大小和类型
- 查看上传日志
- 确认 `LoadProfileAsync()` 重新加载了照片

## 调试技巧

### 1. 使用浏览器开发者工具
- Network 标签：查看 SignalR 连接
- Console 标签：查看日志输出
- Elements 标签：检查 DOM 元素状态

### 2. 检查数据库
```bash
# 使用 SQLite 浏览器工具查看数据
# 或使用命令行（如果安装了 sqlite3）
sqlite3 ai-clinic.db "SELECT id, full_name, LENGTH(profile_photo) as photo_size FROM patient_profiles;"
```

### 3. 添加更多日志
在需要的地方添加 `Console.WriteLine()` 来追踪执行流程

## 预期行为

### 正常流程
1. 用户上传照片 → 立即保存到数据库 → 显示成功消息 → 照片显示
2. 用户修改资料 → 点击保存 → 保存到数据库 → 显示成功消息 → 退出编辑模式
3. 用户删除照片 → 立即从数据库删除 → 显示成功消息 → 显示默认头像

### 关键点
- 照片上传是**立即保存**的，不需要点击 "Save Changes"
- "Save Changes" 只保存**表单数据**（姓名、地址等）
- 每次操作后都会重新加载数据确保显示最新状态

## 代码改进说明

### 之前的问题
```csharp
// 照片上传后没有重新加载
photoDataUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(photoData)}";
StateHasChanged();
```

### 现在的实现
```csharp
// 照片上传后重新加载完整数据
photoDataUrl = $"data:image/jpeg;base64,{Convert.ToBase64String(photoData)}";
await LoadProfileAsync(); // 重新加载
StateHasChanged();
```

### 保存方法改进
```csharp
// 添加了详细日志和 StateHasChanged
Console.WriteLine($"[Profile] Saving profile for user {currentUserId}");
// ... 保存逻辑 ...
Console.WriteLine("[Profile] Profile saved successfully");
StateHasChanged(); // 确保 UI 更新
```

## 总结

修复包括：
- ✅ 添加详细的调试日志
- ✅ 照片上传后重新加载数据
- ✅ 保存后调用 StateHasChanged()
- ✅ 改进错误处理和日志输出

现在应该可以正常工作了。如果还有问题，请查看控制台日志找出具体原因。
