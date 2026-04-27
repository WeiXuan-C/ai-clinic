using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("doctor_profiles")]
public class Doctor : BaseModel
{
    private const int MaxActiveConversations = 10;

    // Primary Key and Foreign Keys
    [PrimaryKey("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("organization_id")]
    public Guid? OrganizationId { get; set; }

    // Basic Information
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;

    [Column("title")]
    public string? Title { get; set; }

    [Column("license_number")]
    public string LicenseNumber { get; set; } = string.Empty;

    // Specialization
    [Column("primary_specialization")]
    public string PrimarySpecialization { get; set; } = string.Empty;

    [Column("sub_specializations")]
    public string[]? SubSpecializations { get; set; }

    [Column("medical_expertise_tags")]
    public string[]? MedicalExpertiseTags { get; set; }

    // Availability
    [Column("availability_status")]
    public string AvailabilityStatus { get; set; } = "offline";

    [Column("current_active_conversations")]
    public int CurrentActiveConversations { get; set; }

    // Performance Metrics
    [Column("average_rating")]
    public decimal AverageRating { get; set; }

    [Column("total_consultations")]
    public int TotalConsultations { get; set; }

    [Column("years_of_experience")]
    public int? YearsOfExperience { get; set; }

    [Column("bio")]
    public string? Bio { get; set; }

    // Status
    [Column("is_verified")]
    public bool IsVerified { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    // Timestamps
    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Public Methods (Business Logic)
    public void UpdateProfile(string fullName, string? title, string? bio, int? yearsOfExperience)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty");

        FullName = fullName.Trim();
        Title = title?.Trim();
        Bio = bio?.Trim();
        YearsOfExperience = yearsOfExperience;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateSpecialization(string primarySpecialization, string[]? subSpecializations, string[]? expertiseTags)
    {
        if (string.IsNullOrWhiteSpace(primarySpecialization))
            throw new ArgumentException("Primary specialization cannot be empty");

        PrimarySpecialization = primarySpecialization;
        SubSpecializations = subSpecializations;
        MedicalExpertiseTags = expertiseTags;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanAcceptNewConversation()
    {
        return IsActive && IsVerified && 
               AvailabilityStatus == "available" && 
               CurrentActiveConversations < MaxActiveConversations;
    }

    public void SetAvailabilityStatus(string status)
    {
        if (!IsValidStatus(status))
            throw new ArgumentException($"Invalid status: {status}");

        AvailabilityStatus = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementActiveConversations()
    {
        if (CurrentActiveConversations >= MaxActiveConversations)
            throw new InvalidOperationException("Maximum active conversations reached");

        CurrentActiveConversations++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DecrementActiveConversations()
    {
        if (CurrentActiveConversations > 0)
        {
            CurrentActiveConversations--;
            UpdatedAt = DateTime.UtcNow;
        }
    }

    public void CompleteConsultation()
    {
        TotalConsultations++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRating(decimal newAverageRating)
    {
        if (newAverageRating < 0 || newAverageRating > 5)
            throw new ArgumentException("Rating must be between 0 and 5");

        AverageRating = newAverageRating;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Verify()
    {
        IsVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        AvailabilityStatus = "offline";
        UpdatedAt = DateTime.UtcNow;
    }

    // Private Helper Methods
    private bool IsValidStatus(string status)
    {
        return status == "available" || status == "busy" || status == "offline";
    }

    // Public method for DAO initialization
    public void Initialize(Guid id, Guid userId, string licenseNumber, DateTime createdAt)
    {
        Id = id;
        UserId = userId;
        LicenseNumber = licenseNumber;
        CreatedAt = createdAt;
    }
}

public enum DoctorStatus
{
    Available,
    Busy,
    Offline
}
