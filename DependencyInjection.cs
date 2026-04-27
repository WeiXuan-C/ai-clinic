using AiClinic.Core.Interfaces;
using AiClinic.DAOs;
using AiClinic.Services;
using AiClinic.Controller;
using AiClinic.UI.State;
using AiClinic.Factories;
using AiClinic.Database;

namespace AiClinic;

/// <summary>
/// Dependency Injection Configuration
/// Registers all services, DAOs, controllers, and state with proper lifetimes
/// </summary>
public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Supabase HTTP Client (direct REST API calls)
        var supabaseUrl = configuration["Supabase:Url"] 
            ?? throw new Exception("Supabase URL not configured");
        var supabaseKey = configuration["Supabase:Key"] 
            ?? throw new Exception("Supabase Key not configured");

        services.AddScoped<SupabaseHttpClient>(_ => 
            new SupabaseHttpClient(supabaseUrl, supabaseKey));

        // Keep Supabase client for Auth only
        services.AddScoped<Supabase.Client>(_ => 
            new Supabase.Client(supabaseUrl, supabaseKey));

        // Register DAOs (Adapter Pattern) - Scoped lifetime
        services.AddScoped<IUserRepository, UserDAO>();
        services.AddScoped<IConversationRepository, ConversationDAO>();
        services.AddScoped<IMessageRepository, MessageDAO>();
        services.AddScoped<IDoctorRepository, DoctorDAO>();
        services.AddScoped<IDocumentRepository, DocumentDAO>();
        services.AddScoped<IPatientProfileRepository, PatientProfileDAO>();
        services.AddScoped<ISupportTicketRepository, SupportTicketDAO>();

        // Register Services (Business Logic) - Scoped lifetime
        services.AddScoped<AuthService>();
        services.AddScoped<ChatService>();
        services.AddScoped<DoctorService>();
        services.AddScoped<PatientService>();

        // Register Factories (Abstract Factory Pattern) - Scoped lifetime
        services.AddScoped<IServiceFactory, ServiceFactory>();

        // Register Controllers (Facade Pattern) - Scoped lifetime
        services.AddScoped<AuthController>();
        services.AddScoped<ChatController>();
        services.AddScoped<DoctorController>();
        services.AddScoped<PatientController>();

        // Register State - Singleton lifetime (system requirement)
        services.AddSingleton<AuthState>();
        services.AddSingleton<ChatState>();
        services.AddSingleton<DoctorState>();

        return services;
    }
}
