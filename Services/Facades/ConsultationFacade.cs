using ai_clinic.Models;
using ai_clinic.Data;
using ai_clinic.Services.Hubs;
using ai_clinic.Services.DoctorRecommendation;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

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
/// - ConsultationHub (SignalR): Real-time messaging
/// 
/// Use cases:
/// - Simplifies client code, hides subsystem complexity
/// - Provides one-stop consultation functionality interface
/// - Coordinates interactions between multiple services
/// - Enables real-time message delivery via SignalR
/// </summary>
public class ConsultationFacade
{
    // Subsystem services
    private readonly ConversationService _conversationService;
    private readonly MessageService _messageService;
    private readonly DoctorProfileService _doctorProfileService;
    private readonly ActivityLogService _activityLogService;
    private readonly AiAssistantService _aiAssistantService;
    private readonly DoctorRecommendationService _doctorRecommendationService;
    private readonly IHubContext<ConsultationHub> _hubContext;
    private readonly ILogger<ConsultationFacade> _logger;

    public ConsultationFacade(
        ConversationService conversationService,
        MessageService messageService,
        DoctorProfileService doctorProfileService,
        ActivityLogService activityLogService,
        AiAssistantService aiAssistantService,
        DoctorRecommendationService doctorRecommendationService,
        IHubContext<ConsultationHub> hubContext,
        ILogger<ConsultationFacade> logger)
    {
        _conversationService = conversationService;
        _messageService = messageService;
        _doctorProfileService = doctorProfileService;
        _activityLogService = activityLogService;
        _aiAssistantService = aiAssistantService;
        _doctorRecommendationService = doctorRecommendationService;
        _hubContext = hubContext;
        _logger = logger;
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
        string? initialMessage = null,
        Guid? sourceConversationId = null)
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

        // 3. 如果是从AI对话转来的，自动发送病情总结给医生
        if (sourceConversationId.HasValue)
        {
            try
            {
                var summary = await GeneratePatientConditionSummaryAsync(sourceConversationId.Value);
                if (!string.IsNullOrWhiteSpace(summary))
                {
                    // 创建系统消息发送给医生
                    var summaryMessage = new Message
                    {
                        ConversationId = conversation.Id,
                        SenderId = null, // System message
                        SenderType = MessageSenderType.AI,
                        Content = summary,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    await _messageService.CreateAsync(summaryMessage);
                    
                    // 通过 SignalR 发送给医生
                    await _hubContext.SendMessageToConversation(conversation.Id, summaryMessage);
                    
                    _logger.LogInformation("[FACADE] Sent patient condition summary to doctor {DoctorId} for conversation {ConversationId}", 
                        doctorId, conversation.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[FACADE] Failed to generate patient condition summary for conversation {ConversationId}", 
                    sourceConversationId.Value);
                // 不影响主流程，继续执行
            }
        }

        // 4. 记录活动日志
        await _activityLogService.LogActivityAsync(
            patientId,
            "start_doctor_consultation",
            $"{{\"conversation_id\": \"{conversation.Id}\", \"doctor_id\": \"{doctorId}\", \"doctor_name\": \"{doctorProfile.FullName}\"}}"
        );

        // 5. 获取消息列表
        var messages = await _messageService.GetByConversationIdAsync(conversation.Id);

        // 6. Return unified session object
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
    /// Internal coordination: Create message + Update conversation + Trigger AI response (if AI conversation) + Send via SignalR
    /// Uses transaction to ensure data consistency
    /// </summary>
    public async Task<MessageResult> SendPatientMessageAsync(
        Guid conversationId, 
        Guid patientId, 
        string content)
    {
        _logger.LogInformation("=== [FACADE] SendPatientMessageAsync Started ===");
        _logger.LogInformation($"[FACADE] Conversation ID: {conversationId}, Patient ID: {patientId}");

        // 使用事务确保所有操作的原子性
        using var db = DbClient.Instance.GetDb();
        using var transaction = await db.Database.BeginTransactionAsync();
        
        try
        {
            // 1. 获取对话信息
            var conversation = await db.Conversations.FindAsync(conversationId);
            if (conversation == null)
            {
                throw new InvalidOperationException("Conversation not found");
            }

            // 2. 创建患者消息
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
            _logger.LogInformation($"[FACADE] Patient message created - ID: {patientMessage.Id}");

            // 🔔 REAL-TIME: Send patient message via SignalR
            await _hubContext.SendMessageToConversation(conversationId, patientMessage);

            // 3. 记录活动日志
            var activityLog = new ActivityLog
            {
                UserId = patientId,
                Action = "send_message",
                Details = $"{{\"conversation_id\": \"{conversationId}\", \"message_id\": \"{patientMessage.Id}\", \"sender_type\": \"patient\"}}",
                CreatedAt = DateTime.UtcNow
            };
            db.ActivityLogs.Add(activityLog);
            await db.SaveChangesAsync();

            // 4. 如果是 AI 对话，触发 AI 响应
            Message? aiResponse = null;
            if (conversation.AssignedDoctorId == null)
            {
                _logger.LogInformation("[FACADE] AI conversation detected, generating response...");
                
                try
                {
                    // 🔔 REAL-TIME: Notify that AI is thinking
                    await _hubContext.SendAiStatusUpdate(conversationId, "thinking", "AI is processing your message...");
                    
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
                    _logger.LogInformation($"[FACADE] AI response created - ID: {aiResponse.Id}");

                    // 🔔 REAL-TIME: Send AI response via SignalR
                    await _hubContext.SendMessageToConversation(conversationId, aiResponse);
                    
                    // 🔔 REAL-TIME: Clear AI status
                    await _hubContext.SendAiStatusUpdate(conversationId, "ready", null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[FACADE] AI response generation failed");
                    
                    // 🔔 REAL-TIME: Notify AI error
                    await _hubContext.SendAiStatusUpdate(conversationId, "error", "AI response failed. Please try again.");
                }
            }

            // 提交事务
            await transaction.CommitAsync();
            _logger.LogInformation("[FACADE] Transaction committed successfully");

            return new MessageResult
            {
                PatientMessage = patientMessage,
                AiResponse = aiResponse,
                IsAiConversation = conversation.AssignedDoctorId == null
            };
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "[FACADE] Transaction rolled back");
            throw;
        }
    }

    /// <summary>
    /// Sends doctor message (simplified interface)
    /// Internal coordination: Create message + Update conversation + Send via SignalR
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

        // 🔔 REAL-TIME: Send doctor message via SignalR
        await _hubContext.SendMessageToConversation(conversationId, doctorMessage);

        // 2. Log activity
        await _activityLogService.LogActivityAsync(
            doctorId,
            "send_message",
            $"{{\"conversation_id\": \"{conversationId}\", \"message_id\": \"{doctorMessage.Id}\", \"sender_type\": \"doctor\"}}"
        );

        return doctorMessage;
    }

    #endregion

    #region Send Messages with Streaming

    /// <summary>
    /// Sends patient message with streaming AI response
    /// Returns an async enumerable that yields message chunks as they arrive
    /// </summary>
    public async IAsyncEnumerable<StreamingMessageChunk> SendPatientMessageWithStreamingAsync(
        Guid conversationId,
        Guid patientId,
        string content)
    {
        _logger.LogInformation("=== [FACADE] SendPatientMessageWithStreamingAsync Started ===");

        // 1. Save patient message first
        Message patientMessage;
        Conversation conversation;
        
        using (var db = DbClient.Instance.GetDb())
        {
            var transaction = await db.Database.BeginTransactionAsync();

            conversation = await db.Conversations.FindAsync(conversationId);
            if (conversation == null)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Conversation not found");
            }

            patientMessage = new Message
            {
                ConversationId = conversationId,
                SenderId = patientId,
                SenderType = MessageSenderType.Patient,
                Content = content,
                CreatedAt = DateTime.UtcNow
            };

            db.Messages.Add(patientMessage);
            conversation.LastMessageAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;
            conversation.TotalMessages++;

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation($"[FACADE] Patient message created - ID: {patientMessage.Id}");
        }

        // Yield user message
        yield return new StreamingMessageChunk
        {
            IsUserMessage = true,
            Message = patientMessage
        };

        // 2. Generate AI response if needed
        if (conversation.AssignedDoctorId == null)
        {
            _logger.LogInformation("[FACADE] AI conversation detected, generating streaming response...");

            var fullResponse = "";
            var success = false;

            // Stream AI response chunks
            await foreach (var chunk in GenerateAiResponseWithFallbackAsync(content))
            {
                fullResponse += chunk;
                success = true;
                
                yield return new StreamingMessageChunk
                {
                    IsAiChunk = true,
                    Content = chunk
                };
            }

            // Save AI response to database
            if (!success || string.IsNullOrEmpty(fullResponse))
            {
                fullResponse = "I apologize, but I'm experiencing technical difficulties at the moment. Please try again in a few moments, or consider consulting with one of our human doctors for immediate assistance.";
            }

            using (var db2 = DbClient.Instance.GetDb())
            {
                var transaction2 = await db2.Database.BeginTransactionAsync();

                var aiResponse = new Message
                {
                    ConversationId = conversationId,
                    SenderId = null,
                    SenderType = MessageSenderType.AI,
                    Content = fullResponse,
                    AiModelUsed = _aiAssistantService.CurrentModelName,
                    AiConfidenceScore = success ? 0.85m : 0.0m,
                    CreatedAt = DateTime.UtcNow
                };

                db2.Messages.Add(aiResponse);

                var conv = await db2.Conversations.FindAsync(conversationId);
                if (conv != null)
                {
                    conv.LastMessageAt = DateTime.UtcNow;
                    conv.UpdatedAt = DateTime.UtcNow;
                    conv.TotalMessages++;
                    conv.AiMessagesCount++;
                }

                await db2.SaveChangesAsync();
                await transaction2.CommitAsync();

                _logger.LogInformation($"[FACADE] AI response saved - ID: {aiResponse.Id}");

                // Yield complete message
                yield return new StreamingMessageChunk
                {
                    IsComplete = true,
                    Message = aiResponse
                };
            }
        }
    }

    /// <summary>
    /// Generates AI response with automatic fallback to other models
    /// </summary>
    private async IAsyncEnumerable<string> GenerateAiResponseWithFallbackAsync(string content)
    {
        _logger.LogInformation("[FACADE] GenerateAiResponseWithFallbackAsync started");
        
        var attemptedModels = new List<string>();
        var currentModel = _aiAssistantService.CurrentModelName;
        var hasYieldedAny = false;

        _logger.LogInformation($"[FACADE] Trying primary model: {currentModel}");
        attemptedModels.Add(currentModel);
        
        // Try current model first
        await foreach (var result in TryGenerateStreamAsync(currentModel, content))
        {
            if (result.Success && result.Chunk != null)
            {
                hasYieldedAny = true;
                yield return result.Chunk;
            }
        }

        // If primary model failed or didn't yield anything, try fallback
        if (!hasYieldedAny)
        {
            _logger.LogWarning("[FACADE] Primary model failed, trying fallback models");
            
            var availableModels = _aiAssistantService.GetAvailableModels();
            var fallbackModels = availableModels
                .Where(m => !attemptedModels.Contains(m.DisplayName))
                .ToList();

            _logger.LogInformation($"[FACADE] Found {fallbackModels.Count} fallback models");

            foreach (var fallbackModel in fallbackModels)
            {
                _logger.LogInformation($"[FACADE] Trying fallback model: {fallbackModel.DisplayName}");
                _aiAssistantService.SwitchModel(fallbackModel.Key);
                attemptedModels.Add(fallbackModel.DisplayName);

                await foreach (var result in TryGenerateStreamAsync(fallbackModel.DisplayName, content))
                {
                    if (result.Success && result.Chunk != null)
                    {
                        hasYieldedAny = true;
                        yield return result.Chunk;
                    }
                }

                if (hasYieldedAny)
                {
                    _logger.LogInformation($"[FACADE] Fallback model {fallbackModel.DisplayName} succeeded");
                    break;
                }
            }
        }

        if (!hasYieldedAny)
        {
            _logger.LogError("[FACADE] All models failed to generate response");
        }
        else
        {
            _logger.LogInformation("[FACADE] GenerateAiResponseWithFallbackAsync completed successfully");
        }
    }

    /// <summary>
    /// Helper method to try generating stream from a model
    /// </summary>
    private async IAsyncEnumerable<StreamResult> TryGenerateStreamAsync(string modelName, string content)
    {
        var chunkCount = 0;
        Exception? caughtException = null;
        
        IAsyncEnumerator<string>? enumerator = null;
        
        var stream = _aiAssistantService.GenerateStreamingMedicalResponseAsync(
            patientQuery: content,
            medicalContext: null,
            temperature: 0.7);
        
        enumerator = stream.GetAsyncEnumerator();
        
        try
        {
            while (true)
            {
                bool hasMore = false;
                string? chunk = null;
                bool moveSuccess = false;
                
                try
                {
                    hasMore = await enumerator.MoveNextAsync();
                    moveSuccess = true;
                    if (hasMore)
                    {
                        chunk = enumerator.Current;
                    }
                }
                catch (Exception ex)
                {
                    caughtException = ex;
                    _logger.LogWarning(ex, $"[FACADE] Model '{modelName}' failed during streaming: {ex.Message}");
                }
                
                if (!moveSuccess || !hasMore)
                    break;
                
                if (chunk != null)
                {
                    chunkCount++;
                    _logger.LogInformation($"[FACADE] Received chunk #{chunkCount} from {modelName}, length: {chunk.Length}");
                    yield return new StreamResult { Success = true, Chunk = chunk };
                }
            }
            
            if (caughtException == null)
            {
                _logger.LogInformation($"[FACADE] Model {modelName} completed successfully, total chunks: {chunkCount}");
            }
        }
        finally
        {
            if (enumerator != null)
            {
                await enumerator.DisposeAsync();
            }
        }
    }

    private class StreamResult
    {
        public bool Success { get; set; }
        public string? Chunk { get; set; }
    }

    #endregion

    #region Send Messages with Streaming and Attachments

    /// <summary>
    /// Sends patient message with attachments and streaming AI response
    /// Returns an async enumerable that yields message chunks as they arrive
    /// </summary>
    public async IAsyncEnumerable<StreamingMessageChunk> SendPatientMessageWithAttachmentsAsync(
        Guid conversationId,
        Guid patientId,
        string content,
        List<Guid> documentIds)
    {
        _logger.LogInformation("=== [FACADE] SendPatientMessageWithAttachmentsAsync Started ===");
        _logger.LogInformation($"[FACADE] Attachments: {documentIds.Count}");

        // 1. Save patient message first
        Message patientMessage;
        Conversation conversation;
        string? attachmentContext = null;
        
        using (var db = DbClient.Instance.GetDb())
        {
            var transaction = await db.Database.BeginTransactionAsync();

            conversation = await db.Conversations.FindAsync(conversationId);
            if (conversation == null)
            {
                await transaction.RollbackAsync();
                throw new InvalidOperationException("Conversation not found");
            }

            // Store document references in message
            var documentReferences = documentIds.Count > 0 
                ? System.Text.Json.JsonSerializer.Serialize(documentIds) 
                : null;

            patientMessage = new Message
            {
                ConversationId = conversationId,
                SenderId = patientId,
                SenderType = MessageSenderType.Patient,
                Content = content,
                DocumentReferences = documentReferences,
                CreatedAt = DateTime.UtcNow
            };

            db.Messages.Add(patientMessage);
            conversation.LastMessageAt = DateTime.UtcNow;
            conversation.UpdatedAt = DateTime.UtcNow;
            conversation.TotalMessages++;

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation($"[FACADE] Patient message created - ID: {patientMessage.Id}");
            
            // 🔔 REAL-TIME: Send patient message via SignalR
            await _hubContext.SendMessageToConversation(conversationId, patientMessage);
            _logger.LogInformation($"[FACADE] Patient message sent via SignalR");

            // Extract text from attachments for AI context
            if (documentIds.Count > 0)
            {
                var documents = await db.Documents
                    .Where(d => documentIds.Contains(d.Id))
                    .ToListAsync();
                
                var attachmentTexts = new List<string>();
                var imageBase64List = new List<string>();
                
                foreach (var doc in documents)
                {
                    attachmentTexts.Add($"[Attachment: {doc.FileName}]");
                    
                    // If it's an image, add to image list for vision AI
                    if (doc.FileType == DocumentType.Image && doc.FileData != null)
                    {
                        var base64 = Convert.ToBase64String(doc.FileData);
                        imageBase64List.Add(base64);
                        _logger.LogInformation($"[FACADE] Added image {doc.FileName} for vision analysis");
                    }
                    
                    if (!string.IsNullOrEmpty(doc.ExtractedText))
                    {
                        attachmentTexts.Add(doc.ExtractedText);
                    }
                }
                
                if (attachmentTexts.Count > 0)
                {
                    attachmentContext = string.Join("\n", attachmentTexts);
                }
                
                // Store image list in a way we can access it later
                if (imageBase64List.Count > 0)
                {
                    _logger.LogInformation($"[FACADE] Total {imageBase64List.Count} images ready for vision AI");
                    // Store in attachmentContext for now - we'll parse it later
                    attachmentContext += $"\n[IMAGES:{imageBase64List.Count}]";
                    foreach (var img in imageBase64List)
                    {
                        attachmentContext += $"\n[IMG_BASE64:{img.Substring(0, Math.Min(50, img.Length))}...]";
                    }
                }
            }
        }

        // Yield user message
        yield return new StreamingMessageChunk
        {
            IsUserMessage = true,
            Message = patientMessage
        };

        // 2. Generate AI response if needed
        if (conversation.AssignedDoctorId == null)
        {
            _logger.LogInformation("[FACADE] AI conversation detected, generating streaming response with attachments...");

            var fullResponse = "";
            var success = false;

            // Combine message content with attachment context
            var fullContext = content;
            if (!string.IsNullOrEmpty(attachmentContext))
            {
                fullContext = $"{content}\n\n{attachmentContext}";
            }

            // Stream AI response chunks
            await foreach (var chunk in GenerateAiResponseWithFallbackAsync(fullContext))
            {
                fullResponse += chunk;
                success = true;
                
                yield return new StreamingMessageChunk
                {
                    IsAiChunk = true,
                    Content = chunk
                };
            }

            // Save AI response to database
            if (!success || string.IsNullOrEmpty(fullResponse))
            {
                fullResponse = "I apologize, but I'm experiencing technical difficulties at the moment. Please try again in a few moments, or consider consulting with one of our human doctors for immediate assistance.";
            }

            using (var db2 = DbClient.Instance.GetDb())
            {
                var transaction2 = await db2.Database.BeginTransactionAsync();

                var aiResponse = new Message
                {
                    ConversationId = conversationId,
                    SenderId = null,
                    SenderType = MessageSenderType.AI,
                    Content = fullResponse,
                    AiModelUsed = _aiAssistantService.CurrentModelName,
                    AiConfidenceScore = success ? 0.85m : 0.0m,
                    CreatedAt = DateTime.UtcNow
                };

                db2.Messages.Add(aiResponse);

                var conv = await db2.Conversations.FindAsync(conversationId);
                if (conv != null)
                {
                    conv.LastMessageAt = DateTime.UtcNow;
                    conv.UpdatedAt = DateTime.UtcNow;
                    conv.TotalMessages++;
                    conv.AiMessagesCount++;
                }

                await db2.SaveChangesAsync();
                await transaction2.CommitAsync();

                _logger.LogInformation($"[FACADE] AI response saved - ID: {aiResponse.Id}");
                
                // 🔔 REAL-TIME: Send AI response via SignalR (for doctor to see)
                await _hubContext.SendMessageToConversation(conversationId, aiResponse);
                _logger.LogInformation($"[FACADE] AI response sent via SignalR");

                // Yield complete message
                yield return new StreamingMessageChunk
                {
                    IsComplete = true,
                    Message = aiResponse
                };
            }
        }
    }

    #endregion

    #region Get Consultation Information

    /// <summary>
    /// Gets complete consultation session information (simplified interface)
    /// Internal coordination: Get conversation + Get messages + Get doctor info + Mark as read + Send read receipt
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

        // 🔔 REAL-TIME: Send read receipts for unread messages
        var unreadMessages = messages.Where(m => !m.IsRead && m.SenderType != excludeSenderType);
        foreach (var message in unreadMessages)
        {
            await _hubContext.SendMessageReadReceipt(conversationId, message.Id, userId);
        }

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

    /// <summary>
    /// Gets AI-recommended doctors based on conversation context (simplified interface)
    /// Uses Strategy Pattern to intelligently match doctors based on symptoms and conversation history
    /// </summary>
    public async Task<List<RecommendedDoctorItem>> GetAiRecommendedDoctorsAsync(
        Guid conversationId, 
        int maxResults = 5)
    {
        _logger.LogInformation($"[FACADE] Getting AI-recommended doctors for conversation {conversationId}");

        // 1. Get conversation messages to extract symptoms and context
        var messages = await _messageService.GetByConversationIdAsync(conversationId);
        
        // 2. Extract symptoms from patient messages using AI
        var patientMessages = messages
            .Where(m => m.SenderType == MessageSenderType.Patient)
            .Select(m => m.Content)
            .ToList();
        
        var conversationContext = string.Join(" ", patientMessages);
        
        // 3. Use AI to extract symptoms and conditions
        var extractedInfo = await ExtractSymptomsFromConversationAsync(conversationContext);
        
        // 4. Build search criteria
        var criteria = new DoctorSearchCriteria
        {
            Symptoms = extractedInfo.Symptoms,
            Conditions = extractedInfo.Conditions,
            PreferredSpecialization = extractedInfo.SuggestedSpecialization,
            MaxResults = maxResults,
            MinRating = 4.0m // Only recommend highly-rated doctors
        };

        _logger.LogInformation($"[FACADE] Search criteria - Symptoms: {string.Join(", ", criteria.Symptoms)}, " +
                             $"Specialization: {criteria.PreferredSpecialization}");

        // 5. Get recommendations using Strategy Pattern
        var matchResults = await _doctorRecommendationService.GetRecommendedDoctorsAsync(criteria);
        
        // 6. Convert to UI-friendly format
        var recommendedDoctors = matchResults.Select(result => new RecommendedDoctorItem
        {
            UserId = result.Doctor.UserId,
            FullName = result.Doctor.FullName,
            PrimarySpecialization = result.Doctor.PrimarySpecialization,
            YearsOfExperience = result.Doctor.YearsOfExperience,
            AverageRating = result.Doctor.AverageRating,
            TotalRatings = result.Doctor.TotalRatings,
            ProfilePhotoUrl = result.Doctor.ProfilePhotoUrl,
            AvailabilityStatus = result.Doctor.AvailabilityStatus,
            MatchScore = result.MatchScore,
            MatchReasons = result.MatchReasons,
            IsRecommended = true
        }).ToList();

        _logger.LogInformation($"[FACADE] Found {recommendedDoctors.Count} recommended doctors");

        return recommendedDoctors;
    }

    /// <summary>
    /// Extracts symptoms and conditions from conversation using AI
    /// </summary>
    private async Task<ExtractedMedicalInfo> ExtractSymptomsFromConversationAsync(string conversationContext)
    {
        try
        {
            // Use AI to analyze conversation and extract medical information
            var prompt = $@"Analyze the following patient conversation and extract:
1. List of symptoms mentioned
2. Any medical conditions mentioned
3. Suggested medical specialization

Conversation:
{conversationContext}

Respond in JSON format:
{{
    ""symptoms"": [""symptom1"", ""symptom2""],
    ""conditions"": [""condition1""],
    ""suggested_specialization"": ""specialization_name""
}}";

            var aiResponse = await _aiAssistantService.GenerateMedicalResponseAsync(
                patientQuery: prompt,
                medicalContext: null,
                temperature: 0.3 // Lower temperature for more focused extraction
            );

            // Parse AI response (simplified - in production, use proper JSON parsing)
            var info = ParseMedicalInfoFromAiResponse(aiResponse);
            return info;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FACADE] Failed to extract symptoms from conversation, using fallback");
            
            // Fallback: simple keyword extraction
            return ExtractSymptomsUsingKeywords(conversationContext);
        }
    }

    /// <summary>
    /// Generates a comprehensive summary of patient's condition from AI conversation
    /// This summary will be sent to the doctor when patient selects them
    /// </summary>
    private async Task<string> GeneratePatientConditionSummaryAsync(Guid aiConversationId)
    {
        try
        {
            // 1. Get the AI conversation messages
            var messages = await _messageService.GetByConversationIdAsync(aiConversationId);
            if (!messages.Any())
            {
                return string.Empty;
            }

            // 2. Build conversation context (only patient and AI messages)
            var conversationContext = string.Join("\n\n", messages
                .Where(m => m.SenderType == MessageSenderType.Patient || m.SenderType == MessageSenderType.AI)
                .OrderBy(m => m.CreatedAt)
                .Select(m => $"{(m.SenderType == MessageSenderType.Patient ? "Patient" : "AI")}: {m.Content}"));

            // 3. Use AI to generate a professional medical summary
            var prompt = $@"Based on the following conversation between a patient and an AI assistant, generate a concise professional medical summary for the doctor. The summary should include:

1. Chief Complaint: Main reason for consultation
2. Symptoms: List of symptoms mentioned with duration and severity
3. Medical History: Any relevant medical history mentioned
4. AI Assessment: Key findings from the AI conversation
5. Reason for Referral: Why the patient is being referred to this doctor

Keep the summary professional, concise, and focused on medically relevant information.

Conversation:
{conversationContext}

Generate the summary in a clear, structured format suitable for a doctor to quickly understand the patient's condition.";

            var summary = await _aiAssistantService.GenerateMedicalResponseAsync(
                patientQuery: prompt,
                medicalContext: null,
                temperature: 0.5 // Balanced temperature for professional summary
            );

            // 4. Format the summary with a header
            var formattedSummary = $@"📋 **Patient Condition Summary** (AI-Generated)

{summary}

---
*This summary was automatically generated from the patient's conversation with the AI assistant. Please review and verify all information with the patient.*";

            return formattedSummary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[FACADE] Failed to generate patient condition summary for conversation {ConversationId}", aiConversationId);
            
            // Fallback: Create a basic summary
            try
            {
                var messages = await _messageService.GetByConversationIdAsync(aiConversationId);
                var patientMessages = messages
                    .Where(m => m.SenderType == MessageSenderType.Patient)
                    .OrderBy(m => m.CreatedAt)
                    .Take(3)
                    .Select(m => m.Content)
                    .ToList();

                if (patientMessages.Any())
                {
                    return $@"📋 **Patient Condition Summary**

**Patient's Main Concerns:**
{string.Join("\n", patientMessages.Select((msg, idx) => $"{idx + 1}. {msg.Substring(0, Math.Min(200, msg.Length))}{(msg.Length > 200 ? "..." : "")}"))}

---
*This is a basic summary. Please discuss details with the patient.*";
                }
            }
            catch
            {
                // Ignore fallback errors
            }

            return string.Empty;
        }
    }

    /// <summary>
    /// Parses medical information from AI response
    /// </summary>
    private ExtractedMedicalInfo ParseMedicalInfoFromAiResponse(string aiResponse)
    {
        try
        {
            // Try to parse JSON response
            var jsonStart = aiResponse.IndexOf('{');
            var jsonEnd = aiResponse.LastIndexOf('}');
            
            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonStr = aiResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var parsed = System.Text.Json.JsonSerializer.Deserialize<ExtractedMedicalInfo>(
                    jsonStr, 
                    new System.Text.Json.JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true 
                    }
                );
                
                if (parsed != null)
                {
                    return parsed;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[FACADE] Failed to parse AI response as JSON");
        }

        // Fallback to empty info
        return new ExtractedMedicalInfo();
    }

    /// <summary>
    /// Fallback method: Extract symptoms using simple keyword matching
    /// </summary>
    private ExtractedMedicalInfo ExtractSymptomsUsingKeywords(string text)
    {
        var info = new ExtractedMedicalInfo();
        var lowerText = text.ToLower();

        // Common symptoms
        var symptomKeywords = new Dictionary<string, string>
        {
            { "headache", "Headache" },
            { "fever", "Fever" },
            { "cough", "Cough" },
            { "pain", "Pain" },
            { "dizzy", "Dizziness" },
            { "nausea", "Nausea" },
            { "fatigue", "Fatigue" },
            { "chest pain", "Chest Pain" },
            { "shortness of breath", "Shortness of Breath" },
            { "sore throat", "Sore Throat" }
        };

        foreach (var keyword in symptomKeywords)
        {
            if (lowerText.Contains(keyword.Key))
            {
                info.Symptoms.Add(keyword.Value);
            }
        }

        // Suggest specialization based on symptoms
        if (info.Symptoms.Any(s => s.Contains("Chest") || s.Contains("Heart")))
        {
            info.SuggestedSpecialization = "Cardiology";
        }
        else if (info.Symptoms.Any(s => s.Contains("Headache") || s.Contains("Dizzy")))
        {
            info.SuggestedSpecialization = "Neurology";
        }
        else if (info.Symptoms.Any(s => s.Contains("Cough") || s.Contains("Breath")))
        {
            info.SuggestedSpecialization = "Pulmonology";
        }
        else
        {
            info.SuggestedSpecialization = "General Practice";
        }

        return info;
    }

    #endregion

    #region Manage Consultation

    /// <summary>
    /// Closes consultation session (simplified interface)
    /// Internal coordination: Update status + Log activity + Notify via SignalR
    /// </summary>
    public async Task CloseConsultationAsync(Guid conversationId, Guid userId)
    {
        // 1. 更新对话状态
        await _conversationService.UpdateStatusAsync(conversationId, ConversationStatus.Closed);

        // 🔔 REAL-TIME: Notify status change
        await _hubContext.SendConversationStatusUpdate(conversationId, ConversationStatus.Closed);

        // 2. Log activity
        await _activityLogService.LogActivityAsync(
            userId,
            "close_consultation",
            $"{{\"conversation_id\": \"{conversationId}\", \"status\": \"closed\"}}"
        );
    }

    /// <summary>
    /// Archives consultation session (simplified interface)
    /// Internal coordination: Update status + Log activity + Notify via SignalR
    /// </summary>
    public async Task ArchiveConsultationAsync(Guid conversationId, Guid userId)
    {
        // 1. 更新对话状态
        await _conversationService.UpdateStatusAsync(conversationId, ConversationStatus.Archived);

        // 🔔 REAL-TIME: Notify status change
        await _hubContext.SendConversationStatusUpdate(conversationId, ConversationStatus.Archived);

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
    /// Includes automatic fallback to alternative models on failure
    /// </summary>
    private async Task<AiResponseContent> GenerateAiResponseContentAsync(string userMessage)
    {
        _logger.LogInformation("=== [AI GENERATION] GenerateAiResponseContentAsync Started ===");
        _logger.LogInformation($"[AI GEN] User Message Length: {userMessage.Length} chars");
        _logger.LogInformation($"[AI GEN] Current AI Model: {_aiAssistantService.CurrentModelName}");
        
        var attemptedModels = new List<string>();
        var currentModel = _aiAssistantService.CurrentModelName;
        attemptedModels.Add(currentModel);
        
        try
        {
            // 尝试使用当前模型
            string aiResponse = await _aiAssistantService.GenerateMedicalResponseAsync(
                patientQuery: userMessage,
                medicalContext: null,
                temperature: 0.7
            );
            
            _logger.LogInformation($"[AI GEN] Response received - Length: {aiResponse.Length} chars");
            _logger.LogInformation("=== [AI GENERATION] Completed Successfully ===");

            return new AiResponseContent
            {
                Content = aiResponse,
                ModelUsed = _aiAssistantService.CurrentModelName,
                ConfidenceScore = 0.85m
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"[AI GEN] Primary model '{currentModel}' failed, attempting fallback");
            
            // 获取所有可用模型
            var availableModels = _aiAssistantService.GetAvailableModels();
            var fallbackModels = availableModels
                .Where(m => !attemptedModels.Contains(m.DisplayName))
                .ToList();
            
            // 尝试fallback模型
            foreach (var fallbackModel in fallbackModels)
            {
                try
                {
                    _logger.LogInformation($"[AI GEN] Trying fallback model: {fallbackModel.DisplayName}");
                    
                    // 切换到fallback模型
                    _aiAssistantService.SwitchModel(fallbackModel.Key);
                    attemptedModels.Add(fallbackModel.DisplayName);
                    
                    // 重试请求
                    string aiResponse = await _aiAssistantService.GenerateMedicalResponseAsync(
                        patientQuery: userMessage,
                        medicalContext: null,
                        temperature: 0.7
                    );
                    
                    _logger.LogInformation($"[AI GEN] Fallback successful with {fallbackModel.DisplayName}");
                    
                    return new AiResponseContent
                    {
                        Content = aiResponse,
                        ModelUsed = fallbackModel.DisplayName,
                        ConfidenceScore = 0.75m // Slightly lower confidence for fallback
                    };
                }
                catch (Exception fallbackEx)
                {
                    _logger.LogWarning(fallbackEx, $"[AI GEN] Fallback model '{fallbackModel.DisplayName}' also failed");
                    continue;
                }
            }
            
            // 所有模型都失败了，返回友好的错误消息
            _logger.LogError("[AI GEN] All AI models failed");
            
            return new AiResponseContent
            {
                Content = "I apologize, but I'm experiencing technical difficulties at the moment. Please try again in a few moments, or consider consulting with one of our human doctors for immediate assistance.",
                ModelUsed = "Error",
                ConfidenceScore = 0.0m
            };
        }
    }

    #endregion

    #region AI Model Management

    /// <summary>
    /// Get available AI models for consultation
    /// Facade method to expose AI model information to UI
    /// </summary>
    public List<AiModelInfo> GetAvailableAiModels()
    {
        var models = _aiAssistantService.GetAvailableModels();
        return models.Select(m => new AiModelInfo
        {
            Key = m.Key,
            ModelId = m.ModelId,
            DisplayName = m.DisplayName,
            Description = GetModelDescription(m.Key)
        }).ToList();
    }

    /// <summary>
    /// Switch AI model for current session
    /// Facade method to control AI model selection
    /// </summary>
    public void SwitchAiModel(string modelKey)
    {
        _aiAssistantService.SwitchModel(modelKey);
        _logger.LogInformation($"[CONSULTATION FACADE] Switched AI model to: {modelKey}");
    }

    /// <summary>
    /// Get current AI model name
    /// Facade method to query current model
    /// </summary>
    public string GetCurrentAiModelName()
    {
        return _aiAssistantService.CurrentModelName;
    }

    /// <summary>
    /// Get model description for UI display
    /// </summary>
    private static string GetModelDescription(string modelKey)
    {
        return modelKey switch
        {
            "owl-alpha" => "High-performance reasoning model, best for complex medical analysis",
            "gemma-4" => "Google's powerful open-source model with strong general capabilities",
            "minimax" => "Excellent multilingual support and natural conversation",
            "nemotron" => "NVIDIA's advanced model for technical tasks",
            "qianfan-ocr" => "Specialized in document analysis and OCR",
            _ => "AI model for medical consultation"
        };
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

/// <summary>
/// Streaming message chunk for real-time updates
/// </summary>
public class StreamingMessageChunk
{
    public bool IsUserMessage { get; set; }
    public bool IsAiChunk { get; set; }
    public bool IsComplete { get; set; }
    public string Content { get; set; } = string.Empty;
    public Message? Message { get; set; }
}

/// <summary>
/// Recommended doctor item with match information
/// </summary>
public class RecommendedDoctorItem
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PrimarySpecialization { get; set; } = string.Empty;
    public int? YearsOfExperience { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public DoctorAvailabilityStatus AvailabilityStatus { get; set; }
    public decimal MatchScore { get; set; }
    public List<string> MatchReasons { get; set; } = new();
    public bool IsRecommended { get; set; }
}

/// <summary>
/// Extracted medical information from conversation
/// </summary>
internal class ExtractedMedicalInfo
{
    public List<string> Symptoms { get; set; } = new();
    public List<string> Conditions { get; set; } = new();
    public string? SuggestedSpecialization { get; set; }
}

#endregion
