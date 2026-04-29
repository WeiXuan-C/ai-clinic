using AiClinic.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AiClinic.DAOs;

/// <summary>
/// Adapter Pattern Implementation
/// Adapts Supabase Auth REST API to ISupabaseAuthClient interface
/// </summary>
public class SupabaseAuthClient : ISupabaseAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;

    public SupabaseAuthClient(IConfiguration configuration)
    {
        _supabaseUrl = configuration["Supabase:Url"] ?? throw new Exception("Supabase URL not configured");
        _supabaseKey = configuration["Supabase:Key"] ?? throw new Exception("Supabase Key not configured");

        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);

        Console.WriteLine($"🔧 SupabaseAuthClient initialized with URL: {_supabaseUrl}");
    }

    public async Task<bool> SendOtpAsync(string email)
    {
        try
        {
            email = email.ToLower();

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

            var response = await _httpClient.PostAsync(otpUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"❌ Failed to send OTP to {email}: {responseContent}");
                return false;
            }

            Console.WriteLine($"✅ OTP sent to {email}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error sending OTP: {ex.Message}");
            return false;
        }
    }

    public async Task<(bool Success, string? AccessToken, string? RefreshToken, string? Error)> VerifyOtpAsync(string email, string otp)
    {
        try
        {
            email = email.ToLower();

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

            var verifyUrl = $"{_supabaseUrl}/auth/v1/verify";
            Console.WriteLine($"🔍 Verifying OTP at: {verifyUrl}");

            var response = await _httpClient.PostAsync(verifyUrl, content);
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

                return (false, null, null, errorMessage);
            }

            var jsonDoc = System.Text.Json.JsonDocument.Parse(responseContent);

            string? accessToken = null;
            string? refreshToken = null;

            if (jsonDoc.RootElement.TryGetProperty("access_token", out var accessTokenProp))
                accessToken = accessTokenProp.GetString();

            if (jsonDoc.RootElement.TryGetProperty("refresh_token", out var refreshTokenProp))
                refreshToken = refreshTokenProp.GetString();

            Console.WriteLine($"✅ OTP verified successfully for {email}");

            return (true, accessToken, refreshToken, null);
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

            return (false, null, null, errorMessage);
        }
    }

    public async Task<bool> RestoreSessionAsync(string accessToken, string refreshToken)
    {
        try
        {
            Console.WriteLine("🔄 Restoring session...");
            // Implementation depends on your Supabase client library
            // For now, we'll just validate that tokens exist
            await Task.CompletedTask;
            return !string.IsNullOrEmpty(accessToken) && !string.IsNullOrEmpty(refreshToken);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error restoring session: {ex.Message}");
            return false;
        }
    }

    public async Task<(bool Success, string? AccessToken, string? RefreshToken)> RefreshSessionAsync()
    {
        try
        {
            Console.WriteLine("🔄 Refreshing session...");
            // Implementation depends on your Supabase client library
            // This would typically call the Supabase refresh token endpoint
            await Task.CompletedTask;
            return (false, null, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error refreshing session: {ex.Message}");
            return (false, null, null);
        }
    }

    public async Task SignOutAsync()
    {
        try
        {
            Console.WriteLine("👋 Signing out from Supabase Auth...");
            // Implementation depends on your requirements
            // This would typically call the Supabase signout endpoint
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error signing out: {ex.Message}");
        }
    }
}
