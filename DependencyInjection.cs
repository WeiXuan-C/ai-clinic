using AiClinic.Application.Commands;
using AiClinic.Application.DTOs;
using AiClinic.Application.Factories;
using AiClinic.Application.Services;
using AiClinic.Core.Interfaces;
using AiClinic.Infrastructure.Data;
using AiClinic.Infrastructure.Repositories;
using AiClinic.Infrastructure.Services;
using AiClinic.Presentation.Controllers;
using AiClinic.Presentation.State;
using Supabase;

namespace AiClinic;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Supabase Client
        services.AddScoped<Client>(provider =>
        {
            var url = configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL not configured");
            var key = configuration["Supabase:Key"] ?? throw new InvalidOperationException("Supabase Key not configured");
            
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };
            
            return new Client(url, key, options);
        });

        // Infrastructure - Data
        services.AddScoped<SupabaseContext>();

        // Infrastructure - Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<IDoctorRepository, DoctorRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IPatientProfileRepository, PatientProfileRepository>();
        services.AddScoped<IDoctorRatingRepository, DoctorRatingRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();

        // Application - Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IChatService, ChatService>();
        services.AddScoped<IDoctorAssignmentService, DoctorAssignmentService>();
        services.AddScoped<IAiService, AiService>();
        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<IDoctorService, DoctorService>();

        // Application - Factories (Abstract Factory Pattern)
        services.AddScoped<IMessageHandlerFactory, MessageHandlerFactory>();

        // Application - Command Handlers (Command Pattern)
        services.AddScoped<ICommandHandler<CreateConversationCommand, ConversationDto>, CreateConversationCommandHandler>();
        services.AddScoped<ICommandHandler<SendMessageCommand, MessageDto>, SendMessageCommandHandler>();

        // Presentation - State (Singleton Pattern)
        services.AddSingleton<AppState>(AppState.Instance);

        // Presentation - Controllers (Adapter Pattern)
        services.AddScoped<AuthController>();
        services.AddScoped<ChatController>();
        services.AddScoped<PatientController>();
        services.AddScoped<DoctorController>();

        return services;
    }
}
