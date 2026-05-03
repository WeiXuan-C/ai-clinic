using ai_clinic.Models;

namespace ai_clinic.Services.Facades;

/// <summary>
/// 🎭 FACADE PATTERN - 咨询外观类
/// 为复杂的咨询系统提供统一的高层接口
/// 
/// 子系统包括:
/// - ConversationService: 管理对话
/// - MessageService: 管理消息
/// - DoctorProfileService: 管理医生信息
/// - ActivityLogService: 记录活动日志
/// 
/// 使用场景:
/// - 简化客户端代码，隐藏子系统复杂性
/// - 提供一站式的咨询功能接口
/// - 协调多个服务之间的交互
/// </summary>
public class ConsultationFacade
{
    // 子系统服务
    private readonly ConversationService _conversationService;
    private readonly MessageService _messageService;
    private readonly DoctorProfileService _doctorProfileService;
    private readonly ActivityLogService _activityLogService;

    public ConsultationFacade(
        ConversationService conversationService,
        MessageService messageService,
        DoctorProfileService doctorProfileService,
        ActivityLogService activityLogService)
    {
        _conversationService = conversationService;
        _messageService = messageService;
        _doctorProfileService = doctorProfileService;
        _activityLogService = activityLogService;
    }

    #region 创建咨询会话

    /// <summary>
    /// 创建 AI 咨询会话（简化接口）
    /// 内部协调: 创建对话 + 发送初始消息 + 记录日志
    /// </summary>
    public async Task<ConsultationSession> StartAiConsultationAsync(Guid patientId, string? initialMessage = null)
    {
        // 1. 创建对话
        var conversation = await _conversationService.CreateAiConversationAsync(patientId, initialMessage);

        // 2. 记录活动日志
        var messagePreview = initialMessage != null && initialMessage.Length > 50 
            ? initialMessage.Substring(0, 50) + "..." 
            : initialMessage ?? "";
        
        await _activityLogService.LogActivityAsync(
            patientId,
            "start_ai_consultation",
            $"{{\"conversation_id\": \"{conversation.Id}\", \"initial_message\": \"{messagePreview}\"}}"
        );

        // 3. 获取消息列表
        var messages = await _messageService.GetByConversationIdAsync(conversation.Id);

        // 4. 返回统一的会话对象
        return new ConsultationSession
        {
            Conversation = conversation,
            Messages = messages,
            IsAiConsultation = true,
            DoctorInfo = null
        };
    }

    /// <summary>
    /// 创建医生咨询会话（简化接口）
    /// 内部协调: 验证医生 + 创建对话 + 发送初始消息 + 记录日志
    /// </summary>
    public async Task<ConsultationSession> StartDoctorConsultationAsync(
        Guid patientId, 
        Guid doctorId, 
        string? initialMessage = null)
    {
        // 1. 获取医生信息（验证医生存在且可用）
        var doctorProfile = await _doctorProfileService.GetByUserIdAsync(doctorId);
        if (doctorProfile == null || !doctorProfile.IsActive || !doctorProfile.IsAcceptingPatients)
        {
            throw new InvalidOperationException("Doctor is not available for consultation");
        }

        // 2. 创建对话
        var conversation = await _conversationService.CreateDoctorConversationAsync(
            patientId, 
            doctorId, 
            initialMessage
        );

        // 3. 记录活动日志
        await _activityLogService.LogActivityAsync(
            patientId,
            "start_doctor_consultation",
            $"{{\"conversation_id\": \"{conversation.Id}\", \"doctor_id\": \"{doctorId}\", \"doctor_name\": \"{doctorProfile.FullName}\"}}"
        );

        // 4. 获取消息列表
        var messages = await _messageService.GetByConversationIdAsync(conversation.Id);

        // 5. 返回统一的会话对象
        return new ConsultationSession
        {
            Conversation = conversation,
            Messages = messages,
            IsAiConsultation = false,
            DoctorInfo = new DoctorInfo
            {
                UserId = doctorProfile.UserId,
                FullName = doctorProfile.FullName,
                Specialization = doctorProfile.PrimarySpecialization,
                ProfilePhotoUrl = doctorProfile.ProfilePhotoUrl,
                AvailabilityStatus = doctorProfile.AvailabilityStatus
            }
        };
    }

    #endregion

    #region 发送消息

    /// <summary>
    /// 发送患者消息（简化接口）
    /// 内部协调: 创建消息 + 更新对话 + 触发 AI 响应（如果是 AI 对话）
    /// </summary>
    public async Task<MessageResult> SendPatientMessageAsync(
        Guid conversationId, 
        Guid patientId, 
        string content)
    {
        // 1. 获取对话信息
        var conversation = await _conversationService.GetByIdAsync(conversationId);
        if (conversation == null)
        {
            throw new InvalidOperationException("Conversation not found");
        }

        // 2. 创建患者消息
        var patientMessage = await _messageService.CreatePatientMessageAsync(
            conversationId, 
            patientId, 
            content
        );

        // 3. 记录活动日志
        await _activityLogService.LogActivityAsync(
            patientId,
            "send_message",
            $"{{\"conversation_id\": \"{conversationId}\", \"message_id\": \"{patientMessage.Id}\", \"sender_type\": \"patient\"}}"
        );

        // 4. 如果是 AI 对话，触发 AI 响应
        Message? aiResponse = null;
        if (conversation.AssignedDoctorId == null)
        {
            aiResponse = await GenerateAiResponseAsync(conversationId, content);
        }

        return new MessageResult
        {
            PatientMessage = patientMessage,
            AiResponse = aiResponse,
            IsAiConversation = conversation.AssignedDoctorId == null
        };
    }

    /// <summary>
    /// 发送医生消息（简化接口）
    /// </summary>
    public async Task<Message> SendDoctorMessageAsync(
        Guid conversationId, 
        Guid doctorId, 
        string content)
    {
        // 1. 创建医生消息
        var doctorMessage = await _messageService.CreateDoctorMessageAsync(
            conversationId, 
            doctorId, 
            content
        );

        // 2. 记录活动日志
        await _activityLogService.LogActivityAsync(
            doctorId,
            "send_message",
            $"{{\"conversation_id\": \"{conversationId}\", \"message_id\": \"{doctorMessage.Id}\", \"sender_type\": \"doctor\"}}"
        );

        return doctorMessage;
    }

    #endregion

    #region 获取咨询信息

    /// <summary>
    /// 获取完整的咨询会话信息（简化接口）
    /// 内部协调: 获取对话 + 获取消息 + 获取医生信息 + 标记已读
    /// </summary>
    public async Task<ConsultationSession> GetConsultationSessionAsync(
        Guid conversationId, 
        Guid userId,
        UserRole userRole)
    {
        // 1. 获取对话
        var conversation = await _conversationService.GetByIdAsync(conversationId);
        if (conversation == null)
        {
            throw new InvalidOperationException("Conversation not found");
        }

        // 2. 获取消息列表
        var messages = await _messageService.GetByConversationIdAsync(conversationId);

        // 3. 标记消息为已读
        var excludeSenderType = userRole == UserRole.Patient 
            ? MessageSenderType.Patient 
            : MessageSenderType.Doctor;
        await _messageService.MarkConversationAsReadAsync(conversationId, excludeSenderType);

        // 4. 获取医生信息（如果有）
        DoctorInfo? doctorInfo = null;
        if (conversation.AssignedDoctorId.HasValue)
        {
            var doctorProfile = await _doctorProfileService.GetByUserIdAsync(conversation.AssignedDoctorId.Value);
            if (doctorProfile != null)
            {
                doctorInfo = new DoctorInfo
                {
                    UserId = doctorProfile.UserId,
                    FullName = doctorProfile.FullName,
                    Specialization = doctorProfile.PrimarySpecialization,
                    ProfilePhotoUrl = doctorProfile.ProfilePhotoUrl,
                    AvailabilityStatus = doctorProfile.AvailabilityStatus
                };
            }
        }

        return new ConsultationSession
        {
            Conversation = conversation,
            Messages = messages,
            IsAiConsultation = conversation.AssignedDoctorId == null,
            DoctorInfo = doctorInfo
        };
    }

    /// <summary>
    /// 获取患者的所有咨询列表（简化接口）
    /// </summary>
    public async Task<List<ConversationListItem>> GetPatientConsultationsAsync(Guid patientId)
    {
        return await _conversationService.GetConversationListByPatientIdAsync(patientId);
    }

    /// <summary>
    /// 获取可用医生列表（简化接口）
    /// </summary>
    public async Task<List<DoctorListItem>> GetAvailableDoctorsAsync()
    {
        return await _conversationService.GetAvailableDoctorsAsync();
    }

    #endregion

    #region 管理咨询

    /// <summary>
    /// 关闭咨询会话（简化接口）
    /// 内部协调: 更新状态 + 记录日志
    /// </summary>
    public async Task CloseConsultationAsync(Guid conversationId, Guid userId)
    {
        // 1. 更新对话状态
        await _conversationService.UpdateStatusAsync(conversationId, ConversationStatus.Closed);

        // 2. 记录活动日志
        await _activityLogService.LogActivityAsync(
            userId,
            "close_consultation",
            $"{{\"conversation_id\": \"{conversationId}\", \"status\": \"closed\"}}"
        );
    }

    /// <summary>
    /// 归档咨询会话（简化接口）
    /// </summary>
    public async Task ArchiveConsultationAsync(Guid conversationId, Guid userId)
    {
        // 1. 更新对话状态
        await _conversationService.UpdateStatusAsync(conversationId, ConversationStatus.Archived);

        // 2. 记录活动日志
        await _activityLogService.LogActivityAsync(
            userId,
            "archive_consultation",
            $"{{\"conversation_id\": \"{conversationId}\", \"status\": \"archived\"}}"
        );
    }

    /// <summary>
    /// 更新咨询标题（简化接口）
    /// </summary>
    public async Task UpdateConsultationTitleAsync(Guid conversationId, string title, Guid userId)
    {
        await _conversationService.UpdateTitleAsync(conversationId, title);

        await _activityLogService.LogActivityAsync(
            userId,
            "update_consultation_title",
            $"{{\"conversation_id\": \"{conversationId}\", \"new_title\": \"{title}\"}}"
        );
    }

    #endregion

    #region 私有辅助方法

    /// <summary>
    /// 生成 AI 响应（内部方法）
    /// 在实际项目中，这里会调用真实的 AI 服务（如 OpenAI、Claude 等）
    /// </summary>
    private async Task<Message> GenerateAiResponseAsync(Guid conversationId, string userMessage)
    {
        // 模拟 AI 思考时间
        await Task.Delay(1500);

        // 简单的响应生成逻辑（实际项目中应该调用 AI API）
        string aiResponse = GenerateSimpleAiResponse(userMessage);

        // 创建 AI 消息
        var aiMessage = await _messageService.CreateAiMessageAsync(
            conversationId,
            aiResponse,
            "GPT-4",
            0.85m
        );

        return aiMessage;
    }

    /// <summary>
    /// 简单的 AI 响应生成（示例）
    /// </summary>
    private string GenerateSimpleAiResponse(string userMessage)
    {
        var lowerMessage = userMessage.ToLower();

        if (lowerMessage.Contains("pain") || lowerMessage.Contains("疼痛"))
        {
            return "I understand you're experiencing pain. Can you describe the pain in more detail? For example:\n" +
                   "- Where exactly is the pain located?\n" +
                   "- Is it sharp or dull?\n" +
                   "- When did it start?\n" +
                   "- Does anything make it better or worse?\n\n" +
                   "This information will help me provide better guidance.";
        }
        else if (lowerMessage.Contains("fever") || lowerMessage.Contains("发烧"))
        {
            return "Fever can be a sign of infection. Have you measured your temperature? " +
                   "Are you experiencing any other symptoms like chills, body aches, or fatigue?\n\n" +
                   "If your fever is above 39°C (102°F) or persists for more than 3 days, " +
                   "I recommend consulting with a human doctor.";
        }
        else if (lowerMessage.Contains("headache") || lowerMessage.Contains("头痛"))
        {
            return "Headaches can have various causes. Let me ask you a few questions:\n" +
                   "- How long have you had this headache?\n" +
                   "- Is it constant or does it come and go?\n" +
                   "- On a scale of 1-10, how severe is the pain?\n" +
                   "- Are you experiencing any other symptoms like nausea or sensitivity to light?";
        }
        else if (lowerMessage.Contains("cough") || lowerMessage.Contains("咳嗽"))
        {
            return "I see you have a cough. To better understand your condition:\n" +
                   "- Is it a dry cough or are you producing mucus?\n" +
                   "- How long have you had this cough?\n" +
                   "- Do you have any other symptoms like fever or shortness of breath?\n" +
                   "- Have you been exposed to anyone who was sick recently?";
        }
        else if (lowerMessage.Contains("thank") || lowerMessage.Contains("谢谢"))
        {
            return "You're welcome! I'm here to help. If you have any other questions or concerns about your health, " +
                   "please don't hesitate to ask. Your well-being is my priority.";
        }
        else
        {
            return "Thank you for sharing that information. To provide you with the best possible guidance, " +
                   "could you tell me more about:\n" +
                   "- When did these symptoms start?\n" +
                   "- How severe are they on a scale of 1-10?\n" +
                   "- Have you tried any treatments or medications?\n\n" +
                   "The more details you provide, the better I can assist you.";
        }
    }

    #endregion
}

#region DTOs (Data Transfer Objects)

/// <summary>
/// 咨询会话完整信息
/// </summary>
public class ConsultationSession
{
    public Conversation Conversation { get; set; } = null!;
    public List<Message> Messages { get; set; } = new();
    public bool IsAiConsultation { get; set; }
    public DoctorInfo? DoctorInfo { get; set; }
}

/// <summary>
/// 医生信息摘要
/// </summary>
public class DoctorInfo
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
    public string? ProfilePhotoUrl { get; set; }
    public DoctorAvailabilityStatus AvailabilityStatus { get; set; }
}

/// <summary>
/// 消息发送结果
/// </summary>
public class MessageResult
{
    public Message PatientMessage { get; set; } = null!;
    public Message? AiResponse { get; set; }
    public bool IsAiConversation { get; set; }
}

#endregion
