using Microsoft.AspNetCore.Components;
using ai_clinic.Models;
using ai_clinic.Services;
using ai_clinic.Services.Facades;

namespace ai_clinic.UI.Pages.Admin;

/// <summary>
/// Code-behind for AI Assistant Settings management page
/// Follows Separation of Concerns principle
/// </summary>
public partial class AiAssistantSettings
{
    [Inject] private AuthFacade _authFacade { get; set; } = default!;
    [Inject] private AdminFacade _adminFacade { get; set; } = default!;
    [Inject] private NavigationManager _navigation { get; set; } = default!;

    private List<AiAssistantSetting>? _settings;
    private AiSettingsStats? _stats;
    private bool _isLoading = true;
    private string? _successMessage;
    private string? _errorMessage;

    // Modal state
    private bool _showModal = false;
    private bool _showDeleteModal = false;
    private bool _showDetailsModal = false;
    private bool _isEditMode = false;
    private AiAssistantSetting? _selectedSetting;
    private AiAssistantSettingFormModel _formModel = new();

    protected override Task OnInitializedAsync()
    {
        return LoadData();
    }

    private async Task LoadData()
    {
        try
        {
            _isLoading = true;

            var currentUser = _authFacade.CurrentUser;
            if (currentUser == null)
            {
                _navigation.NavigateTo("/auth/signin");
                return;
            }

            // Load settings and statistics in parallel using Facade
            var settingsTask = _adminFacade.GetAllAiSettingsAsync(currentUser.Id);
            var statsTask = _adminFacade.GetAiSettingsStatsAsync();

            await Task.WhenAll(settingsTask, statsTask);

            _settings = await settingsTask;
            _stats = await statsTask;
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to load AI settings: {ex.Message}";
            Console.WriteLine($"[AiSettings] Error loading data: {ex}");
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void ShowCreateModal()
    {
        _isEditMode = false;
        _formModel = new AiAssistantSettingFormModel
        {
            ModelType = AiModelType.Gemma4,
            IsActive = false,
            IsAvailableForPatients = true,
            DisplayOrder = 0,
            EnableDocumentAnalysis = true,
            EnableSymptomChecker = true,
            EnableDoctorRecommendation = true
        };
        _showModal = true;
    }

    private void ShowEditModal(AiAssistantSetting setting)
    {
        _isEditMode = true;
        _selectedSetting = setting;
        _formModel = new AiAssistantSettingFormModel
        {
            ModelName = setting.ModelName,
            ModelType = setting.ModelType,
            IsActive = setting.IsActive,
            IsAvailableForPatients = setting.IsAvailableForPatients,
            DisplayOrder = setting.DisplayOrder,
            Description = setting.Description,
            SystemPrompt = setting.SystemPrompt,
            EnableDocumentAnalysis = setting.EnableDocumentAnalysis,
            EnableSymptomChecker = setting.EnableSymptomChecker,
            EnableDoctorRecommendation = setting.EnableDoctorRecommendation
        };
        _showModal = true;
    }

    private void ShowDeleteModal(AiAssistantSetting setting)
    {
        _selectedSetting = setting;
        _showDeleteModal = true;
    }

    private void ShowDetailsModal(AiAssistantSetting setting)
    {
        _selectedSetting = setting;
        _showDetailsModal = true;
    }

    private void CloseModal()
    {
        _showModal = false;
        _formModel = new();
        _selectedSetting = null;
    }

    private void CloseDeleteModal()
    {
        _showDeleteModal = false;
        _selectedSetting = null;
    }

    private void CloseDetailsModal()
    {
        _showDetailsModal = false;
        _selectedSetting = null;
    }

    private async Task SaveSetting()
    {
        try
        {
            var currentUser = _authFacade.CurrentUser;
            if (currentUser == null)
            {
                _errorMessage = "Authentication required";
                return;
            }

            // Validate
            if (string.IsNullOrWhiteSpace(_formModel.ModelName))
            {
                _errorMessage = "Model name is required";
                return;
            }

            if (_isEditMode && _selectedSetting != null)
            {
                // Update existing setting
                _selectedSetting.ModelName = _formModel.ModelName;
                _selectedSetting.ModelType = _formModel.ModelType;
                _selectedSetting.IsActive = _formModel.IsActive;
                _selectedSetting.IsAvailableForPatients = _formModel.IsActive && _formModel.IsAvailableForPatients;
                _selectedSetting.DisplayOrder = _formModel.DisplayOrder;
                _selectedSetting.Description = _formModel.Description;
                _selectedSetting.SystemPrompt = _formModel.SystemPrompt;
                _selectedSetting.EnableDocumentAnalysis = _formModel.EnableDocumentAnalysis;
                _selectedSetting.EnableSymptomChecker = _formModel.EnableSymptomChecker;
                _selectedSetting.EnableDoctorRecommendation = _formModel.EnableDoctorRecommendation;

                await _adminFacade.UpdateAiSettingAsync(_selectedSetting, currentUser.Id);
                _successMessage = "AI configuration updated successfully";
            }
            else
            {
                // Create new setting
                var newSetting = new AiAssistantSetting
                {
                    ModelName = _formModel.ModelName,
                    ModelType = _formModel.ModelType,
                    IsActive = _formModel.IsActive,
                    IsAvailableForPatients = _formModel.IsActive && _formModel.IsAvailableForPatients,
                    DisplayOrder = _formModel.DisplayOrder,
                    Description = _formModel.Description,
                    SystemPrompt = _formModel.SystemPrompt,
                    EnableDocumentAnalysis = _formModel.EnableDocumentAnalysis,
                    EnableSymptomChecker = _formModel.EnableSymptomChecker,
                    EnableDoctorRecommendation = _formModel.EnableDoctorRecommendation
                };

                await _adminFacade.CreateAiSettingAsync(newSetting, currentUser.Id);
                _successMessage = "AI configuration created successfully";
            }

            CloseModal();
            await LoadData();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to save AI configuration: {ex.Message}";
            Console.WriteLine($"[AiSettings] Error saving: {ex}");
        }
    }

    private async Task DeleteSetting()
    {
        try
        {
            var currentUser = _authFacade.CurrentUser;
            if (currentUser == null || _selectedSetting == null)
                return;

            await _adminFacade.DeleteAiSettingAsync(_selectedSetting.Id, currentUser.Id);
            _successMessage = "AI configuration deleted successfully";

            CloseDeleteModal();
            await LoadData();
        }
        catch (InvalidOperationException ex)
        {
            _errorMessage = ex.Message;
            CloseDeleteModal();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to delete AI configuration: {ex.Message}";
            Console.WriteLine($"[AiSettings] Error deleting: {ex}");
            CloseDeleteModal();
        }
    }

    private async Task ActivateSetting(Guid settingId)
    {
        try
        {
            var currentUser = _authFacade.CurrentUser;
            if (currentUser == null)
                return;

            await _adminFacade.ActivateAiSettingAsync(settingId, currentUser.Id);
            _successMessage = "AI configuration activated successfully";

            await LoadData();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Failed to activate AI configuration: {ex.Message}";
            Console.WriteLine($"[AiSettings] Error activating: {ex}");
        }
    }

    private void NavigateBack()
    {
        _navigation.NavigateTo("/admin/dashboard");
    }

    private async Task TogglePatientAvailability(Guid settingId, bool isAvailable)
    {
        try
        {
            var aiSettingsService = new AiAssistantSettingsService();
            await aiSettingsService.TogglePatientAvailabilityAsync(settingId, isAvailable);
            
            var setting = _settings?.FirstOrDefault(s => s.Id == settingId);
            if (setting != null)
            {
                setting.IsAvailableForPatients = isAvailable;
            }
            
            await LoadData();
            _successMessage = $"Patient availability {(isAvailable ? "enabled" : "disabled")}";
        }
        catch (Exception ex)
        {
            _errorMessage = "Failed to update patient availability";
            Console.WriteLine($"[AiSettings] Error toggling patient availability: {ex.Message}");
        }
    }

    private async Task UpdateDisplayOrder(Guid settingId, int order)
    {
        try
        {
            var aiSettingsService = new AiAssistantSettingsService();
            var updates = new Dictionary<Guid, int> { { settingId, order } };
            await aiSettingsService.UpdateDisplayOrdersAsync(updates);
            
            var setting = _settings?.FirstOrDefault(s => s.Id == settingId);
            if (setting != null)
            {
                setting.DisplayOrder = order;
            }
            
            _successMessage = "Display order updated";
        }
        catch (Exception ex)
        {
            _errorMessage = "Failed to update display order";
            Console.WriteLine($"[AiSettings] Error updating display order: {ex.Message}");
        }
    }

    private string GetModelIcon(AiModelType modelType)
    {
        return modelType switch
        {
            AiModelType.Gemma4 => "🔷",
            AiModelType.MiniMax => "⚡",
            AiModelType.Nemotron => "🔬",
            AiModelType.Owlapha => "🦉",
            _ => "🤖"
        };
    }
}

/// <summary>
/// Form model for creating/editing AI settings
/// Separates UI concerns from domain model
/// </summary>
public class AiAssistantSettingFormModel
{
    public string ModelName { get; set; } = string.Empty;
    public AiModelType ModelType { get; set; } = AiModelType.Gemma4;
    public bool IsActive { get; set; }
    public bool IsAvailableForPatients { get; set; } = true;
    public int DisplayOrder { get; set; } = 0;
    public string? Description { get; set; }
    public string? SystemPrompt { get; set; }
    public bool EnableDocumentAnalysis { get; set; }
    public bool EnableSymptomChecker { get; set; }
    public bool EnableDoctorRecommendation { get; set; }
}
