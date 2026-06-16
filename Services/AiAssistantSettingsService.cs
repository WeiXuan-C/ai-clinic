using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

/// <summary>
/// Service for managing AI Assistant Settings
/// Follows Repository pattern for data access
/// </summary>
public class AiAssistantSettingsService
{
    /// <summary>
    /// Get all AI assistant settings ordered by display order and creation date
    /// </summary>
    public async Task<List<AiAssistantSetting>> GetAllAsync()
    {
        using var db = DbClient.Instance.GetDb();
        return await db.AiAssistantSettings
            .OrderBy(s => s.DisplayOrder)
            .ThenByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get all AI models available for patients to use
    /// Returns only active models that are marked as available for patients
    /// </summary>
    public async Task<List<AiAssistantSetting>> GetAvailableForPatientsAsync()
    {
        using var db = DbClient.Instance.GetDb();
        return await db.AiAssistantSettings
            .Where(s => s.IsActive && s.IsAvailableForPatients)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.ModelName)
            .ToListAsync();
    }

    /// <summary>
    /// Get AI model by type
    /// </summary>
    public async Task<AiAssistantSetting?> GetByModelTypeAsync(AiModelType modelType)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.AiAssistantSettings
            .FirstOrDefaultAsync(s => s.ModelType == modelType && s.IsActive);
    }

    /// <summary>
    /// Get a specific AI assistant setting by ID
    /// </summary>
    public async Task<AiAssistantSetting?> GetByIdAsync(Guid id)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.AiAssistantSettings
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    /// <summary>
    /// Get the currently active AI assistant setting
    /// Only one setting should be active at a time
    /// </summary>
    public async Task<AiAssistantSetting?> GetActiveSettingAsync()
    {
        using var db = DbClient.Instance.GetDb();
        return await db.AiAssistantSettings
            .FirstOrDefaultAsync(s => s.IsActive);
    }

    /// <summary>
    /// Create a new AI assistant setting
    /// Multiple settings can be active simultaneously
    /// </summary>
    public async Task<AiAssistantSetting> CreateAsync(AiAssistantSetting setting)
    {
        using var db = DbClient.Instance.GetDb();

        setting.Id = Guid.NewGuid();
        setting.CreatedAt = DateTime.UtcNow;
        setting.UpdatedAt = DateTime.UtcNow;

        db.AiAssistantSettings.Add(setting);
        await db.SaveChangesAsync();

        return setting;
    }

    /// <summary>
    /// Update an existing AI assistant setting
    /// Handles availability for patients and display order
    /// </summary>
    public async Task<AiAssistantSetting?> UpdateAsync(AiAssistantSetting setting)
    {
        using var db = DbClient.Instance.GetDb();
        
        var existing = await db.AiAssistantSettings
            .FirstOrDefaultAsync(s => s.Id == setting.Id);

        if (existing == null)
            return null;

        existing.ModelName = setting.ModelName;
        existing.ModelType = setting.ModelType;
        existing.IsActive = setting.IsActive;
        existing.IsAvailableForPatients = setting.IsAvailableForPatients;
        existing.DisplayOrder = setting.DisplayOrder;
        existing.Description = setting.Description;
        existing.SystemPrompt = setting.SystemPrompt;
        existing.EnableDocumentAnalysis = setting.EnableDocumentAnalysis;
        existing.EnableSymptomChecker = setting.EnableSymptomChecker;
        existing.EnableDoctorRecommendation = setting.EnableDoctorRecommendation;
        existing.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return existing;
    }

    /// <summary>
    /// Delete an AI assistant setting
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        using var db = DbClient.Instance.GetDb();
        
        var setting = await db.AiAssistantSettings
            .FirstOrDefaultAsync(s => s.Id == id);

        if (setting == null)
            return false;

        db.AiAssistantSettings.Remove(setting);
        await db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Toggle patient availability for a specific AI model
    /// </summary>
    public async Task<bool> TogglePatientAvailabilityAsync(Guid id, bool isAvailable)
    {
        using var db = DbClient.Instance.GetDb();
        
        var setting = await db.AiAssistantSettings
            .FirstOrDefaultAsync(s => s.Id == id);

        if (setting == null)
            return false;

        setting.IsAvailableForPatients = isAvailable;
        setting.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Update display order for multiple AI models
    /// </summary>
    public async Task<bool> UpdateDisplayOrdersAsync(Dictionary<Guid, int> orderUpdates)
    {
        using var db = DbClient.Instance.GetDb();
        
        foreach (var (id, order) in orderUpdates)
        {
            var setting = await db.AiAssistantSettings
                .FirstOrDefaultAsync(s => s.Id == id);
            
            if (setting != null)
            {
                setting.DisplayOrder = order;
                setting.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Activate a specific AI assistant setting
    /// No longer deactivates others - multiple models can be active
    /// </summary>
    public async Task<bool> ActivateSettingAsync(Guid id)
    {
        using var db = DbClient.Instance.GetDb();
        
        var setting = await db.AiAssistantSettings
            .FirstOrDefaultAsync(s => s.Id == id);

        if (setting == null)
            return false;

        // Just activate - no need to deactivate others anymore
        setting.IsActive = true;
        setting.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Deactivate all AI assistant settings
    /// Used internally to maintain single active setting constraint
    /// </summary>
    private async Task DeactivateAllAsync()
    {
        using var db = DbClient.Instance.GetDb();
        
        var activeSettings = await db.AiAssistantSettings
            .Where(s => s.IsActive)
            .ToListAsync();

        foreach (var setting in activeSettings)
        {
            setting.IsActive = false;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Get statistics about AI assistant settings
    /// </summary>
    public async Task<AiSettingsStats> GetStatsAsync()
    {
        using var db = DbClient.Instance.GetDb();
        
        return new AiSettingsStats
        {
            TotalSettings = await db.AiAssistantSettings.CountAsync(),
            ActiveSettings = await db.AiAssistantSettings.CountAsync(s => s.IsActive),
            AvailableForPatients = await db.AiAssistantSettings.CountAsync(s => s.IsActive && s.IsAvailableForPatients),
            ModelsUsed = await db.AiAssistantSettings
                .Select(s => s.ModelName)
                .Distinct()
                .CountAsync()
        };
    }
}

/// <summary>
/// Statistics about AI assistant settings
/// </summary>
public class AiSettingsStats
{
    public int TotalSettings { get; set; }
    public int ActiveSettings { get; set; }
    public int AvailableForPatients { get; set; }
    public int ModelsUsed { get; set; }
}
