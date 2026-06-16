namespace ai_clinic.Models;

public enum UserRole
{
    Patient,
    Doctor,
    Admin
}

public enum ConversationStatus
{
    Active,
    Closed,
    Archived,
    Deactive
}

public enum MessageSenderType
{
    Patient,
    Doctor,
    AI
}

public enum DoctorAvailabilityStatus
{
    Available,
    Busy,
    Offline
}

public enum DocumentType
{
    MedicalRecord,
    LabResult,
    Prescription,
    Image,
    Other
}

public enum AiModelType
{
    Gemma4,
    MiniMax,
    Nemotron,
    Owlapha
}
