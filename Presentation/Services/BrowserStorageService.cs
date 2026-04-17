using Microsoft.JSInterop;

namespace AiClinic.Presentation.Services;

public class BrowserStorageService
{
    private readonly IJSRuntime _jsRuntime;

    public BrowserStorageService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    private bool IsJavaScriptAvailable()
    {
        // Check if we're in a prerendering context
        return _jsRuntime is IJSInProcessRuntime || 
               (_jsRuntime as IJSUnmarshalledRuntime) != null;
    }

    public async Task SetItemAsync(string key, string value)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", key, value);
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering - ignore
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error setting item in storage: {ex.Message}");
        }
    }

    public async Task<string?> GetItemAsync(string key)
    {
        try
        {
            return await _jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", key);
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering - return null
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting item from storage: {ex.Message}");
            return null;
        }
    }

    public async Task RemoveItemAsync(string key)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", key);
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering - ignore
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error removing item from storage: {ex.Message}");
        }
    }

    public async Task ClearAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.clear");
        }
        catch (InvalidOperationException)
        {
            // JavaScript not available during prerendering - ignore
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error clearing storage: {ex.Message}");
        }
    }
}
