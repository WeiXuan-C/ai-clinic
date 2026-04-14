using AiClinic.Application.DTOs;
using AiClinic.Application.Services;

namespace AiClinic.Application.Commands;

public class SendMessageCommand : ICommand<MessageDto>
{
    public Guid ConversationId { get; set; }
    public Guid? SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string SenderType { get; set; } = string.Empty;

    public Task<MessageDto> ExecuteAsync()
    {
        throw new NotImplementedException("Use handler");
    }
}

public class SendMessageCommandHandler : ICommandHandler<SendMessageCommand, MessageDto>
{
    private readonly IChatService _chatService;

    public SendMessageCommandHandler(IChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<MessageDto> HandleAsync(SendMessageCommand command)
    {
        var request = new SendMessageRequest(
            command.ConversationId,
            command.SenderId,
            command.Content,
            command.SenderType
        );
        
        return await _chatService.SendMessageAsync(request);
    }
}
