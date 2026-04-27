using Postgrest.Attributes;
using Postgrest.Models;
using System.Text.Json.Serialization;

namespace AiClinic.Core.Entities;

[Table("patient_profiles")]
public class PatientProfile : BaseModel
{
    // Primary Key and Foreign Keys
    [PrimaryKey("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    [JsonPropertyName("user_id")]
    public Guid UserId { get; set; }

    // Basic Information
    [Column("full_name")]
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [Column("date_of_birth")]
    [JsonPropertyName("date_of_birth")]
    public DateTime? DateOfBirth { get; set; }

    [Column("gender")]
    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [Column("address")]
    [JsonPropertyName("address")]
    public string? Address { get; set; }

    // Emergency Contact
    [Column("emergency_contact_name")]
    [JsonPropertyName("emergency_contact_name")]
    public string? EmergencyContactName { get; set; }

    [Column("emergency_contact_phone")]
    [JsonPropertyName("emergency_contact_phone")]
    public string? EmergencyContactPhone { get; set; }

    // Medical Information
    [Column("blood_type")]
    [JsonPropertyName("blood_type")]
    public string? BloodType { get; set; }

    [Column("allergies")]
    [JsonPropertyName("allergies")]
    public string[]? Allergies { get; set; }

    [Column("chronic_conditions")]
    [JsonPropertyName("chronic_conditions")]
    public string[]? ChronicConditions { get; set; }

    [Column("current_medications")]
    [JsonPropertyName("current_medications")]
    public string[]? CurrentMedications { get; set; }

    // Timestamps
    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Parameterless constructor for ORM
    public PatientProfile() { }

    // JSON constructor for deserialization
    [JsonConstructor]
    public PatientProfile(
        Guid id, Guid user_id, string? full_name, DateTime? date_of_birth,
        string? gender, string? address, string? emergency_contact_name,
        string? emergency_contact_phone, string? blood_type, string[]? allergies,
        string[]? chronic_conditions, string[]? current_medications,
        DateTime created_at, DateTime? updated_at)
    {
        Id = id;
        UserId = user_id;
        FullName = full_name;
        DateOfBirth = date_of_birth;
        Gender = gender;
        Address = address;
        EmergencyContactName = emergency_contact_name;
        EmergencyContactPhone = emergency_contact_phone;
        BloodType = blood_type;
        Allergies = allergies;
        ChronicConditions = chronic_conditions;
        CurrentMedications = current_medications;
        CreatedAt = created_at;
        UpdatedAt = updated_at;
    }

    // Factory method for creating new profile
    public static PatientProfile Create(Guid userId, string fullName)
    {
        return new PatientProfile(
            id: Guid.NewGuid(),
            user_id: userId,
            full_name: fullName,
            date_of_birth: null,
            gender: null,
            address: null,
            emergency_contact_name: null,
            emergency_contact_phone: null,
            blood_type: null,
            allergies: null,
            chronic_conditions: null,
            current_medications: null,
            created_at: DateTime.UtcNow,
            updated_at: null
        );
    }

    // Factory method for updating profile
    public PatientProfile WithUpdatedInfo(
        string? fullName, DateTime? dateOfBirth, string? gender, string? address,
        string? emergencyContactName, string? emergencyContactPhone,
        string? bloodType, string[]? allergies, string[]? chronicConditions,
        string[]? currentMedications)
    {
        return new PatientProfile(
            Id, UserId, fullName, dateOfBirth, gender, address,
            emergencyContactName, emergencyContactPhone, bloodType,
            allergies, chronicConditions, currentMedications,
            CreatedAt, DateTime.UtcNow
        );
    }

    // Public Methods (Business Logic)
    public void UpdateBasicInfo(string? fullName, DateTime? dateOfBirth, string? gender, string? address) { }
    public void UpdateEmergencyContact(string? contactName, string? contactPhone) { }
    public void UpdateMedicalInfo(string? bloodType, string[]? allergies, string[]? chronicConditions, string[]? currentMedications) { }
    public void AddAllergy(string allergy) { }
    public void RemoveAllergy(string allergy) { }
    public void AddChronicCondition(string condition) { }
    public void AddMedication(string medication) { }
    
    public int CalculateAge()
    {
        if (!DateOfBirth.HasValue)
            return 0;

        var today = DateTime.Today;
        var age = today.Year - DateOfBirth.Value.Year;
        if (DateOfBirth.Value.Date > today.AddYears(-age))
            age--;

        return age;
    }
}
