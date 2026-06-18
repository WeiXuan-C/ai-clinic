using MudBlazor.Services;
using ai_clinic.Data;
using ai_clinic;
using ai_clinic.Services.Hubs;
using QuestPDF.Infrastructure;

// Configure QuestPDF license (Community license for non-commercial use)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// 🔒 Singleton Pattern: DbClient 通过静态 Instance 属性访问
// 不需要在 DI 容器中注册，因为它自己管理生命周期

// Configure SignalR for Blazor Server with better error handling
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 102400000; // 100 MB
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Add application services with dependency injection
builder.Services.AddApplicationServices();

var app = builder.Build();

// 确保数据库已创建
try
{
    using var db = DbClient.Instance.GetDb();
    await db.Database.EnsureCreatedAsync();
    
    // Run manual migrations for existing databases
    await DatabaseMigrationHelper.AddProfilePhotoColumnAsync("Data Source=ai-clinic.db");
    await DatabaseMigrationHelper.AddMedicalDocumentFieldsAsync("Data Source=ai-clinic.db");
    await DatabaseMigrationHelper.AddDoctorSettingsColumnsAsync("Data Source=ai-clinic.db");
    await DatabaseMigrationHelper.MakeDocumentsConversationIdNullableAsync("Data Source=ai-clinic.db");
    await DatabaseMigrationHelper.AddAiConsultationSummaryColumnsAsync("Data Source=ai-clinic.db");
    await DatabaseMigrationHelper.AddAiModelManagementAsync("Data Source=ai-clinic.db");
    
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("数据库已准备就绪");
    
    // Initialize AI service with admin settings
    // Create a scope to resolve scoped services
    try
    {
        using var scope = app.Services.CreateScope();
        var aiService = scope.ServiceProvider.GetRequiredService<ai_clinic.Services.AiAssistantService>();
        await aiService.InitializeFromSettingsAsync();
        logger.LogInformation("AI service initialized with admin settings");
    }
    catch (Exception aiEx)
    {
        logger.LogWarning(aiEx, "Failed to initialize AI service with admin settings, using defaults");
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "数据库初始化时发生错误");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// 🔔 Map SignalR Hub for real-time consultation messaging
app.MapHub<ConsultationHub>("/consultationHub");

app.MapRazorComponents<ai_clinic.UI.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
