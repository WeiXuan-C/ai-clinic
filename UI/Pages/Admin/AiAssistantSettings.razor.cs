using Microsoft.AspNetCore.Components;
using ai_clinic.Models;
using ai_clinic.Services;

namespace ai_clinic.UI.Pages.Admin;

/// <summary>
/// Code-behind for AI Assistant Settings management page
/// Follows Separation of Concerns principle
/// </summary>
public partial class AiAssistantSettings
{
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

    protected override async Task OnInitializedAsync()
    {
        await LoadData();
    }

    private async Task LoadData()
    {
        try
        {
            _isLoading = true;
            
            var currentUser = AuthFacade.CurrentUser;
            if (currentUser == null)
            {
                Navigation.NavigateTo("/auth/signin");
                return;
            }

            // Load settings and statistics in parallel using Facade
            var settingsTask = AdminFacade.GetAllAiSettingsAsync(currentUser.Id);
            var statsTask = AdminFacade.GetAiSettingsStatsAsync();

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
            IsActive = false,
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
            IsActive = setting.IsActive,
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
            var currentUser = AuthFacade.CurrentUser;
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
                _selectedSetting.IsActive = _formModel.IsActive;
                _selectedSetting.SystemPrompt = _formModel.SystemPrompt;
                _selectedSetting.EnableDocumentAnalysis = _formModel.EnableDocumentAnalysis;
                _selectedSetting.EnableSymptomChecker = _formModel.EnableSymptomChecker;
                _selectedSetting.EnableDoctorRecommendation = _formModel.EnableDoctorRecommendation;

                await AdminFacade.UpdateAiSettingAsync(_selectedSetting, currentUser.Id);
                _successMessage = "AI configuration updated successfully";
            }
            else
            {
                // Create new setting
                var newSetting = new AiAssistantSetting
                {
                    ModelName = _formModel.ModelName,
                    IsActive = _formModel.IsActive,
                    SystemPrompt = _formModel.SystemPrompt,
                    EnableDocumentAnalysis = _formModel.EnableDocumentAnalysis,
                    EnableSymptomChecker = _formModel.EnableSymptomChecker,
                    EnableDoctorRecommendation = _formModel.EnableDoctorRecommendation
                };

                await AdminFacade.CreateAiSettingAsync(newSetting, currentUser.Id);
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
            var currentUser = AuthFacade.CurrentUser;
            if (currentUser == null || _selectedSetting == null)
                return;

            await AdminFacade.DeleteAiSettingAsync(_selectedSetting.Id, currentUser.Id);
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
            var currentUser = AuthFacade.CurrentUser;
            if (currentUser == null)
                return;

            await AdminFacade.ActivateAiSettingAsync(settingId, currentUser.Id);
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
        Navigation.NavigateTo("/admin/dashboard");
    }
}

/// <summary>
/// Form model for creating/editing AI settings
/// Separates UI concerns from domain model
/// </summary>
public class AiAssistantSettingFormModel
{
    public string ModelName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string? SystemPrompt { get; set; }
    public bool EnableDocumentAnalysis { get; set; }
    public bool EnableSymptomChecker { get; set; }
    public bool EnableDoctorRecommendation { get; set; }
}
