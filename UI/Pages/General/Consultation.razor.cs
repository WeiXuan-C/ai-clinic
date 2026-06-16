using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using ai_clinic.Services;
using ai_clinic.Services.Facades;

namespace ai_clinic.UI.Pages.General;

/// <summary>
/// General consultation page for anonymous users
/// Allows up to 3 queries before requiring login
/// </summary>
public partial class Consultation : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private AiFacade AiFacade { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private readonly List<ChatMessage> messages = [];
    private string newMessage = "";
    private bool isTyping = false;
    private int availableCredits = 3;
    private string sessionId = "";
    private bool showLoginPrompt = false;

    protected override async Task OnInitializedAsync()
    {
        // If user is already logged in, redirect to patient consultation
        if (AuthFacade.IsAuthenticated)
        {
            Navigation.NavigateTo("/patient/consultation");
            return;
        }

        // Generate or retrieve session ID
        sessionId = await GetOrCreateSessionId();

        // Get remaining queries (now async)
        availableCredits = await AiFacade.GetAnonymousRemainingQueriesAsync(sessionId);

        Console.WriteLine($"[GENERAL CONSULTATION] Session ID: {sessionId}, Available Credits: {availableCredits}");
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Initialize Lucide icons
            try
            {
                await JS.InvokeVoidAsync("lucide.createIcons");
            }
            catch
            {
                // Ignore if lucide is not available
            }
        }
    }

    /// <summary>
    /// Gets or creates a session ID for tracking anonymous queries
    /// </summary>
    private async Task<string> GetOrCreateSessionId()
    {
        try
        {
            // Try to get existing session ID from localStorage
            var existingId = await JS.InvokeAsync<string>("localStorage.getItem", "anonymousSessionId");

            if (!string.IsNullOrEmpty(existingId))
            {
                return existingId;
            }
        }
        catch
        {
            // If localStorage is not available, generate new ID
        }

        // Generate new session ID
        var newId = Guid.NewGuid().ToString();

        try
        {
            await JS.InvokeVoidAsync("localStorage.setItem", "anonymousSessionId", newId);
        }
        catch
        {
            // Ignore if localStorage is not available
        }

        return newId;
    }

    /// <summary>
    /// Sends message to AI assistant
    /// </summary>
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(newMessage)) return;

        // Check if user needs to login
        if (availableCredits <= 0)
        {
            showLoginPrompt = true;
            StateHasChanged();
            return;
        }

        var userMessage = new ChatMessage
        {
            Type = MessageType.User,
            Content = newMessage
        };

        messages.Add(userMessage);
        var messageContent = newMessage;
        newMessage = "";
        StateHasChanged();
        await ScrollToBottom();

        // Show typing indicator
        isTyping = true;
        StateHasChanged();

        try
        {
            // Send query to backend
            var result = await AiFacade.SendAnonymousQueryAsync(sessionId, messageContent);

            // Update available credits
            availableCredits = result.RemainingQueries;

            if (result.Success && !string.IsNullOrEmpty(result.Response))
            {
                // Add AI response
                var aiResponse = new ChatMessage
                {
                    Type = MessageType.AI,
                    Content = result.Response
                };

                messages.Add(aiResponse);

                // Show login prompt if this was the last query
                if (result.RequiresLogin)
                {
                    showLoginPrompt = true;
                }
            }
            else if (result.RequiresLogin)
            {
                // Show login prompt
                showLoginPrompt = true;

                // Add system message
                var systemMessage = new ChatMessage
                {
                    Type = MessageType.AI,
                    Content = result.ErrorMessage ?? "You have reached the maximum number of queries. Please sign in to continue.",
                    ActionLabel = "Sign In to Continue"
                };
                messages.Add(systemMessage);
            }
            else
            {
                // Show error message
                var errorMessage = new ChatMessage
                {
                    Type = MessageType.AI,
                    Content = result.ErrorMessage ?? "An error occurred. Please try again."
                };
                messages.Add(errorMessage);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");

            var errorMessage = new ChatMessage
            {
                Type = MessageType.AI,
                Content = "I apologize, but I'm having trouble processing your request. Please try again."
            };
            messages.Add(errorMessage);
        }
        finally
        {
            isTyping = false;
            StateHasChanged();
            await ScrollToBottom();
        }
    }

    private async Task HandleKeyPress(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !string.IsNullOrWhiteSpace(newMessage))
        {
            await SendMessage();
        }
    }

    private void HandleAIInsight(string insight)
    {
        if (insight == "Sign In to Continue")
        {
            Navigation.NavigateTo("/auth/signin");
        }
    }

    private static void AttachDocument()
    {
        // Not available for anonymous users
        Console.WriteLine("Document attachment requires login");
    }

    private static void VoiceInput()
    {
        // Not available for anonymous users
        Console.WriteLine("Voice input requires login");
    }

    private async Task ScrollToBottom()
    {
        try
        {
            await JS.InvokeVoidAsync("scrollToBottom", "messages-container");
        }
        catch
        {
            // Ignore JS interop errors
        }
    }

    private void NavigateToSignIn()
    {
        Navigation.NavigateTo("/auth/signin");
    }

    private void NavigateToSignUp()
    {
        Navigation.NavigateTo("/auth/signup");
    }

    private void CloseLoginPrompt()
    {
        showLoginPrompt = false;
        StateHasChanged();
    }

    public enum MessageType
    {
        AI,
        User,
        Doctor,
        Suggestion
    }

    public class ChatMessage
    {
        public MessageType Type { get; set; }
        public string Content { get; set; } = "";
        public string? ActionLabel { get; set; }
        public string? DoctorName { get; set; }
        public string? Title { get; set; }
    }
}

