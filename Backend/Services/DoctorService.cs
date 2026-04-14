using Backend.Models;

namespace Backend.Services;

public class DoctorService
{
    public List<Doctor> GetAvailableDoctors()
    {
        return new List<Doctor>
        {
            new Doctor { Id = 1, Name = "Dr. Sarah Chen", Specialty = "Cardiology", ImageUrl = "/api/placeholder/80/80", Rating = 4.9m, ReviewCount = 234, YearsExperience = 15, Languages = new[] { "English", "Mandarin" }, IsAvailable = true, ConsultationFee = 150, IsVerified = true, NextAvailable = "Today 2:00 PM" },
            new Doctor { Id = 2, Name = "Dr. Michael Rodriguez", Specialty = "Dermatology", ImageUrl = "/api/placeholder/80/80", Rating = 4.8m, ReviewCount = 189, YearsExperience = 12, Languages = new[] { "English", "Spanish" }, IsAvailable = true, ConsultationFee = 120, IsVerified = true, NextAvailable = "Today 4:30 PM" },
            new Doctor { Id = 3, Name = "Dr. Aria Patel", Specialty = "General Practice", ImageUrl = "/api/placeholder/80/80", Rating = 4.7m, ReviewCount = 312, YearsExperience = 8, Languages = new[] { "English", "Hindi" }, IsAvailable = false, ConsultationFee = 100, IsVerified = true, NextAvailable = "Tomorrow 9:00 AM" },
            new Doctor { Id = 4, Name = "Dr. James Wilson", Specialty = "Cardiology", ImageUrl = "/api/placeholder/80/80", Rating = 4.9m, ReviewCount = 267, YearsExperience = 20, Languages = new[] { "English" }, IsAvailable = true, ConsultationFee = 180, IsVerified = true, NextAvailable = "Today 3:15 PM" },
            new Doctor { Id = 5, Name = "Dr. Emily Thompson", Specialty = "Nutrition", ImageUrl = "/api/placeholder/80/80", Rating = 4.6m, ReviewCount = 145, YearsExperience = 10, Languages = new[] { "English", "French" }, IsAvailable = true, ConsultationFee = 110, IsVerified = false, NextAvailable = "Today 5:00 PM" }
        };
    }

    public List<string> GetSpecialties()
    {
        return new List<string> { "All Specialties", "Cardiology", "Dermatology", "General Practice", "Nutrition" };
    }
}
