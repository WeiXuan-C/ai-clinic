using MudBlazor.Services;
using ai_clinic.Data;
using ai_clinic;
using ai_clinic.Services.Hubs;
using QuestPDF.Infrastructure;
using System.Linq;

// Configure QuestPDF license (Community license for non-commercial use)
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add MudBlazor services
builder.Services.AddMudServices();

// 🔒 Singleton Pattern: DbClient accessed through static Instance property
// No need to register in DI container as it manages its own lifecycle

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

// Verify DbClient singleton pattern
if (DbClient.VerifySingletonInstance())
{
    Console.WriteLine("✓ DbClient Singleton Pattern Verified Successfully");
}

// Ensure database is created
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
    
     using var scope = app.Services.CreateScope();

}
catch
{
    // Database initialization error
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
