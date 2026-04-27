using System.Text.Json.Serialization;

namespace AiClinic.Core.Entities;

public class User
{
    // Primary Key
    [JsonPropertyName("id")]
    public Guid Id { get; private set; }

    // Basic Information
    [JsonPropertyName("email")]
    public string Email { get; private set; } = string.Empty;

    [JsonPropertyName("full_name")]
    public string? FullName { get; private set; }

    [JsonPropertyName("phone")]
    public string? PhoneNumber { get; private set; }

    [JsonPropertyName("role")]
    public string Role { get; private set; } = "patient";

    [JsonPropertyName("is_active")]
    public bool IsActive { get; private set; } = true;

    // Timestamps
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; private set; }

    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; private set; }

    [JsonPropertyName("last_login_at")]
    public DateTime? LastLoginAt { get; private set; }

    // Privacy settings
    [JsonPropertyName("data_sharing_enabled")]
    public bool DataSharingEnabled { get; private set; } = false;

    [JsonPropertyName("ai_analysis_enabled")]
    public bool AiAnalysisEnabled { get; private set; } = true;

    [JsonPropertyName("activity_tracking_enabled")]
    public bool ActivityTrackingEnabled { get; private set; } = true;

    // Account status
    [JsonPropertyName("is_deactivated")]
    public bool IsDeactivated { get; private set; } = false;

    [JsonPropertyName("deactivated_at")]
    public DateTime? DeactivatedAt { get; private set; }

    // Parameterless constructor for ORM (required by Postgrest)
    public User()
    {
    }

    // Constructor for deserialization (used by JSON deserializer)
    [JsonConstructor]
    public User(
        Guid id,
        string email,
        string? fullName,
        string? phoneNumber,
        string role,
        bool isActive,
        DateTime createdAt,
        DateTime? updatedAt,
        DateTime? lastLoginAt,
        bool dataSharingEnabled,
        bool aiAnalysisEnabled,
        bool activityTrackingEnabled,
        bool isDeactivated,
        DateTime? deactivatedAt)
    {
        Id = id;
        Email = email;
        FullName = fullName;
        PhoneNumber = phoneNumber;
        Role = role;
        IsActive = isActive;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
        LastLoginAt = lastLoginAt;
        DataSharingEnabled = dataSharingEnabled;
        AiAnalysisEnabled = aiAnalysisEnabled;
        ActivityTrackingEnabled = activityTrackingEnabled;
        IsDeactivated = isDeactivated;
        DeactivatedAt = deactivatedAt;
    }

    // Factory method for creating new users
    public static User Create(string email, string fullName, string role)
    {
        return new User(
            id: Guid.NewGuid(),
            email: email.ToLower().Trim(),
            fullName: fullName.Trim(),
            phoneNumber: null,
            role: role.ToLower(),
            isActive: true,
            createdAt: DateTime.UtcNow,
            updatedAt: null,
            lastLoginAt: DateTime.UtcNow,
            dataSharingEnabled: false,
            aiAnalysisEnabled: true,
            activityTrackingEnabled: true,
            isDeactivated: false,
            deactivatedAt: null
        );
    }

    // Factory method for updating user (creates new instance with updated values)
    public User WithUpdatedLogin()
    {
        return new User(
            id: Id,
            email: Email,
            fullName: FullName,
            phoneNumber: PhoneNumber,
            role: Role,
            isActive: IsActive,
            createdAt: CreatedAt,
            updatedAt: DateTime.UtcNow,
            lastLoginAt: DateTime.UtcNow,
            dataSharingEnabled: DataSharingEnabled,
            aiAnalysisEnabled: AiAnalysisEnabled,
            activityTrackingEnabled: ActivityTrackingEnabled,
            isDeactivated: IsDeactivated,
            deactivatedAt: DeactivatedAt
        );
    }

    // Factory method for updating profile
    public User WithUpdatedProfile(string? fullName, string? phoneNumber)
    {
        return new User(
            id: Id,
            email: Email,
            fullName: fullName?.Trim(),
            phoneNumber: phoneNumber?.Trim(),
            role: Role,
            isActive: IsActive,
            createdAt: CreatedAt,
            updatedAt: DateTime.UtcNow,
            lastLoginAt: LastLoginAt,
            dataSharingEnabled: DataSharingEnabled,
            aiAnalysisEnabled: AiAnalysisEnabled,
            activityTrackingEnabled: ActivityTrackingEnabled,
            isDeactivated: IsDeactivated,
            deactivatedAt: DeactivatedAt
        );
    }

    // Factory method for deactivation
    public User WithDeactivation()
    {
        return new User(
            id: Id,
            email: Email,
            fullName: FullName,
            phoneNumber: PhoneNumber,
            role: Role,
            isActive: false,
            createdAt: CreatedAt,
            updatedAt: DateTime.UtcNow,
            lastLoginAt: LastLoginAt,
            dataSharingEnabled: DataSharingEnabled,
            aiAnalysisEnabled: AiAnalysisEnabled,
            activityTrackingEnabled: ActivityTrackingEnabled,
            isDeactivated: true,
            deactivatedAt: DateTime.UtcNow
        );
    }

    // Factory method for updating privacy settings
    public User WithUpdatedPrivacySettings(bool dataSharingEnabled, bool aiAnalysisEnabled, bool activityTrackingEnabled)
    {
        return new User(
            id: Id,
            email: Email,
            fullName: FullName,
            phoneNumber: PhoneNumber,
            role: Role,
            isActive: IsActive,
            createdAt: CreatedAt,
            updatedAt: DateTime.UtcNow,
            lastLoginAt: LastLoginAt,
            dataSharingEnabled: dataSharingEnabled,
            aiAnalysisEnabled: aiAnalysisEnabled,
            activityTrackingEnabled: activityTrackingEnabled,
            isDeactivated: IsDeactivated,
            deactivatedAt: DeactivatedAt
        );
    }
}

public enum UserRole
{
    Patient,
    Doctor,
    Admin
}
