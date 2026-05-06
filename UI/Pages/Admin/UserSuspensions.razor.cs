using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ai_clinic.Services.Facades;
using ai_clinic.Models;

namespace ai_clinic.UI.Pages.Admin;

/// <summary>
/// Admin User Suspensions Management page
/// Uses Facade Pattern - only calls AdminFacade
/// </summary>
public partial class UserSuspensions : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private AdminFacade AdminFacade { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private List<UserSuspension> activeSuspensions = new();
    private UserSuspension? selectedSuspension;
    private DateTime? newEndDate;
    
    private bool isLoading = true;
    private bool showExtendDialog = false;
    private bool isProcessing = false;
    private string? errorMessage;

    protected override async Task OnInitializedAsync()
    {
        if (!AuthFacade.IsAuthenticated || AuthFacade.CurrentUser?.Role != UserRole.Admin)
        {
            Navigation.NavigateTo("/auth/signin");
            return;
        }

        await LoadSuspensions();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
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
    /// Load all active suspensions
    /// </summary>
    private async Task LoadSuspensions()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            activeSuspensions = await AdminFacade.GetAllActiveSuspensionsAsync();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading suspensions: {ex.Message}";
            Console.WriteLine($"[ERROR] {ex}");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Show extend suspension dialog
    /// </summary>
    private void ShowExtendDialog(UserSuspension suspension)
    {
        selectedSuspension = suspension;
        newEndDate = suspension.SuspensionEnd ?? DateTime.Now.AddDays(7);
        showExtendDialog = true;
        StateHasChanged();
    }

    /// <summary>
    /// Close extend dialog
    /// </summary>
    private void CloseExtendDialog()
    {
        showExtendDialog = false;
        selectedSuspension = null;
        newEndDate = null;
        StateHasChanged();
    }

    /// <summary>
    /// Extend suspension
    /// </summary>
    private async Task ExtendSuspension()
    {
        if (selectedSuspension == null || !newEndDate.HasValue)
        {
            return;
        }

        isProcessing = true;
        StateHasChanged();

        try
        {
            var success = await AdminFacade.ExtendSuspensionAsync(
                selectedSuspension.UserId,
                newEndDate.Value,
                AuthFacade.CurrentUser!.Id
            );

            if (success)
            {
                await JS.InvokeVoidAsync("alert", "Suspension extended successfully!");
                CloseExtendDialog();
                await LoadSuspensions();
            }
            else
            {
                await JS.InvokeVoidAsync("alert", "Failed to extend suspension.");
            }
        }
        catch (Exception ex)
        {
            await JS.InvokeVoidAsync("alert", $"Error: {ex.Message}");
            Console.WriteLine($"[ERROR] {ex}");
        }
        finally
        {
            isProcessing = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Lift (remove) suspension
    /// </summary>
    private async Task LiftSuspension(Guid userId)
    {
        if (!await JS.InvokeAsync<bool>("confirm", "Are you sure you want to lift this suspension and reactivate the user?"))
        {
            return;
        }

        try
        {
            var success = await AdminFacade.LiftUserSuspensionAsync(userId, AuthFacade.CurrentUser!.Id);

            if (success)
            {
                await JS.InvokeVoidAsync("alert", "Suspension lifted successfully!");
                await LoadSuspensions();
            }
            else
            {
                await JS.InvokeVoidAsync("alert", "Failed to lift suspension.");
            }
        }
        catch (Exception ex)
        {
            await JS.InvokeVoidAsync("alert", $"Error: {ex.Message}");
            Console.WriteLine($"[ERROR] {ex}");
        }
    }
}
