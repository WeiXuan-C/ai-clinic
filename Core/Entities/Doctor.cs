namespace AiClinic.Core.Entities;

public class Doctor
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Specialization { get; set; } = string.Empty;
    public string Organization { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public DoctorStatus Status { get; set; }
    public int ActiveConversations { get; set; }
    public decimal Rating { get; set; }
    public int TotalConsultations { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public User? User { get; set; }
}

public enum DoctorStatus
{
    Available,
    Busy,
    Offline
}
