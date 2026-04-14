using AiClinic.Application.DTOs;

namespace AiClinic.Application.Factories;

// Abstract Factory Pattern
public interface IMessageHandlerFactory
{
    IMessageHandler CreateHandler(string messageType);
}

public interface IMessageHandler
{
    Task<MessageDto> HandleAsync(SendMessageRequest request);
}
