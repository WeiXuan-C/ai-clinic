using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ai_clinic.Database;

/// <summary>
/// Direct HTTP client for Supabase REST API
/// Bypasses Postgrest-csharp to allow private setters with proper encapsulation
/// </summary>
public class SupabaseHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly string _supabaseUrl;
    private readonly string _supabaseKey;
    private readonly JsonSerializerOptions _jsonOptions;

    public SupabaseHttpClient(string supabaseUrl, string supabaseKey)
    {
        _supabaseUrl = supabaseUrl.TrimEnd('/');
        _supabaseKey = supabaseKey;
        _httpClient = new HttpClient();
        
        // Configure JSON serializer to work with private setters
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// GET request to Supabase REST API
    /// </summary>
    public async Task<List<T>> GetAsync<T>(string table, string? filter = null)
    {
        var url = $"{_supabaseUrl}/rest/v1/{table}";
        if (!string.IsNullOrEmpty(filter))
        {
            url += $"?{filter}";
        }

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("apikey", _supabaseKey);
        request.Headers.Add("Authorization", $"Bearer {_supabaseKey}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<T>>(json, _jsonOptions) ?? new List<T>();
    }

    /// <summary>
    /// GET single record from Supabase REST API
    /// </summary>
    public async Task<T?> GetSingleAsync<T>(string table, string filter)
    {
        var url = $"{_supabaseUrl}/rest/v1/{table}?{filter}";

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("apikey", _supabaseKey);
        request.Headers.Add("Authorization", $"Bearer {_supabaseKey}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.pgrst.object+json"));

        var response = await _httpClient.SendAsync(request);
        
        if (response.StatusCode == System.Net.HttpStatusCode.NotAcceptable)
        {
            return default;
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        Console.WriteLine($"🔍 JSON RESPONSE: {json}");
        return JsonSerializer.Deserialize<T>(json, _jsonOptions);
    }

    /// <summary>
    /// POST request to Supabase REST API
    /// </summary>
    public async Task<T?> PostAsync<T>(string table, object data) where T : class
    {
        var url = $"{_supabaseUrl}/rest/v1/{table}";

        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("apikey", _supabaseKey);
        request.Headers.Add("Authorization", $"Bearer {_supabaseKey}");
        request.Headers.Add("Prefer", "return=representation");
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<T>>(responseJson, _jsonOptions);
        return result?.FirstOrDefault();
    }

    /// <summary>
    /// PATCH request to Supabase REST API
    /// </summary>
    public async Task<T?> PatchAsync<T>(string table, string filter, object data) where T : class
    {
        var url = $"{_supabaseUrl}/rest/v1/{table}?{filter}";

        var json = JsonSerializer.Serialize(data, _jsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Patch, url);
        request.Headers.Add("apikey", _supabaseKey);
        request.Headers.Add("Authorization", $"Bearer {_supabaseKey}");
        request.Headers.Add("Prefer", "return=representation");
        request.Content = content;

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<List<T>>(responseJson, _jsonOptions);
        return result?.FirstOrDefault();
    }

    /// <summary>
    /// DELETE request to Supabase REST API
    /// </summary>
    public async Task<bool> DeleteAsync(string table, string filter)
    {
        var url = $"{_supabaseUrl}/rest/v1/{table}?{filter}";

        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add("apikey", _supabaseKey);
        request.Headers.Add("Authorization", $"Bearer {_supabaseKey}");

        var response = await _httpClient.SendAsync(request);
        return response.IsSuccessStatusCode;
    }
}
