# SignalR Real-Time Messaging Implementation

## Overview

This implementation provides real-time messaging capabilities for the AI Clinic consultation system using SignalR. It enables instant message delivery, typing indicators, online status tracking, and conversation status updates.

## Architecture

### Design Patterns Used

1. **Facade Pattern** (ConsultationFacade)
   - Coordinates multiple subsystems: ConversationService, MessageService, SignalR Hub
   - Provides simplified interface for complex consultation operations
   - Handles both database operations and real-time notifications

2. **Observer Pattern** (SignalR Events)
   - Clients subscribe to events (ReceiveMessage, UserTyping, etc.)
   - Hub notifies all subscribers when events occur
   - Decouples message senders from receivers

3. **Singleton Pattern** (DbClient)
   - Single database connection instance
   - Thread-safe access to database operations

## Components

### 1. ConsultationHub (Server-Side)

**Location:** `Services/Hubs/ConsultationHub.cs`

**Responsibilities:**
- Manage SignalR connections
- Handle conversation group membership
- Broadcast messages to conversation participants
- Track online users and typing indicators

**Key Methods:**
```csharp
// Connection Management
Task RegisterUser(string userId)
Task OnConnectedAsync()
Task OnDisconnectedAsync(Exception? exception)

// Group Management
Task JoinConversation(string conversationId)
Task LeaveConversation(string conversationId)

// Typing Indicators
Task NotifyTyping(string conversationId, string userName, string userRole)
Task NotifyStoppedTyping(string conversationId, string userName)
```

**Extension Methods:**
```csharp
// Send message to all conversation participants
Task SendMessageToConversation(IHubContext<ConsultationHub>, Guid conversationId, Message message)

// Send AI status updates
Task SendAiStatusUpdate(IHubContext<ConsultationHub>, Guid conversationId, string status, string? details)

// Send conversation status changes
Task SendConversationStatusUpdate(IHubContext<ConsultationHub>, Guid conversationId, ConversationStatus status)

// Send message read receipts
Task SendMessageReadReceipt(IHubContext<ConsultationHub>, Guid conversationId, Guid messageId, Guid readByUserId)
```

### 2. SignalRConsultationService (Client-Side)

**Location:** `Services/SignalRConsultationService.cs`

**Responsibilities:**
- Establish and maintain SignalR connection
- Provide event handlers for real-time updates
- Handle automatic reconnection
- Manage conversation group subscriptions

**Events:**
```csharp
event Action<MessageReceivedEventArgs>? OnMessageReceived
event Action<AiStatusEventArgs>? OnAiStatusUpdate
event Action<TypingEventArgs>? OnUserTyping
event Action<TypingEventArgs>? OnUserStoppedTyping
event Action<ConversationStatusEventArgs>? OnConversationStatusChanged
event Action<MessageReadEventArgs>? OnMessageRead
event Action? OnConnected
event Action<Exception?>? OnDisconnected
event Action<Exception>? OnReconnecting
event Action? OnReconnected
```

**Key Methods:**
```csharp
Task InitializeAsync(string hubUrl)
Task RegisterUserAsync(Guid userId)
Task JoinConversationAsync(Guid conversationId)
Task LeaveConversationAsync(Guid conversationId)
Task NotifyTypingAsync(Guid conversationId, string userName, string userRole)
Task NotifyStoppedTypingAsync(Guid conversationId, string userName)
bool IsConnected { get; }
```

### 3. ConsultationFacade (Updated)

**Location:** `Services/Facades/ConsultationFacade.cs`

**New Dependencies:**
- `IHubContext<ConsultationHub>` - For sending real-time notifications
- `ILogger<ConsultationFacade>` - For structured logging

**Real-Time Integration Points:**

1. **SendPatientMessageAsync**
   - Sends patient message via SignalR immediately after database save
   - Notifies AI status ("thinking", "ready", "error")
   - Sends AI response via SignalR when ready

2. **SendDoctorMessageAsync**
   - Sends doctor message via SignalR immediately after database save

3. **GetConsultationSessionAsync**
   - Sends read receipts for unread messages via SignalR

4. **CloseConsultationAsync / ArchiveConsultationAsync**
   - Notifies conversation status changes via SignalR

## Usage Examples

### Server-Side (Blazor Component or Service)

```csharp
@inject ConsultationFacade ConsultationFacade
@inject SignalRConsultationService SignalRService

protected override async Task OnInitializedAsync()
{
    // Initialize SignalR connection
    await SignalRService.InitializeAsync("/consultationHub");
    
    // Register current user
    await SignalRService.RegisterUserAsync(currentUserId);
    
    // Subscribe to events
    SignalRService.OnMessageReceived += HandleMessageReceived;
    SignalRService.OnAiStatusUpdate += HandleAiStatus;
    SignalRService.OnUserTyping += HandleUserTyping;
    
    // Join conversation
    await SignalRService.JoinConversationAsync(conversationId);
}

private void HandleMessageReceived(MessageReceivedEventArgs args)
{
    // Update UI with new message
    messages.Add(new Message
    {
        Id = args.MessageId,
        Content = args.Content,
        SenderType = Enum.Parse<MessageSenderType>(args.SenderType),
        CreatedAt = args.CreatedAt
    });
    
    StateHasChanged(); // Refresh Blazor UI
}

private void HandleAiStatus(AiStatusEventArgs args)
{
    if (args.Status == "thinking")
    {
        // Show "AI is typing..." indicator
        isAiTyping = true;
    }
    else if (args.Status == "ready")
    {
        // Hide typing indicator
        isAiTyping = false;
    }
    
    StateHasChanged();
}

private void HandleUserTyping(TypingEventArgs args)
{
    // Show "{userName} is typing..." indicator
    typingUsers.Add(args.UserName);
    StateHasChanged();
}

// Send message (automatically broadcasts via SignalR)
private async Task SendMessage()
{
    var result = await ConsultationFacade.SendPatientMessageAsync(
        conversationId,
        currentUserId,
        messageContent
    );
    
    // Message is automatically sent to all conversation participants via SignalR
    // No need to manually update UI - OnMessageReceived event will handle it
}

// Notify typing
private async Task OnInputChanged(string value)
{
    messageContent = value;
    
    if (!string.IsNullOrWhiteSpace(value))
    {
        await SignalRService.NotifyTypingAsync(conversationId, currentUserName, "Patient");
    }
    else
    {
        await SignalRService.NotifyStoppedTypingAsync(conversationId, currentUserName);
    }
}

public async ValueTask DisposeAsync()
{
    // Unsubscribe from events
    SignalRService.OnMessageReceived -= HandleMessageReceived;
    SignalRService.OnAiStatusUpdate -= HandleAiStatus;
    SignalRService.OnUserTyping -= HandleUserTyping;
    
    // Leave conversation
    await SignalRService.LeaveConversationAsync(conversationId);
    
    // Dispose SignalR connection
    await SignalRService.DisposeAsync();
}
```

## Real-Time Event Flow

### Patient Sends Message to AI

```
1. Patient types message in UI
   ↓
2. UI calls ConsultationFacade.SendPatientMessageAsync()
   ↓
3. Facade saves message to database
   ↓
4. Facade sends message via SignalR → All conversation participants receive it
   ↓
5. Facade sends AI status "thinking" via SignalR
   ↓
6. Facade calls AI service to generate response
   ↓
7. Facade saves AI response to database
   ↓
8. Facade sends AI response via SignalR → All participants receive it
   ↓
9. Facade sends AI status "ready" via SignalR
```

### Patient Sends Message to Doctor

```
1. Patient types message in UI
   ↓
2. UI calls ConsultationFacade.SendPatientMessageAsync()
   ↓
3. Facade saves message to database
   ↓
4. Facade sends message via SignalR → Doctor receives it in real-time
   ↓
5. Doctor's UI updates automatically via OnMessageReceived event
```

### Doctor Sends Message to Patient

```
1. Doctor types message in UI
   ↓
2. UI calls ConsultationFacade.SendDoctorMessageAsync()
   ↓
3. Facade saves message to database
   ↓
4. Facade sends message via SignalR → Patient receives it in real-time
   ↓
5. Patient's UI updates automatically via OnMessageReceived event
```

## Configuration

### Program.cs

```csharp
// Add SignalR services
builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 102400000; // 100 MB
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

// Map SignalR hub endpoint
app.MapHub<ConsultationHub>("/consultationHub");
```

### DependencyInjection.cs

```csharp
// Register SignalR client service
services.AddScoped<SignalRConsultationService>();
```

## Security Considerations

1. **Authentication**: Consider adding authentication to the hub using `[Authorize]` attribute
2. **Authorization**: Verify users can only join conversations they're part of
3. **Input Validation**: Validate all hub method parameters
4. **Rate Limiting**: Implement rate limiting for typing notifications
5. **Connection Limits**: Monitor and limit connections per user

## Performance Optimization

1. **Group Management**: Users only receive messages for conversations they've joined
2. **Automatic Reconnection**: Built-in reconnection with exponential backoff
3. **Message Batching**: Consider batching typing indicators to reduce traffic
4. **Connection Pooling**: SignalR handles connection pooling automatically

## Monitoring and Logging

All SignalR operations are logged using `ILogger`:
- Connection events (connect, disconnect, reconnect)
- Group join/leave operations
- Message delivery
- Error conditions

Example log output:
```
[INFO] SignalR connection established
[INFO] User {userId} registered with SignalR hub
[INFO] Joined conversation {conversationId}
[INFO] Received message: {messageId}
[WARN] SignalR reconnecting...
[INFO] SignalR reconnected with connection ID: {connectionId}
```

## Testing

### Manual Testing Checklist

- [ ] Patient sends message to AI - AI responds in real-time
- [ ] Patient sends message to doctor - Doctor receives in real-time
- [ ] Doctor sends message to patient - Patient receives in real-time
- [ ] Typing indicators work for both patient and doctor
- [ ] AI status updates show "thinking" and "ready" states
- [ ] Multiple users in same conversation all receive messages
- [ ] Connection survives network interruptions (automatic reconnection)
- [ ] Read receipts are sent when messages are viewed
- [ ] Conversation status changes are broadcast to all participants

## Troubleshooting

### Connection Issues

**Problem:** SignalR connection fails to establish
**Solution:** 
- Check that hub is mapped in Program.cs: `app.MapHub<ConsultationHub>("/consultationHub")`
- Verify URL is correct: `/consultationHub`
- Check browser console for errors

**Problem:** Messages not received in real-time
**Solution:**
- Verify user has joined conversation: `await SignalRService.JoinConversationAsync(conversationId)`
- Check that event handlers are subscribed: `SignalRService.OnMessageReceived += HandleMessageReceived`
- Ensure `StateHasChanged()` is called in Blazor components

**Problem:** Connection drops frequently
**Solution:**
- Increase timeout values in SignalR configuration
- Check network stability
- Review server logs for errors

## Future Enhancements

1. **File Sharing**: Add support for sending files/images via SignalR
2. **Voice/Video**: Integrate WebRTC for voice/video calls
3. **Presence**: Show online/offline status for doctors
4. **Notifications**: Push notifications for offline users
5. **Message Reactions**: Add emoji reactions to messages
6. **Message Editing**: Allow editing sent messages
7. **Message Search**: Real-time search across conversations
8. **Analytics**: Track message delivery times and user engagement

## References

- [SignalR Documentation](https://docs.microsoft.com/en-us/aspnet/core/signalr/)
- [Blazor with SignalR](https://docs.microsoft.com/en-us/aspnet/core/blazor/tutorials/signalr-blazor)
- [SignalR Hub API](https://docs.microsoft.com/en-us/aspnet/core/signalr/hubs)
