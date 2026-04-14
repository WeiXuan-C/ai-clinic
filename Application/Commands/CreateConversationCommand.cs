using AiClinic.Application.DTOs;
using AiClinic.Application.Services;

namespace AiClinic.Application.Commands;

public class CreateConversationCommand : ICommand<ConversationDto>
{
    public Guid PatientId { get; set; }

    public Task<ConversationDto> ExecuteAsync()
    {
        throw new NotImplementedException("Use handler");
    }
}

public class CreateConversationCommandHandler : ICommandHandler<CreateConversationCommand, ConversationDto>
{
    private readonly IChatService _chatService;

    public CreateConversationCommandHandler(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<ConversationDto> HandleAsync(CreateConversationCommand command)
    {
        return await _chatService.CreateConversationAsync(command.PatientId);
    }
}
