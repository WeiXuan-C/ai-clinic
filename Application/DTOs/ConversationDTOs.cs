namespace AiClinic.Application.DTOs;

public record CreateConversationRequest(Guid PatientId);

public record SendMessageRequest(
    Guid ConversationId,
    Guid? SenderId,
    string Content,
    string SenderType
);

public record ConversationDto(
    Guid Id,
    Guid PatientId,
    Guid? DoctorId,
    string Status,
    string Type,
    DateTime CreatedAt,
    List<MessageDto> Messages
);

public record MessageDto(
    Guid Id,
    string Content,
    string SenderType,
    DateTime SentAt,
    bool IsRead
);
