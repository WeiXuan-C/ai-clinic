using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ai_clinic.UI.Pages;

public partial class Index : ComponentBase
{
    [Inject] private IJSRuntime JS { get; set; } = null!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                // Initialize Lucide icons
                await JS.InvokeVoidAsync("initializeLucide");
            }
            catch
            {
                // Ignore if lucide is not available
            }
        }
    }
}
