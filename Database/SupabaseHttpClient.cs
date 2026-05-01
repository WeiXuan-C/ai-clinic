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
        
        // Configure JSON serializer to work with private setters and snake_case
        // Note: JsonNamingPolicy.SnakeCaseLower is available in .NET 8.0+
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
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

        // Convert to DTO to avoid serializing Postgrest attributes
        var dto = ConvertToDto(data);
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
        Console.WriteLine($"📤 POST REQUEST to {url}");
        Console.WriteLine($"📦 PAYLOAD: {json}");
        
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("apikey", _supabaseKey);
        request.Headers.Add("Authorization", $"Bearer {_supabaseKey}");
        request.Headers.Add("Prefer", "return=representation");
        request.Content = content;

        try
        {
            var response = await _httpClient.SendAsync(request);
            var responseJson = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"📥 RESPONSE STATUS: {response.StatusCode}");
            Console.WriteLine($"📥 RESPONSE BODY: {responseJson}");
            
            response.EnsureSuccessStatusCode();

            var result = JsonSerializer.Deserialize<List<T>>(responseJson, _jsonOptions);
            return result?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ POST ERROR: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// PATCH request to Supabase REST API
    /// </summary>
    public async Task<T?> PatchAsync<T>(string table, string filter, object data) where T : class
    {
        var url = $"{_supabaseUrl}/rest/v1/{table}?{filter}";

        // Convert to DTO to avoid serializing Postgrest attributes
        var dto = ConvertToDto(data);
        var json = JsonSerializer.Serialize(dto, _jsonOptions);
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

    /// <summary>
    /// Converts an entity with Postgrest attributes to a plain DTO for serialization
    /// This avoids serialization errors with Postgrest attributes
    /// </summary>
    private Dictionary<string, object?> ConvertToDto(object entity)
    {
        var dto = new Dictionary<string, object?>();
        var type = entity.GetType();
        var properties = type.GetProperties();

        foreach (var prop in properties)
        {
            // Skip properties from BaseModel that shouldn't be serialized
            if (prop.Name == "PrimaryKey" || prop.Name == "TableName")
                continue;

            var value = prop.GetValue(entity);
            
            // Get the Column attribute to determine the database column name
            var columnAttr = prop.GetCustomAttributes(typeof(Postgrest.Attributes.ColumnAttribute), false)
                .FirstOrDefault() as Postgrest.Attributes.ColumnAttribute;
            
            var columnName = columnAttr?.ColumnName ?? ToSnakeCase(prop.Name);
            
            // Only include non-null values or explicitly set values
            if (value != null || ShouldIncludeNull(prop.Name))
            {
                dto[columnName] = value;
            }
        }

        return dto;
    }

    /// <summary>
    /// Converts PascalCase to snake_case
    /// </summary>
    private string ToSnakeCase(string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        var sb = new StringBuilder();
        sb.Append(char.ToLower(str[0]));

        for (int i = 1; i < str.Length; i++)
        {
            if (char.IsUpper(str[i]))
            {
                sb.Append('_');
                sb.Append(char.ToLower(str[i]));
            }
            else
            {
                sb.Append(str[i]);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Determines if a null value should be included in the DTO
    /// </summary>
    private bool ShouldIncludeNull(string propertyName)
    {
        // Don't include null for optional fields
        return false;
    }
}
