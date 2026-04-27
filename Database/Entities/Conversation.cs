using Postgrest.Attributes;
using Postgrest.Models;
using System.Text.Json.Serialization;

namespace AiClinic.Core.Entities;

[Table("conversations")]
public class Conversation : BaseModel
{
    // Primary Key and Foreign Keys
    [PrimaryKey("id")]
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [Column("patient_id")]
    [JsonPropertyName("patient_id")]
    public Guid PatientId { get; set; }

    [Column("assigned_doctor_id")]
    [JsonPropertyName("assigned_doctor_id")]
    public Guid? AssignedDoctorId { get; set; }

    // Conversation Details
    [Column("title")]
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [Column("status")]
    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";

    [Column("initial_symptoms")]
    [JsonPropertyName("initial_symptoms")]
    public string[]? InitialSymptoms { get; set; }

    [Column("ai_suggested_specialization")]
    [JsonPropertyName("ai_suggested_specialization")]
    public string? AiSuggestedSpecialization { get; set; }

    // Timestamps
    [Column("started_at")]
    [JsonPropertyName("started_at")]
    public DateTime StartedAt { get; set; }

    [Column("closed_at")]
    [JsonPropertyName("closed_at")]
    public DateTime? ClosedAt { get; set; }

    [Column("last_message_at")]
    [JsonPropertyName("last_message_at")]
    public DateTime LastMessageAt { get; set; }

    // Message Statistics
    [Column("total_messages")]
    [JsonPropertyName("total_messages")]
    public int TotalMessages { get; set; }

    [Column("ai_messages_count")]
    [JsonPropertyName("ai_messages_count")]
    public int AiMessagesCount { get; set; }

    [Column("doctor_messages_count")]
    [JsonPropertyName("doctor_messages_count")]
    public int DoctorMessagesCount { get; set; }

    [Column("created_at")]
    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    [JsonPropertyName("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Parameterless constructor for ORM
    public Conversation() { }

    // JSON constructor for deserialization
    [JsonConstructor]
    public Conversation(
        Guid id, Guid patient_id, Guid? assigned_doctor_id, string? title, string status,
        string[]? initial_symptoms, string? ai_suggested_specialization,
        DateTime started_at, DateTime? closed_at, DateTime last_message_at,
        int total_messages, int ai_messages_count, int doctor_messages_count,
        DateTime created_at, DateTime? updated_at)
    {
        Id = id;
        PatientId = patient_id;
        AssignedDoctorId = assigned_doctor_id;
        Title = title;
        Status = status;
        InitialSymptoms = initial_symptoms;
        AiSuggestedSpecialization = ai_suggested_specialization;
        StartedAt = started_at;
        ClosedAt = closed_at;
        LastMessageAt = last_message_at;
        TotalMessages = total_messages;
        AiMessagesCount = ai_messages_count;
        DoctorMessagesCount = doctor_messages_count;
        CreatedAt = created_at;
        UpdatedAt = updated_at;
    }

    // Factory method for creating new conversation
    public static Conversation Create(Guid patientId, string? title = null)
    {
        var now = DateTime.UtcNow;
        return new Conversation(
            id: Guid.NewGuid(),
            patient_id: patientId,
            assigned_doctor_id: null,
            title: title ?? "New Consultation",
            status: "active",
            initial_symptoms: null,
            ai_suggested_specialization: null,
            started_at: now,
            closed_at: null,
            last_message_at: now,
            total_messages: 0,
            ai_messages_count: 0,
            doctor_messages_count: 0,
            created_at: now,
            updated_at: null
        );
    }

    // Factory method for assigning doctor
    public Conversation WithAssignedDoctor(Guid doctorId)
    {
        return new Conversation(
            Id, PatientId, doctorId, Title, Status, InitialSymptoms, AiSuggestedSpecialization,
            StartedAt, ClosedAt, LastMessageAt, TotalMessages, AiMessagesCount, DoctorMessagesCount,
            CreatedAt, DateTime.UtcNow
        );
    }

    // Factory method for adding message
    public Conversation WithAddedMessage(MessageSenderType senderType)
    {
        var newAiCount = senderType == MessageSenderType.AI ? AiMessagesCount + 1 : AiMessagesCount;
        var newDoctorCount = senderType == MessageSenderType.Doctor ? DoctorMessagesCount + 1 : DoctorMessagesCount;

        return new Conversation(
            Id, PatientId, AssignedDoctorId, Title, Status, InitialSymptoms, AiSuggestedSpecialization,
            StartedAt, ClosedAt, DateTime.UtcNow, TotalMessages + 1, newAiCount, newDoctorCount,
            CreatedAt, DateTime.UtcNow
        );
    }

    // Factory method for closing
    public Conversation WithClosed()
    {
        return new Conversation(
            Id, PatientId, AssignedDoctorId, Title, "closed", InitialSymptoms, AiSuggestedSpecialization,
            StartedAt, DateTime.UtcNow, LastMessageAt, TotalMessages, AiMessagesCount, DoctorMessagesCount,
            CreatedAt, DateTime.UtcNow
        );
    }

    // Public Methods (Business Logic)
    public void AssignDoctor(Guid doctorId)
    {
        if (AssignedDoctorId.HasValue)
            throw new InvalidOperationException("Doctor already assigned to this conversation");
    }

    public void AddMessage(MessageSenderType senderType) { }
    public void Close() { }
    public void Archive() { }
    public void Reopen() { }
    public bool IsActive() => Status == "active";
}

public enum ConversationStatus
{
    Active,
    Closed,
    Archived,
    Deactive
}
