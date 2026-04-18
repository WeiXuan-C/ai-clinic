using AiClinic.Core.Interfaces;
using AiClinic.Services;

namespace AiClinic.Factories;

/// <summary>
/// Abstract Factory Pattern Implementation
/// Creates service instances with proper dependencies
/// </summary>
public interface IServiceFactory
{
    AuthService CreateAuthService();
    ChatService CreateChatService();
    DoctorService CreateDoctorService();
}

public class ServiceFactory : IServiceFactory
{
    private readonly IUserRepository _userRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IConversationRepository _conversationRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly IDoctorRepository _doctorRepository;

    public ServiceFactory(
        IUserRepository userRepository,
        IMessageRepository messageRepository,
        IConversationRepository conversationRepository,
        IDocumentRepository documentRepository,
        IDoctorRepository doctorRepository)
    {
        _userRepository = userRepository;
        _messageRepository = messageRepository;
        _conversationRepository = conversationRepository;
        _documentRepository = documentRepository;
        _doctorRepository = doctorRepository;
    }

    /// <summary>
    /// Creates an AuthService instance with required dependencies
    /// </summary>
    public AuthService CreateAuthService()
    {
        return new AuthService(_userRepository);
    }

    /// <summary>
    /// Creates a ChatService instance with required dependencies
    /// </summary>
    public ChatService CreateChatService()
    {
        return new ChatService(
            _messageRepository,
            _conversationRepository,
            _documentRepository);
    }

    /// <summary>
    /// Creates a DoctorService instance with required dependencies
    /// </summary>
    public DoctorService CreateDoctorService()
    {
        return new DoctorService(
            _doctorRepository,
            _conversationRepository);
    }
}
