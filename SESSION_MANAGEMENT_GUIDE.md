# Session Management Guide

## 功能说明

系统使用 Cookie 来持久化用户会话，实现以下功能：

1. **自动登录** - 刷新页面后自动恢复登录状态
2. **会话保护** - 删除 Cookie 后自动重定向到登录页
3. **角色验证** - 确保用户只能访问其角色允许的页面

## Cookie 详情

- **名称**: `userId`
- **内容**: 用户的 GUID
- **过期时间**: 30 天
- **路径**: `/`
- **SameSite**: `Strict`

## 测试步骤

### 测试 1: 正常登录和会话持久化

1. 访问 `/auth/signin`
2. 输入邮箱和密码登录
3. 成功后会跳转到 Dashboard
4. **刷新页面** - 应该保持登录状态
5. 打开浏览器开发者工具 → Application → Cookies
6. 应该看到 `userId` Cookie

### 测试 2: 删除 Cookie 后自动登出

1. 登录后在 Dashboard 页面
2. 打开浏览器开发者工具 (F12)
3. 进入 Application → Cookies → `http://localhost:5269`
4. 找到 `userId` Cookie 并删除
5. **刷新页面** - 应该自动跳转到 `/auth/signin`

### 测试 3: 角色验证

1. 以 Patient 身份登录
2. 尝试访问 `/doctor/dashboard`
3. 应该被重定向回 `/auth/signin` 或显示无权限

### 测试 4: 已登录状态访问登录页

1. 已登录的情况下访问 `/auth/signin`
2. 应该自动跳转到对应的 Dashboard

## 如何手动删除 Cookie (测试用)

### 方法 1: 浏览器开发者工具
```
1. 按 F12 打开开发者工具
2. 点击 "Application" 标签
3. 左侧展开 "Cookies"
4. 选择你的网站 (http://localhost:5269)
5. 右键点击 "userId" → Delete
6. 刷新页面
```

### 方法 2: 浏览器控制台
```javascript
// 在浏览器控制台 (F12 → Console) 运行:
document.cookie = "userId=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;";
location.reload();
```

### 方法 3: 添加登出按钮
在 Dashboard 添加登出功能（推荐）

## 常见问题

### Q: 删除 Cookie 后没有跳转？
**A**: 确保你刷新了页面。AuthGuard 会在页面加载时检查 Cookie。

### Q: Cookie 一直存在无法删除？
**A**: 
1. 确保删除的是正确域名下的 Cookie
2. 检查是否有多个标签页打开，关闭所有标签页
3. 清除浏览器缓存

### Q: 登录后立即被登出？
**A**: 
1. 检查数据库中用户的 `is_active` 和 `is_deactivated` 字段
2. 查看浏览器控制台是否有 JavaScript 错误
3. 检查 Cookie 是否被正确设置

## 安全注意事项

1. **生产环境**: 应该使用 HTTPS 并添加 `Secure` 标志
2. **敏感操作**: 重要操作应该要求重新验证密码
3. **会话超时**: 考虑添加活动超时机制
4. **多设备登录**: 当前实现允许多设备同时登录

## 下一步改进

- [ ] 添加 "Remember Me" 选项
- [ ] 实现会话超时 (idle timeout)
- [ ] 添加登出功能按钮
- [ ] 实现 "在其他设备登出" 功能
- [ ] 添加会话活动日志
- [ ] 使用 JWT 替代简单的 userId Cookie
