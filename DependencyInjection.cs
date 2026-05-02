using ai_clinic.Services;
using ai_clinic.Services.Facades;

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

        // 🎭 Facade Pattern: High-level business operation coordinators
        // These facades manage multiple subsystems and provide simplified interfaces
        services.AddScoped<AuthFacade>();
        services.AddScoped<PatientFacade>();
        services.AddScoped<DoctorFacade>();
        services.AddScoped<AdminFacade>();

        return services;
    }
}
