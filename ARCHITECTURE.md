# AI Clinic 后端架构文档

## 正确的架构层次

```
Controller → Service → State → DAO → Database
```

### 各层职责

#### 1. Controller 层
- **职责**: 处理 HTTP 请求和响应
- **依赖**: Service
- **文件位置**: `Controller/`

#### 2. Service 层
- **职责**: 业务逻辑层
- **功能**:
  - 实现业务规则和验证
  - 协调多个 State 完成复杂业务逻辑
  - 提供给 Controller 的统一接口
- **依赖**: State
- **文件位置**: `Services/`

#### 3. State 层
- **职责**: 状态管理层
- **功能**:
  - 管理应用状态（当前用户、缓存等）
  - 调用 DAO 进行数据库操作
  - 提供状态变更通知（OnChange 事件）
  - 管理加载状态和错误信息
- **依赖**: DAO (Repository Interface)
- **文件位置**: `UI/State/`

#### 4. DAO 层 (Data Access Object)
- **职责**: 数据访问对象
- **功能**:
  - 实现 Repository 接口
  - 执行 CRUD 数据库操作
  - 将数据库记录转换为业务对象
- **依赖**: Database Client (SupabaseHttpClient)
- **文件位置**: `DAOs/`

#### 5. Interface 层
- **职责**: 定义接口和实体
- **功能**:
  - 定义实体接口（IAdminProfile, IUser 等）
  - 定义 Repository 接口（IAdminProfileRepository 等）
  - 定义 CRUD 操作的方法签名
- **文件位置**: `Interface/`

## 数据流向

```
UI → Controller → Service → State → DAO → Database
                              ↓
                         State Management
                         Cache
                         Loading/Error
```

## 示例代码结构

### Controller 示例
```csharp
public class AdminProfileController
{
    private readonly AdminProfileService _service;
    
    public AdminProfileController(AdminProfileService service)
    {
        _service = service;
    }
    
    public async Task<object?> GetAdminProfileByIdAsync(string adminId)
    {
        return await _service.GetAdminProfileByIdAsync(adminId);
    }
}
```

### Service 示例
```csharp
public class AdminProfileService
{
    private readonly AdminProfileState _state;
    
    public AdminProfileService(AdminProfileState state)
    {
        _state = state;
    }
    
    public async Task<AdminProfile?> GetProfileByIdAsync(Guid id)
    {
        // 业务逻辑可以在这里添加
        return await _state.GetByIdAsync(id);
    }
    
    public async Task<AdminProfile?> CreateProfileAsync(AdminProfile profile)
    {
        // 业务逻辑验证
        if (string.IsNullOrWhiteSpace(profile.FullName))
        {
            throw new ArgumentException("Full name is required");
        }
        
        // 设置时间戳
        profile.CreatedAt = DateTime.UtcNow;
        profile.UpdatedAt = DateTime.UtcNow;
        
        // 调用 State
        return await _state.CreateAsync(profile);
    }
}
```

### State 示例
```csharp
public class AdminProfileState
{
    private readonly IAdminProfileRepository _repository;
    private AdminProfile? _currentProfile;
    private bool _isLoading;
    private string? _errorMessage;
    
    public AdminProfileState(IAdminProfileRepository repository)
    {
        _repository = repository;
    }
    
    public event Action? OnChange;
    
    public AdminProfile? CurrentProfile => _currentProfile;
    public bool IsLoading => _isLoading;
    public string? ErrorMessage => _errorMessage;
    
    public async Task<AdminProfile?> GetByIdAsync(Guid id)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();
            
            // 调用 DAO
            var profile = await _repository.GetByIdAsync(id);
            _currentProfile = profile;
            return profile;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }
    
    private void NotifyStateChanged() => OnChange?.Invoke();
}
```

### DAO 示例
```csharp
public class AdminProfileDAO : IAdminProfileRepository
{
    private readonly SupabaseHttpClient _supabase;
    
    public AdminProfileDAO(SupabaseHttpClient supabase)
    {
        _supabase = supabase;
    }
    
    public async Task<AdminProfile?> GetByIdAsync(Guid id)
    {
        // 纯粹的数据库操作
        return await _supabase.GetSingleAsync<AdminProfile>(
            "admin_profiles", 
            $"id=eq.{id}"
        );
    }
    
    public async Task<AdminProfile> AddAsync(AdminProfile entity)
    {
        var result = await _supabase.PostAsync<AdminProfile>(
            "admin_profiles", 
            entity
        );
        return result ?? entity;
    }
}
```

## 依赖注入配置

```csharp
// DependencyInjection.cs
public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 注册 Database Client
        services.AddScoped<SupabaseHttpClient>(...);
        
        // 注册 DAO (Repository 实现)
        services.AddScoped<IAdminProfileRepository, AdminProfileDAO>();
        services.AddScoped<IUserRepository, UserDAO>();
        services.AddScoped<ISupabaseAuthClient, SupabaseAuthClient>();
        
        // 注册 State (依赖 DAO)
        services.AddScoped<AdminProfileState>();
        services.AddScoped<AuthState>();
        
        // 注册 Service (依赖 State)
        services.AddScoped<AdminProfileService>();
        services.AddScoped<AuthService>();
        
        // 注册 Controller (依赖 Service)
        services.AddScoped<AdminProfileController>();
        services.AddScoped<AuthController>();
        
        return services;
    }
}
```

## 已完成的模块

### ✅ AdminProfile 模块
- `Controller/AdminProfileController.cs` → 依赖 Service
- `Services/AdminProfileService.cs` → 依赖 State
- `UI/State/AdminProfileState.cs` → 依赖 DAO
- `DAOs/AdminProfileDAO.cs` → 实现 Repository 接口
- `Interface/AdminProfileInterface.cs` → 定义接口

### ✅ Auth 模块
- `Controller/AuthController.cs` → 依赖 Service
- `Services/AuthService.cs` → 依赖 State
- `UI/State/AuthState.cs` → 依赖 DAO 和 AuthClient
- `DAOs/UserDAO.cs` → 实现 Repository 接口
- `DAOs/SupabaseAuthClient.cs` → 实现 Auth 接口
- `Interface/UserInterface.cs` → 定义接口
- `Interface/ISupabaseAuthClient.cs` → 定义接口

## 待完成的模块

- DoctorProfile
- PatientProfile
- Conversation
- Message
- Document
- SupportTicket
- DoctorRating

## 架构优势

1. **清晰的职责分离**
   - Controller: HTTP 处理
   - Service: 业务逻辑
   - State: 状态管理
   - DAO: 数据访问

2. **状态管理集中化**
   - State 层统一管理应用状态
   - 提供状态变更通知
   - 便于 UI 组件订阅状态变化

3. **易于测试**
   - 每层可以独立测试
   - 可以 mock 下层依赖

4. **代码复用**
   - Service 可以被多个 Controller 使用
   - State 可以被 Service 和 UI 组件共用

## 注意事项

1. **依赖方向**: Controller → Service → State → DAO
2. **State 管理状态**: 包括当前数据、缓存、加载状态、错误信息
3. **Service 处理业务逻辑**: 验证、数据转换、协调多个 State
4. **DAO 只做数据访问**: 不包含业务逻辑
