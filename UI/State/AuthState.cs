using AiClinic.Interfaces;
using Microsoft.Extensions.Configuration;
using SupabaseClient = Supabase.Client;

namespace AiClinic.UI.State;

/// <summary>
/// Scoped Authentication State for Blazor (Redux-like pattern)
/// Manages authentication data, cache, and Supabase Auth operations
/// </summary>
public class AuthState
{
    private readonly IUserRepository _userRepository;
    private readonly SupabaseClient _supabase;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;
    
    private User? _currentUser;
    private bool _isAuthenticated;
    private string? _accessToken;
    private string? _refreshToken;
    private bool _isLoading;
    private string? _errorMessage;

    public AuthState(IUserRepository userRepository, SupabaseClient supabase, IConfiguration configuration)
    {
        _userRepository = userRepository;
        _supabase = supabase;
        _supabaseUrl = configuration["Supabase:Url"] ?? throw new Exception("Supabase URL not configured");
        _supabaseKey = configuration["Supabase:Key"] ?? throw new Exception("Supabase Key not configured");
        
        Console.WriteLine($"🔧 AuthState initialized with URL: {_supabaseUrl}");
    }

    /// <summary>
    /// Event triggered when authentication state changes
    /// Components should subscribe to this and call StateHasChanged()
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Currently authenticated user
    /// </summary>
    public User? CurrentUser => _currentUser;

    /// <summary>
    /// Access token for Supabase authentication
    /// </summary>
    public string? AccessToken => _accessToken;

    /// <summary>
    /// Refresh token for Supabase authentication
    /// </summary>
    public string? RefreshToken => _refreshToken;

    /// <summary>
    /// Whether a user is currently authenticated
    /// </summary>
    public bool IsAuthenticated => _isAuthenticated;

    /// <summary>
    /// Loading state indicator
    /// </summary>
    public bool IsLoading => _isLoading;

    /// <summary>
    /// Error message if any operation fails
    /// </summary>
    public string? ErrorMessage => _errorMessage;

    /// <summary>
    /// Current user's role
    /// </summary>
    public string? UserRole => _currentUser?.Role;

    /// <summary>
    /// Current user's ID
    /// </summary>
    public Guid? UserId => _currentUser?.Id;

    /// <summary>
    /// Checks if current user has a specific role
    /// </summary>
    public bool HasRole(string role)
    {
        return _currentUser?.Role?.Equals(role, StringComparison.OrdinalIgnoreCase) ?? false;
    }

    /// <summary>
    /// Checks if current user is a patient
    /// </summary>
    public bool IsPatient => HasRole("patient");

    /// <summary>
    /// Checks if current user is a doctor
    /// </summary>
    public bool IsDoctor => HasRole("doctor");

    /// <summary>
    /// Checks if current user is an admin
    /// </summary>
    public bool IsAdmin => HasRole("admin");

    // ==================== Supabase Auth Operations ====================

    /// <summary>
    /// Sends OTP to user's email using Supabase Auth
    /// </summary>
    public async Task<bool> SendOtpAsync(string email)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            email = email.ToLower();
            
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
            
            var otpUrl = $"{_supabaseUrl}/auth/v1/otp";
            
            var requestBody = new
            {
                email = email,
                create_user = true,
                data = new { }
            };
            
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            
            var response = await httpClient.PostAsync(otpUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Failed to send OTP to {email}: {responseContent}");
                _errorMessage = "Failed to send OTP";
                return false;
            }
            
            Console.WriteLine($"✅ OTP sent to {email}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending OTP: {ex.Message}");
            _errorMessage = ex.Message;
            return false;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Verifies OTP using Supabase Auth
    /// </summary>
    public async Task<(bool Success, User? User, string? Error, string? AccessToken, string? RefreshToken)> VerifyOtpAsync(string email, string otp)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            email = email.ToLower();
            
            using var httpClient = new HttpClient();
            
            var requestBody = new
            {
                email = email,
                token = otp,
                type = "email"
            };
            
            var content = new StringContent(
                System.Text.Json.JsonSerializer.Serialize(requestBody),
                System.Text.Encoding.UTF8,
                "application/json"
            );
            
            httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
            
            var verifyUrl = $"{_supabaseUrl}/auth/v1/verify";
            Console.WriteLine($"🔍 Verifying OTP at: {verifyUrl}");
            
            var response = await httpClient.PostAsync(verifyUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"📥 Response Status: {response.StatusCode}");
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ OTP verification failed for {email}");
                
                string errorMessage = "Invalid or expired OTP code. Please request a new code.";
                try
                {
                    var errorDoc = System.Text.Json.JsonDocument.Parse(responseContent);
                    
                    if (errorDoc.RootElement.TryGetProperty("error", out var error))
                    {
                        var errorText = error.GetString()?.ToLower() ?? "";
                        if (errorText.Contains("expired"))
                            errorMessage = "OTP code has expired. Please click 'Resend Code' to get a new one.";
                        else if (errorText.Contains("invalid") || errorText.Contains("not found"))
                            errorMessage = "Invalid OTP code. Please check your email and enter the correct code.";
                    }
                    
                    if (errorDoc.RootElement.TryGetProperty("error_description", out var errorDesc))
                    {
                        var errorDescText = errorDesc.GetString() ?? "";
                        if (!string.IsNullOrEmpty(errorDescText))
                            errorMessage = errorDescText;
                    }
                }
                catch { }
                
                _errorMessage = errorMessage;
                return (false, null, errorMessage, null, null);
            }
            
            var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);
            
            string? accessToken = null;
            string? refreshToken = null;
            
            if (jsonDoc.RootElement.TryGetProperty("access_token", out var accessTokenProp))
                accessToken = accessTokenProp.GetString();
            
            if (jsonDoc.RootElement.TryGetProperty("refresh_token", out var refreshTokenProp))
                refreshToken = refreshTokenProp.GetString();

            Console.WriteLine($"✅ OTP verified successfully for {email}");

            // Check if user exists in local database
            var user = await _userRepository.GetByEmailAsync(email);
            
            if (user == null)
            {
                Console.WriteLine($"📝 New user verified: {email}");
                return (true, null, null, accessToken, refreshToken);
            }

            // Existing user - update login timestamp
            var updatedUser = user.WithUpdatedLogin();
            await _userRepository.UpdateAsync(updatedUser);
            
            // Update state
            _currentUser = updatedUser;
            _isAuthenticated = true;
            _accessToken = accessToken;
            _refreshToken = refreshToken;
            
            Console.WriteLine($"✅ Existing user logged in: {email}");
            
            return (true, updatedUser, null, accessToken, refreshToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error verifying OTP: {ex.Message}");
            
            var errorMessage = ex.Message.ToLower() switch
            {
                var msg when msg.Contains("expired") => "OTP code has expired. Please click 'Resend Code' to get a new one.",
                var msg when msg.Contains("invalid") => "Invalid OTP code. Please check your email and try again.",
                var msg when msg.Contains("too many") => "Too many attempts. Please wait a few minutes and try again.",
                _ => $"Verification failed: {ex.Message}"
            };
            
            _errorMessage = errorMessage;
            return (false, null, errorMessage, null, null);
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Restores session from stored tokens
    /// </summary>
    public async Task<bool> RestoreSessionAsync(string accessToken, string refreshToken)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            await _supabase.Auth.SetSession(accessToken, refreshToken);
            
            var currentUser = _supabase.Auth.CurrentUser;
            if (currentUser != null)
            {
                _accessToken = accessToken;
                _refreshToken = refreshToken;
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error restoring session: {ex.Message}");
            _errorMessage = ex.Message;
            return false;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Refreshes the current session
    /// </summary>
    public async Task<(bool Success, string? AccessToken, string? RefreshToken)> RefreshSessionAsync()
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var session = await _supabase.Auth.RefreshSession();
            
            if (session?.AccessToken != null)
            {
                _accessToken = session.AccessToken;
                _refreshToken = session.RefreshToken;
                return (true, session.AccessToken, session.RefreshToken);
            }
            
            return (false, null, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error refreshing session: {ex.Message}");
            _errorMessage = ex.Message;
            return (false, null, null);
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Signs out the current user from Supabase
    /// </summary>
    public async Task SignOutAsync()
    {
        try
        {
            _isLoading = true;
            NotifyStateChanged();

            await _supabase.Auth.SignOut();
            
            _currentUser = null;
            _isAuthenticated = false;
            _accessToken = null;
            _refreshToken = null;
            
            Console.WriteLine("✅ User signed out from Supabase");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error signing out: {ex.Message}");
            _errorMessage = ex.Message;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    // ==================== User Repository Operations ====================

    /// <summary>
    /// Gets user by ID from repository
    /// </summary>
    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            return await _userRepository.GetByIdAsync(userId);
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Gets user by email from repository
    /// </summary>
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            return await _userRepository.GetByEmailAsync(email);
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Updates user profile in repository
    /// </summary>
    public async Task<User?> UpdateUserAsync(User user)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var updated = await _userRepository.UpdateAsync(user);
            
            if (_currentUser?.Id == user.Id)
                _currentUser = updated;
            
            return updated;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Creates a new user with role and profile
    /// </summary>
    public async Task<User?> CreateUserWithProfileAsync(string email, string fullName, string role)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            email = email.ToLower();
            
            var existingUser = await _userRepository.GetByEmailAsync(email);
            if (existingUser != null)
            {
                Console.WriteLine($"⚠️  User already exists: {email}");
                return existingUser;
            }
            
            var user = User.Create(email, fullName, role);
            var createdUser = await _userRepository.AddAsync(user);
            
            Console.WriteLine($"✅ User created: {email} with role: {role}");
            
            return createdUser;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error creating user: {ex.Message}");
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    // ==================== State Management ====================

    /// <summary>
    /// Sets authentication state with tokens
    /// </summary>
    public void SetAuthentication(User user, string? accessToken, string? refreshToken)
    {
        _currentUser = user;
        _isAuthenticated = true;
        _accessToken = accessToken;
        _refreshToken = refreshToken;
        NotifyStateChanged();
    }

    /// <summary>
    /// Logs out the current user (local state only)
    /// </summary>
    public void Logout()
    {
        _currentUser = null;
        _isAuthenticated = false;
        _accessToken = null;
        _refreshToken = null;
        NotifyStateChanged();
    }

    /// <summary>
    /// Clears error message
    /// </summary>
    public void ClearError()
    {
        _errorMessage = null;
        NotifyStateChanged();
    }

    /// <summary>
    /// Notifies subscribers that state has changed
    /// </summary>
    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}
