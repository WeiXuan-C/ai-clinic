using ai_clinic.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace ai_clinic.Services;

/// <summary>
/// Service to manage authentication state across the application
/// Stores current user information with cookie-based persistence
/// </summary>
public class AuthStateService
{
    private readonly IJSRuntime _jsRuntime;
    private User? _currentUser;
    private PatientProfile? _currentPatientProfile;
    private DoctorProfile? _currentDoctorProfile;
    private bool _isInitialized = false;

    public event Action? OnAuthStateChanged;

    public AuthStateService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Initialize auth state from stored session
    /// Call this on app startup or page load
    /// </summary>
    public async Task InitializeAsync(UserService userService, PatientProfileService patientProfileService, DoctorProfileService doctorProfileService)
    {
        // Prevent multiple simultaneous initializations
        if (_isInitialized)
        {
            Console.WriteLine("[AuthStateService] Already initialized, skipping");
            return;
        }

        Console.WriteLine("[AuthStateService] Starting initialization...");

        try
        {
            // Get user ID from cookie
            var userIdString = await GetCookieAsync("userId");
            Console.WriteLine($"[AuthStateService] Cookie userId: {userIdString ?? "null"}");
            
            if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
            {
                Console.WriteLine($"[AuthStateService] Parsed userId: {userId}");
                
                // Load user from database
                var user = await userService.GetByIdAsync(userId);
                Console.WriteLine($"[AuthStateService] User from DB: {user?.Email ?? "null"}");
                
                if (user != null && user.IsActive && !user.IsDeactivated)
                {
                    _currentUser = user;
                    Console.WriteLine($"[AuthStateService] User is active: {user.Email}, Role: {user.Role}");

                    // Load profile based on role
                    if (user.Role == UserRole.Patient)
                    {
                        _currentPatientProfile = await patientProfileService.GetByUserIdAsync(user.Id);
                        Console.WriteLine($"[AuthStateService] Patient profile loaded: {_currentPatientProfile?.FullName ?? "null"}");
                    }
                    else if (user.Role == UserRole.Doctor)
                    {
                        _currentDoctorProfile = await doctorProfileService.GetByUserIdAsync(user.Id);
                        Console.WriteLine($"[AuthStateService] Doctor profile loaded: {_currentDoctorProfile?.FullName ?? "null"}");
                    }

                    _isInitialized = true;
                    NotifyAuthStateChanged();
                    Console.WriteLine("[AuthStateService] Initialization successful");
                }
                else
                {
                    Console.WriteLine("[AuthStateService] User is inactive or deactivated");
                    // Invalid or deactivated user, clear session
                    await ClearSessionAsync();
                    _currentUser = null;
                    _currentPatientProfile = null;
                    _currentDoctorProfile = null;
                    _isInitialized = true;
                }
            }
            else
            {
                Console.WriteLine("[AuthStateService] No valid cookie found");
                // No cookie found, clear state
                _currentUser = null;
                _currentPatientProfile = null;
                _currentDoctorProfile = null;
                _isInitialized = true;
            }
        }
        catch (InvalidOperationException ex)
        {
            // This is expected during prerendering when JavaScript interop is not available
            Console.WriteLine($"[AuthStateService] Skipping initialization during prerender: {ex.Message}");
            // Don't mark as initialized so it will retry after render
            _isInitialized = false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthStateService] Exception during initialization: {ex.Message}");
            // If initialization fails, just continue without auth
            try
            {
                await ClearSessionAsync();
            }
            catch
            {
                // Ignore errors when clearing session
            }
            _currentUser = null;
            _currentPatientProfile = null;
            _currentDoctorProfile = null;
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Check if auth state has been initialized
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// Force re-check authentication from cookie
    /// Useful when you suspect the session might have changed
    /// </summary>
    public async Task<bool> RevalidateSessionAsync(UserService userService, PatientProfileService patientProfileService, DoctorProfileService doctorProfileService)
    {
        _isInitialized = false; // Force re-initialization
        await InitializeAsync(userService, patientProfileService, doctorProfileService);
        return IsAuthenticated;
    }

    /// <summary>
    /// Get the current logged-in user
    /// </summary>
    public User? CurrentUser => _currentUser;

    /// <summary>
    /// Get the current patient profile (if user is a patient)
    /// </summary>
    public PatientProfile? CurrentPatientProfile => _currentPatientProfile;

    /// <summary>
    /// Get the current doctor profile (if user is a doctor)
    /// </summary>
    public DoctorProfile? CurrentDoctorProfile => _currentDoctorProfile;

    /// <summary>
    /// Check if user is authenticated
    /// </summary>
    public bool IsAuthenticated => _currentUser != null;

    /// <summary>
    /// Check if current user is a patient
    /// </summary>
    public bool IsPatient => _currentUser?.Role == UserRole.Patient;

    /// <summary>
    /// Check if current user is a doctor
    /// </summary>
    public bool IsDoctor => _currentUser?.Role == UserRole.Doctor;

    /// <summary>
    /// Check if current user is an admin
    /// </summary>
    public bool IsAdmin => _currentUser?.Role == UserRole.Admin;

    /// <summary>
    /// Set the current authenticated user and load their profile
    /// Persists session to cookie
    /// </summary>
    public async Task SetCurrentUserAsync(User user, UserService userService, PatientProfileService patientProfileService, DoctorProfileService doctorProfileService)
    {
        Console.WriteLine($"[AuthStateService] Setting current user: {user.Email}, Role: {user.Role}");
        _currentUser = user;

        // Load profile based on role
        if (user.Role == UserRole.Patient)
        {
            _currentPatientProfile = await patientProfileService.GetByUserIdAsync(user.Id);
            Console.WriteLine($"[AuthStateService] Patient profile loaded: {_currentPatientProfile?.FullName ?? "null"}");
        }
        else if (user.Role == UserRole.Doctor)
        {
            _currentDoctorProfile = await doctorProfileService.GetByUserIdAsync(user.Id);
            Console.WriteLine($"[AuthStateService] Doctor profile loaded: {_currentDoctorProfile?.FullName ?? "null"}");
        }

        // Persist to cookie (30 days expiration)
        Console.WriteLine($"[AuthStateService] Setting cookie for userId: {user.Id}");
        await SetCookieAsync("userId", user.Id.ToString(), 30);
        
        // Brief wait to ensure cookie is written
        await Task.Delay(50);
        Console.WriteLine("[AuthStateService] Cookie set");

        NotifyAuthStateChanged();
        Console.WriteLine("[AuthStateService] Auth state changed notification sent");
    }

    /// <summary>
    /// Clear the current user (logout)
    /// Removes session cookie
    /// </summary>
    public async Task ClearCurrentUserAsync()
    {
        _currentUser = null;
        _currentPatientProfile = null;
        _currentDoctorProfile = null;
        
        await ClearSessionAsync();
        NotifyAuthStateChanged();
    }

    /// <summary>
    /// Clear the current user synchronously (for compatibility)
    /// </summary>
    public void ClearCurrentUser()
    {
        _currentUser = null;
        _currentPatientProfile = null;
        _currentDoctorProfile = null;
        NotifyAuthStateChanged();
    }

    /// <summary>
    /// Clear session cookie
    /// </summary>
    private async Task ClearSessionAsync()
    {
        try
        {
            await DeleteCookieAsync("userId");
        }
        catch
        {
            // Ignore errors when clearing session
        }
    }

    /// <summary>
    /// Get user initials for avatar display
    /// </summary>
    public string GetUserInitials()
    {
        if (_currentUser == null) return "?";

        if (_currentPatientProfile != null && !string.IsNullOrEmpty(_currentPatientProfile.FullName))
        {
            var names = _currentPatientProfile.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (names.Length >= 2)
                return $"{names[0][0]}{names[1][0]}".ToUpper();
            if (names.Length == 1)
                return names[0][0].ToString().ToUpper();
        }

        if (_currentDoctorProfile != null && !string.IsNullOrEmpty(_currentDoctorProfile.FullName))
        {
            var names = _currentDoctorProfile.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (names.Length >= 2)
                return $"{names[0][0]}{names[1][0]}".ToUpper();
            if (names.Length == 1)
                return names[0][0].ToString().ToUpper();
        }

        // Fallback to email
        return _currentUser.Email[0].ToString().ToUpper();
    }

    /// <summary>
    /// Get display name for current user
    /// </summary>
    public string GetDisplayName()
    {
        if (_currentUser == null) return "Guest";

        if (_currentPatientProfile != null && !string.IsNullOrEmpty(_currentPatientProfile.FullName))
            return _currentPatientProfile.FullName;

        if (_currentDoctorProfile != null && !string.IsNullOrEmpty(_currentDoctorProfile.FullName))
            return _currentDoctorProfile.FullName;

        return _currentUser.Email;
    }

    private void NotifyAuthStateChanged()
    {
        OnAuthStateChanged?.Invoke();
    }

    // Cookie management helpers using JavaScript interop
    private async Task<string?> GetCookieAsync(string name)
    {
        try
        {
            // Single attempt with reasonable timeout
            var timeoutTask = Task.Delay(1500);
            var cookieTask = _jsRuntime.InvokeAsync<string?>("eval", $"document.cookie.split('; ').find(row => row.startsWith('{name}='))?.split('=')[1]").AsTask();
            
            var completedTask = await Task.WhenAny(cookieTask, timeoutTask);
            
            if (completedTask == cookieTask)
            {
                var result = await cookieTask;
                if (!string.IsNullOrEmpty(result))
                {
                    Console.WriteLine($"[AuthStateService] Cookie '{name}' read successfully: {result}");
                    return result;
                }
            }
            else
            {
                Console.WriteLine($"[AuthStateService] Cookie '{name}' read timed out");
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AuthStateService] Exception reading cookie: {ex.Message}");
            return null;
        }
    }

    private async Task SetCookieAsync(string name, string value, int days)
    {
        try
        {
            var expires = DateTime.UtcNow.AddDays(days).ToString("R");
            await _jsRuntime.InvokeVoidAsync("eval", $"document.cookie = '{name}={value}; expires={expires}; path=/; SameSite=Strict'");
        }
        catch
        {
            // Ignore cookie errors
        }
    }

    private async Task DeleteCookieAsync(string name)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("eval", $"document.cookie = '{name}=; expires=Thu, 01 Jan 1970 00:00:00 UTC; path=/;'");
        }
        catch
        {
            // Ignore cookie errors
        }
    }
}
