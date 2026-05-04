using ai_clinic.Models;
using ai_clinic.Data;

namespace ai_clinic.Services.Facades;

/// <summary>
/// FACADE PATTERN - Consultation Facade
/// Provides a unified high-level interface for the complex consultation system
/// 
/// Subsystems include:
/// - ConversationService: Manages conversations
/// - MessageService: Manages messages
/// - DoctorProfileService: Manages doctor information
/// - ActivityLogService: Records activity logs
/// - AiAssistantService: AI assistant service
/// 
/// Use cases:
/// - Simplifies client code, hides subsystem complexity
/// - Provides one-stop consultation functionality interface
/// - Coordinates interactions between multiple services
/// </summary>
public class ConsultationFacade
{
    // Subsystem services
    private readonly ConversationService _conversationService;
    private readonly MessageService _messageService;
    private readonly DoctorProfileService _doctorProfileService;
    private readonly ActivityLogService _activityLogService;
    private readonly AiAssistantService _aiAssistantService;

    public ConsultationFacade(
        ConversationService conversationService,
        MessageService messageService,
        DoctorProfileService doctorProfileService,
        ActivityLogService activityLogService,
        AiAssistantService aiAssistantService)
    {
        _conversationService = conversationService;
        _messageService = messageService;
        _doctorProfileService = doctorProfileService;
        _activityLogService = activityLogService;
        _aiAssistantService = aiAssistantService;
    }

    #region Create Consultation Session

    /// <summary>
    /// Creates AI consultation session (simplified interface)
    /// Internal coordination: Create conversation + Send initial message + Log activity
    /// </summary>
    public async Task<ConsultationSession> StartAiConsultationAsync(Guid patientId, string? initialMessage = null)
    {
        // 1. Create conversation
        var conversation = await _conversationService.CreateAiConversationAsync(patientId, initialMessage);

        // 2. Log activity
        var messagePreview = initialMessage != null && initialMessage.Length > 50 
            ? initialMessage.Substring(0, 50) + "..." 
            : initialMessage ?? "";
        
        await _activityLogService.LogActivityAsync(
            patientId,
            "start_ai_consultation",
            $"{{\"conversation_id\": \"{conversation.Id}\", \"initial_message\": \"{messagePreview}\"}}"
        );

        // 3. Get message list
        var messages = await _messageService.GetByConversationIdAsync(conversation.Id);

        // 4. Return unified session object
        return new ConsultationSession
        {
            Conversation = conversation,
            Messages = messages,
            IsAiConsultation = true,
            DoctorInfo = null
        };
    }

    /// <summary>
    /// Creates doctor consultation session (simplified interface)
    /// Internal coordination: Verify doctor + Create conversation + Send initial message + Log activity
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

        // 5. Return unified session object
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

    #region Send Messages

    /// <summary>
    /// Sends patient message (simplified interface)
    /// Internal coordination: Create message + Update conversation + Trigger AI response (if AI conversation)
    /// Uses transaction to ensure data consistency
    /// </summary>
    public async Task<MessageResult> SendPatientMessageAsync(
        Guid conversationId, 
        Guid patientId, 
        string content)
    {
        Console.WriteLine("=== [FACADE DEBUG] SendPatientMessageAsync Started ===");
        Console.WriteLine($"[FACADE] Conversation ID: {conversationId}");
        Console.WriteLine($"[FACADE] Patient ID: {patientId}");
        Console.WriteLine($"[FACADE] Content Length: {content.Length} chars");

        // 使用事务确保所有操作的原子性
        using var db = DbClient.Instance.GetDb();
        using var transaction = await db.Database.BeginTransactionAsync();
        
        try
        {
            // 1. 获取对话信息
            Console.WriteLine("[FACADE] Step 1: Getting conversation...");
            var conversation = await db.Conversations.FindAsync(conversationId);
            if (conversation == null)
            {
                Console.WriteLine("[FACADE ERROR] Conversation not found!");
                throw new InvalidOperationException("Conversation not found");
            }
            Console.WriteLine($"[FACADE] Conversation found - Is AI: {conversation.AssignedDoctorId == null}");

            // 2. 创建患者消息
            Console.WriteLine("[FACADE] Step 2: Creating patient message...");
            var patientMessage = new Message
            {
                ConversationId = conversationId,
                SenderId = patientId,
                SenderType = MessageSenderType.Patient,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };
            
            db.Messages.Add(patientMessage);
            
            // 更新对话的最后消息时间和计数
            conversation.LastMessageAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;
            conversation.TotalMessages++;
            
            await db.SaveChangesAsync();
            Console.WriteLine($"[FACADE] Patient message created - ID: {patientMessage.Id}");

            // 3. 记录活动日志
            Console.WriteLine("[FACADE] Step 3: Logging activity...");
            var activityLog = new ActivityLog
            {
                UserId = patientId,
                Action = "send_message",
                Details = $"{{\"conversation_id\": \"{conversationId}\", \"message_id\": \"{patientMessage.Id}\", \"sender_type\": \"patient\"}}",
                CreatedAt = DateTime.UtcNow
            };
            db.ActivityLogs.Add(activityLog);
            await db.SaveChangesAsync();
            Console.WriteLine("[FACADE] Activity logged");

            // 4. 如果是 AI 对话，触发 AI 响应
            Message? aiResponse = null;
            if (conversation.AssignedDoctorId == null)
            {
                Console.WriteLine("[FACADE] Step 4: This is an AI conversation, generating AI response...");
                
                try
                {
                    // 生成 AI 响应（这个操作在事务外部，因为它调用外部 API）
                    var aiResponseContent = await GenerateAiResponseContentAsync(content);
                    
                    // 创建 AI 消息（在事务内）
                    aiResponse = new Message
                    {
                        ConversationId = conversationId,
                        SenderId = null,
                        SenderType = MessageSenderType.AI,
                        Content = aiResponseContent.Content,
                        AiModelUsed = aiResponseContent.ModelUsed,
                        AiConfidenceScore = aiResponseContent.ConfidenceScore,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    db.Messages.Add(aiResponse);
                    
                    // 更新对话统计
                    conversation.LastMessageAt = DateTime.UtcNow;
                    conversation.UpdatedAt = DateTime.UtcNow;
                    conversation.TotalMessages++;
                    conversation.AiMessagesCount++;
                    
                    await db.SaveChangesAsync();
                    Console.WriteLine($"[FACADE] AI response created - ID: {aiResponse.Id}, Length: {aiResponse.Content.Length} chars");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FACADE WARNING] AI response generation failed: {ex.Message}");
                    Console.WriteLine("[FACADE] Continuing without AI response (patient message will be saved)");
                    // AI 响应失败不影响患者消息的保存
                    // 但我们仍然在事务中，所以如果需要可以回滚
                }
            }
            else
            {
                Console.WriteLine("[FACADE] Step 4: This is a doctor conversation, skipping AI response");
            }

            // 提交事务
            await transaction.CommitAsync();
            Console.WriteLine("[FACADE] Transaction committed successfully");
            Console.WriteLine("=== [FACADE DEBUG] SendPatientMessageAsync Completed ===\n");

            return new MessageResult
            {
                PatientMessage = patientMessage,
                AiResponse = aiResponse,
                IsAiConversation = conversation.AssignedDoctorId == null
            };
        }
        catch (Exception ex)
        {
            // 回滚事务
            await transaction.RollbackAsync();
            Console.WriteLine("=== [FACADE ERROR] Transaction rolled back ===");
            Console.WriteLine($"[FACADE ERROR] Exception Type: {ex.GetType().Name}");
            Console.WriteLine($"[FACADE ERROR] Message: {ex.Message}");
            Console.WriteLine($"[FACADE ERROR] Stack Trace: {ex.StackTrace}");
            throw;
        }
    }

    /// <summary>
    /// Sends doctor message (simplified interface)
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

        // 2. Log activity
        await _activityLogService.LogActivityAsync(
            doctorId,
            "send_message",
            $"{{\"conversation_id\": \"{conversationId}\", \"message_id\": \"{doctorMessage.Id}\", \"sender_type\": \"doctor\"}}"
        );

        return doctorMessage;
    }

    #endregion

    #region Get Consultation Information

    /// <summary>
    /// Gets complete consultation session information (simplified interface)
    /// Internal coordination: Get conversation + Get messages + Get doctor info + Mark as read
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
    /// Gets all patient consultations list (simplified interface)
    /// </summary>
    public async Task<List<ConversationListItem>> GetPatientConsultationsAsync(Guid patientId)
    {
        return await _conversationService.GetConversationListByPatientIdAsync(patientId);
    }

    /// <summary>
    /// Gets available doctors list (simplified interface)
    /// </summary>
    public async Task<List<DoctorListItem>> GetAvailableDoctorsAsync()
    {
        return await _conversationService.GetAvailableDoctorsAsync();
    }

    #endregion

    #region Manage Consultation

    /// <summary>
    /// Closes consultation session (simplified interface)
    /// Internal coordination: Update status + Log activity
    /// </summary>
    public async Task CloseConsultationAsync(Guid conversationId, Guid userId)
    {
        // 1. 更新对话状态
        await _conversationService.UpdateStatusAsync(conversationId, ConversationStatus.Closed);

        // 2. Log activity
        await _activityLogService.LogActivityAsync(
            userId,
            "close_consultation",
            $"{{\"conversation_id\": \"{conversationId}\", \"status\": \"closed\"}}"
        );
    }

    /// <summary>
    /// Archives consultation session (simplified interface)
    /// </summary>
    public async Task ArchiveConsultationAsync(Guid conversationId, Guid userId)
    {
        // 1. 更新对话状态
        await _conversationService.UpdateStatusAsync(conversationId, ConversationStatus.Archived);

        // 2. Log activity
        await _activityLogService.LogActivityAsync(
            userId,
            "archive_consultation",
            $"{{\"conversation_id\": \"{conversationId}\", \"status\": \"archived\"}}"
        );
    }

    /// <summary>
    /// Updates consultation title (simplified interface)
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

    #region Private Helper Methods

    /// <summary>
    /// Generates AI response content (does not involve database operations)
    /// This method only calls AI API, returns response content and metadata
    /// </summary>
    private async Task<AiResponseContent> GenerateAiResponseContentAsync(string userMessage)
    {
        Console.WriteLine("=== [AI GENERATION DEBUG] GenerateAiResponseContentAsync Started ===");
        Console.WriteLine($"[AI GEN] User Message: {userMessage}");
        Console.WriteLine($"[AI GEN] Current AI Model: {_aiAssistantService.CurrentModelName}");
        
        try
        {
            // 调用真实的 AI 服务
            Console.WriteLine("[AI GEN] Calling AiAssistantService.GenerateMedicalResponseAsync...");
            string aiResponse = await _aiAssistantService.GenerateMedicalResponseAsync(
                patientQuery: userMessage,
                medicalContext: null,
                temperature: 0.7
            );
            
            Console.WriteLine($"[AI GEN] Response received from AI - Length: {aiResponse.Length} chars");
            Console.WriteLine($"[AI GEN] Response preview: {aiResponse.Substring(0, Math.Min(150, aiResponse.Length))}...");
            Console.WriteLine("=== [AI GENERATION DEBUG] GenerateAiResponseContentAsync Completed ===\n");

            return new AiResponseContent
            {
                Content = aiResponse,
                ModelUsed = _aiAssistantService.CurrentModelName,
                ConfidenceScore = 0.85m
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("=== [AI GENERATION ERROR] ===");
            Console.WriteLine($"[AI GEN ERROR] Exception Type: {ex.GetType().Name}");
            Console.WriteLine($"[AI GEN ERROR] Message: {ex.Message}");
            Console.WriteLine($"[AI GEN ERROR] Stack Trace: {ex.StackTrace}");
            
            // 如果 AI 调用失败，返回错误消息
            Console.WriteLine("[AI GEN] Returning error message...");
            Console.WriteLine("=== [AI GENERATION ERROR] Fallback message returned ===\n");
            
            return new AiResponseContent
            {
                Content = "I apologize, but I'm having trouble processing your request right now. Please try again in a moment, or consider consulting with a human doctor for immediate assistance.",
                ModelUsed = "Error",
                ConfidenceScore = 0.0m
            };
        }
    }

    #endregion
}

#region DTOs (Data Transfer Objects)

/// <summary>
/// AI response content (for transaction management)
/// </summary>
internal class AiResponseContent
{
    public string Content { get; set; } = string.Empty;
    public string ModelUsed { get; set; } = string.Empty;
    public decimal ConfidenceScore { get; set; }
}

/// <summary>
/// Complete consultation session information
/// </summary>
public class ConsultationSession
{
    public Conversation Conversation { get; set; } = null!;
    public List<Message> Messages { get; set; } = new();
    public bool IsAiConsultation { get; set; }
    public DoctorInfo? DoctorInfo { get; set; }
}

/// <summary>
/// Doctor information summary
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
/// Message send result
/// </summary>
public class MessageResult
{
    public Message PatientMessage { get; set; } = null!;
    public Message? AiResponse { get; set; }
    public bool IsAiConversation { get; set; }
}

#endregion
