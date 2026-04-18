using AiClinic.Core.Entities;
using AiClinic.Services;
using AiClinic.UI.State;

namespace AiClinic.Controller;

/// <summary>
/// Facade Pattern Implementation
/// Simplifies complex chat operations by coordinating multiple services and state
/// </summary>
public class ChatController
{
    private readonly ChatService _chatService;
    private readonly DoctorService _doctorService;
    private readonly ChatState _chatState;
    private readonly AuthState _authState;

    public ChatController(
        ChatService chatService,
        DoctorService doctorService,
        ChatState chatState,
        AuthState authState)
    {
        _chatService = chatService;
        _doctorService = doctorService;
        _chatState = chatState;
        _authState = authState;
    }

    /// <summary>
    /// Sends a message with intelligent routing (AI vs Doctor)
    /// Facade method that implements the complex chat routing logic
    /// </summary>
    public async Task<(bool Success, string Message)> SendMessageAsync(string content)
    {
        try
        {
            _chatState.IsLoading = true;
            _chatState.ErrorMessage = null;

            var userId = _authState.UserId;
            if (!userId.HasValue)
            {
                return (false, "User not authenticated");
            }

            // Get or create active conversation
            var conversation = _chatState.CurrentConversation;
            if (conversation == null)
            {
                conversation = await _chatService.CreateConversationAsync(userId.Value);
                _chatState.CurrentConversation = conversation;
            }

            Message responseMessage;

            // ROUTING LOGIC (Business Rule Implementation)
            if (conversation.AssignedDoctorId != null)
            {
                // Route to assigned doctor
                responseMessage = await _chatService.SendToDoctorAsync(
                    conversation.Id,
                    userId.Value,
                    content);
            }
            else
            {
                // Check for available doctors
                var availableDoctor = await _doctorService.FindAvailableDoctorAsync();

                if (availableDoctor != null)
                {
                    // Assign doctor and route to them
                    await _chatService.AssignDoctorAsync(conversation.Id, availableDoctor.Id);
                    await _doctorService.IncrementActiveConversationsAsync(availableDoctor.Id);

                    _chatState.CurrentConversation = await _chatService.GetActiveConversationAsync(userId.Value);

                    responseMessage = await _chatService.SendToDoctorAsync(
                        conversation.Id,
                        userId.Value,
                        content);
                }
                else
                {
                    // No doctors available - route to AI
                    responseMessage = await _chatService.SendToAIAsync(
                        conversation.Id,
                        userId.Value,
                        content);
                }
            }

            // Update state with new message
            _chatState.AddMessage(responseMessage);

            return (true, "Message sent successfully");
        }
        catch (Exception ex)
        {
            _chatState.ErrorMessage = ex.Message;
            return (false, $"Error: {ex.Message}");
        }
        finally
        {
            _chatState.IsLoading = false;
        }
    }

    /// <summary>
    /// Loads conversation history
    /// Facade method that coordinates loading conversation and messages
    /// </summary>
    public async Task<(bool Success, string Message)> LoadConversationAsync(Guid? conversationId = null)
    {
        try
        {
            _chatState.IsLoading = true;

            var userId = _authState.UserId;
            if (!userId.HasValue)
            {
                return (false, "User not authenticated");
            }

            Conversation? conversation;

            if (conversationId.HasValue)
            {
                // Load specific conversation
                conversation = await _chatService.GetActiveConversationAsync(userId.Value);
            }
            else
            {
                // Load active conversation
                conversation = await _chatService.GetActiveConversationAsync(userId.Value);

                if (conversation == null)
                {
                    // Create new conversation
                    conversation = await _chatService.CreateConversationAsync(userId.Value);
                }
            }

            _chatState.CurrentConversation = conversation;

            // Load messages
            if (conversation != null)
            {
                var messages = await _chatService.GetConversationMessagesAsync(conversation.Id);
                _chatState.SetMessages(messages);
            }

            return (true, "Conversation loaded successfully");
        }
        catch (Exception ex)
        {
            _chatState.ErrorMessage = ex.Message;
            return (false, $"Error: {ex.Message}");
        }
        finally
        {
            _chatState.IsLoading = false;
        }
    }

    /// <summary>
    /// Gets all conversations for current user
    /// </summary>
    public async Task<IEnumerable<Conversation>> GetUserConversationsAsync()
    {
        var userId = _authState.UserId;
        if (!userId.HasValue)
        {
            return Enumerable.Empty<Conversation>();
        }

        return await _chatService.GetPatientConversationsAsync(userId.Value);
    }

    /// <summary>
    /// Marks a message as read
    /// </summary>
    public async Task MarkMessageAsReadAsync(Guid messageId)
    {
        await _chatService.MarkMessageAsReadAsync(messageId);
    }

    /// <summary>
    /// Clears current conversation
    /// </summary>
    public void ClearConversation()
    {
        _chatState.ClearConversation();
    }

    /// <summary>
    /// Requests doctor assignment
    /// Facade method that finds and assigns a doctor
    /// </summary>
    public async Task<(bool Success, string Message)> RequestDoctorAsync(string? specialization = null)
    {
        try
        {
            var userId = _authState.UserId;
            if (!userId.HasValue)
            {
                return (false, "User not authenticated");
            }

            var conversation = _chatState.CurrentConversation;
            if (conversation == null)
            {
                return (false, "No active conversation");
            }

            var doctor = await _doctorService.FindAvailableDoctorAsync(specialization);

            if (doctor == null)
            {
                return (false, "No doctors available at the moment. Please try again later.");
            }

            await _chatService.AssignDoctorAsync(conversation.Id, doctor.Id);
            await _doctorService.IncrementActiveConversationsAsync(doctor.Id);

            _chatState.CurrentConversation = await _chatService.GetActiveConversationAsync(userId.Value);

            return (true, $"Connected to Dr. {doctor.FullName}");
        }
        catch (Exception ex)
        {
            return (false, $"Error: {ex.Message}");
        }
    }
}
