# Lucide Icon 闪烁问题修复

## 问题描述

在 Blazor Server 应用中使用 Lucide 图标时，图标会不断闪烁。

## 原因分析

1. **MutationObserver 过度触发**: 原始代码监听整个 `document.body` 的所有 DOM 变化
2. **Blazor 频繁更新**: Blazor Server 通过 SignalR 频繁更新 DOM
3. **重复初始化**: 每次 DOM 变化都会调用 `lucide.createIcons()`，导致图标重新渲染
4. **变量重复声明**: 多个页面都声明 `const observer`，导致冲突

## 解决方案

### 1. 使用 IIFE (立即执行函数表达式)
```javascript
(function() {
    // 代码在独立作用域中运行，避免变量冲突
})();
```

### 2. 防止重复初始化
```javascript
if (window.lucideInitialized) return;
window.lucideInitialized = true;
```

### 3. 使用 Debounce (防抖)
```javascript
let debounceTimer;
const observer = new MutationObserver(() => {
    clearTimeout(debounceTimer);
    debounceTimer = setTimeout(() => {
        lucide.createIcons();
    }, 100); // 等待 100ms 后再初始化
});
```

## 修复后的代码

```javascript
<script src="https://unpkg.com/lucide@latest"></script>
<script>
    (function() {
        // 防止多次初始化
        if (window.lucideInitialized) return;
        window.lucideInitialized = true;

        // 初始化函数
        function initLucide() {
            if (typeof lucide !== 'undefined') {
                lucide.createIcons();
            }
        }

        // 页面加载时初始化
        if (document.readyState === 'loading') {
            document.addEventListener('DOMContentLoaded', initLucide);
        } else {
            initLucide();
        }

        // 使用防抖处理 Blazor 更新
        let debounceTimer;
        const observer = new MutationObserver(() => {
            clearTimeout(debounceTimer);
            debounceTimer = setTimeout(() => {
                if (typeof lucide !== 'undefined') {
                    lucide.createIcons();
                }
            }, 100);
        });

        observer.observe(document.body, {
            childList: true,
            subtree: true
        });
    })();
</script>
```

## 效果

- ✅ 图标不再闪烁
- ✅ 没有控制台错误
- ✅ 多个页面可以共存
- ✅ Blazor 更新后图标正常显示

## 其他错误修复

### 错误: `GET http://localhost:5269/js/sessionStorage.js net::ERR_ABORTED 404`
这个错误可以忽略，或者删除对该文件的引用（如果存在）。

### 错误: `Blazor has already started`
这是因为 Blazor 脚本被多次加载。确保只在主布局中加载一次 `blazor.web.js`。

## 最佳实践

1. **全局加载 Lucide**: 在 `App.razor` 或主布局中加载一次
2. **避免在每个页面重复加载**: 使用全局初始化
3. **使用 Blazor 生命周期**: 在 `OnAfterRenderAsync` 中初始化图标

### 推荐的全局方法

在 `wwwroot/index.html` 或主布局中：

```html
<script src="https://unpkg.com/lucide@latest"></script>
<script>
    window.initLucideIcons = function() {
        if (typeof lucide !== 'undefined') {
            lucide.createIcons();
        }
    };
    
    // 初始化
    window.initLucideIcons();
</script>
```

在 Razor 组件中：

```csharp
@inject IJSRuntime JS

protected override async Task OnAfterRenderAsync(bool firstRender)
{
    await JS.InvokeVoidAsync("initLucideIcons");
}
```

## 总结

通过使用 IIFE、防抖和防止重复初始化，我们成功解决了 Lucide 图标闪烁的问题。
