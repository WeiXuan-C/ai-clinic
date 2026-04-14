namespace Backend.Models;

public class Doctor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Specialty { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public int ReviewCount { get; set; }
    public int YearsExperience { get; set; }
    public string[] Languages { get; set; } = Array.Empty<string>();
    public bool IsAvailable { get; set; }
    public decimal ConsultationFee { get; set; }
    public bool IsVerified { get; set; }
    public string? NextAvailable { get; set; }
}
