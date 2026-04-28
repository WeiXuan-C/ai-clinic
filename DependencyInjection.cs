using AiClinic.Interfaces;
using AiClinic.DAOs;
using AiClinic.UI.State;
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
        services.AddScoped<IDoctorRepository, DoctorProfileDAO>();
        services.AddScoped<IDocumentRepository, DocumentDAO>();
        services.AddScoped<IPatientProfileRepository, PatientProfileDAO>();
        services.AddScoped<ISupportTicketRepository, SupportTicketDAO>();
        services.AddScoped<IAdminProfileRepository, AdminProfileDAO>();
        services.AddScoped<IDoctorRatingRepository, DoctorRatingDAO>();

        // Register State - Singleton lifetime (system requirement)
        services.AddSingleton<AuthState>();
        services.AddSingleton<ConversationState>();
        services.AddSingleton<MessageState>();
        services.AddSingleton<DoctorProfileState>();
        services.AddSingleton<PatientProfileState>();
        services.AddSingleton<AdminProfileState>();
        services.AddSingleton<SupportTicketState>();
        services.AddSingleton<DocumentState>();
        services.AddSingleton<DoctorRatingState>();

        // Register Services (Business Logic) - Scoped lifetime
        services.AddScoped<Services.AuthService>();
        services.AddScoped<Services.ConversationService>();
        services.AddScoped<Services.MessageService>();
        services.AddScoped<Services.DoctorProfileService>();
        services.AddScoped<Services.PatientProfileService>();
        services.AddScoped<Services.AdminProfileService>();
        services.AddScoped<Services.SupportTicketService>();
        services.AddScoped<Services.DocumentService>();
        services.AddScoped<Services.DoctorRatingService>();

        // Register Controllers (Facade Pattern) - Scoped lifetime
        services.AddScoped<Controller.AuthController>();
        services.AddScoped<Controller.ConversationController>();
        services.AddScoped<Controller.MessageController>();
        services.AddScoped<Controller.DoctorProfileController>();
        services.AddScoped<Controller.PatientProfileController>();
        services.AddScoped<Controller.AdminProfileController>();
        services.AddScoped<Controller.SupportTicketController>();
        services.AddScoped<Controller.DocumentController>();
        services.AddScoped<Controller.DoctorRatingController>();

        return services;
    }
}
