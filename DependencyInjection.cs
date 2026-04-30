using ai_clinic.Interfaces;
using ai_clinic.DAOs;
using ai_clinic.UI.State;
using ai_clinic.Database;

namespace ai_clinic;

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

        // Register Supabase Auth Client (Adapter Pattern)
        services.AddScoped<ISupabaseAuthClient, SupabaseAuthClient>();

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

        // Register State - Scoped for Blazor Server (prevents data leakage between users)
        // Use AddSingleton only if using Blazor WebAssembly
        services.AddScoped<AuthState>();
        services.AddScoped<ConversationState>();
        services.AddScoped<MessageState>();
        services.AddScoped<DoctorProfileState>();
        services.AddScoped<PatientProfileState>();
        services.AddScoped<AdminProfileState>();
        services.AddScoped<SupportTicketState>();
        services.AddScoped<DocumentState>();
        services.AddScoped<DoctorRatingState>();

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
