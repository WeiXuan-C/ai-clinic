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
    /// Get all AI assistant settings ordered by creation date
    /// </summary>
    public async Task<List<AiAssistantSetting>> GetAllAsync()
    {
        using var db = DbClient.Instance.GetDb();
        return await db.AiAssistantSettings
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
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
    /// Deactivates all other settings if this one is active
    /// </summary>
    public async Task<AiAssistantSetting> CreateAsync(AiAssistantSetting setting)
    {
        using var db = DbClient.Instance.GetDb();
        
        // If this setting is active, deactivate all others
        if (setting.IsActive)
        {
            await DeactivateAllAsync();
        }

        setting.Id = Guid.NewGuid();
        setting.CreatedAt = DateTime.UtcNow;
        setting.UpdatedAt = DateTime.UtcNow;

        db.AiAssistantSettings.Add(setting);
        await db.SaveChangesAsync();

        return setting;
    }

    /// <summary>
    /// Update an existing AI assistant setting
    /// Ensures only one active setting at a time
    /// </summary>
    public async Task<AiAssistantSetting?> UpdateAsync(AiAssistantSetting setting)
    {
        using var db = DbClient.Instance.GetDb();
        
        var existing = await db.AiAssistantSettings
            .FirstOrDefaultAsync(s => s.Id == setting.Id);

        if (existing == null)
            return null;

        // If activating this setting, deactivate all others first
        if (setting.IsActive && !existing.IsActive)
        {
            await DeactivateAllAsync();
        }

        existing.ModelName = setting.ModelName;
        existing.IsActive = setting.IsActive;
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
    /// Prevents deletion if it's the only active setting
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        using var db = DbClient.Instance.GetDb();
        
        var setting = await db.AiAssistantSettings
            .FirstOrDefaultAsync(s => s.Id == id);

        if (setting == null)
            return false;

        // Prevent deletion if this is the only active setting
        if (setting.IsActive)
        {
            var activeCount = await db.AiAssistantSettings
                .CountAsync(s => s.IsActive);
            
            if (activeCount <= 1)
            {
                throw new InvalidOperationException("Cannot delete the only active AI assistant setting");
            }
        }

        db.AiAssistantSettings.Remove(setting);
        await db.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Activate a specific AI assistant setting
    /// Deactivates all others to maintain single active setting
    /// </summary>
    public async Task<bool> ActivateSettingAsync(Guid id)
    {
        using var db = DbClient.Instance.GetDb();
        
        var setting = await db.AiAssistantSettings
            .FirstOrDefaultAsync(s => s.Id == id);

        if (setting == null)
            return false;

        // Deactivate all settings first
        await DeactivateAllAsync();

        // Activate the target setting
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
    public int ModelsUsed { get; set; }
}
