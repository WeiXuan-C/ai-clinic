using ai_clinic.Services;
using ai_clinic.Services.Facades;
using ai_clinic.Services.AI;

namespace ai_clinic;

/// <summary>
/// Dependency Injection Configuration
/// Registers all services, adapters, and strategies with proper lifetimes
/// </summary>
public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // 🎯 Singleton Pattern: DbClient is accessed via static Instance property
        // No need to register in DI container as it manages its own lifecycle

        // 📦 Base Services - All services use DbClient singleton as entry point
        services.AddScoped<UserService>();
        services.AddScoped<DocumentService>();
        services.AddScoped<ConversationService>();
        services.AddScoped<MessageService>();

        // Patient-related services
        services.AddScoped<PatientProfileService>();
        services.AddScoped<MedicalRecordService>();
        services.AddScoped<ConsultationService>();
        services.AddScoped<PrescriptionService>();
        services.AddScoped<SupportTicketService>();
        services.AddScoped<PatientSettingsService>();

        // Doctor-related services
        services.AddScoped<DoctorProfileService>();

        // System services
        services.AddScoped<ActivityLogService>();
        services.AddScoped<StatisticsService>();
        services.AddScoped<AuthStateService>(); // Authentication state management
        services.AddScoped<AdminService>(); // Admin operations service

        // 🔔 Real-time Services - SignalR for live messaging
        // Scoped lifetime ensures proper lifecycle management in Blazor Server
        services.AddScoped<SignalRConsultationService>();

        // 🎭 Facade Pattern: High-level business operation coordinators
        // These facades manage multiple subsystems and provide simplified interfaces
        services.AddScoped<AuthFacade>();
        services.AddScoped<PatientFacade>();
        services.AddScoped<DoctorFacade>();
        services.AddScoped<AdminFacade>();
        services.AddScoped<ConsultationFacade>(); // 咨询外观 - 协调对话、消息、医生、SignalR等子系统

        // 🤖 AI Services - Strategy & Adapter Patterns
        // Adaptee: OpenRouter API client (the external API we're adapting)
        services.AddHttpClient<OpenRouterApiClient>();
        services.AddScoped<OpenRouterApiClient>();

        services.AddScoped<Services.DoctorRecommendation.DoctorRecommendationService>();       
        
        // Context: Manages strategy selection and execution
        services.AddScoped<AiModelContext>();
        
        // High-level service: Provides domain-specific AI functionality
        services.AddScoped<AiAssistantService>();
        
        // 🎭 AI Facade: Unified interface for AI model switching and generation
        // Coordinates: AiModelContext + AiAssistantService + ActivityLogService
        services.AddScoped<AiFacade>(); // AI外观 - 协调模型切换、响应生成、日志记录等子系统

        return services;
    }
}
