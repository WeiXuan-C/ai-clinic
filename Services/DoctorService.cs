using AiClinic.Core.Entities;
using AiClinic.Core.Interfaces;

namespace AiClinic.Services;

/// <summary>
/// Doctor Service - Business Logic Layer
/// Handles doctor matching, assignment, and availability management
/// </summary>
public class DoctorService
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly IConversationRepository _conversationRepository;

    public DoctorService(
        IDoctorRepository doctorRepository,
        IConversationRepository conversationRepository)
    {
        _doctorRepository = doctorRepository;
        _conversationRepository = conversationRepository;
    }

    /// <summary>
    /// Finds the best available doctor based on specialization, workload, and rating
    /// </summary>
    public async Task<Doctor?> FindAvailableDoctorAsync(string? specialization = null)
    {
        IEnumerable<Doctor> doctors;
        
        if (!string.IsNullOrEmpty(specialization))
        {
            doctors = await _doctorRepository.GetBySpecializationAsync(specialization);
        }
        else
        {
            doctors = await _doctorRepository.GetAvailableDoctorsAsync();
        }
        
        if (!doctors.Any())
        {
            return null;
        }
        
        // Score each doctor based on multiple factors
        var scoredDoctors = doctors.Select(d => new
        {
            Doctor = d,
            Score = CalculateDoctorScore(d)
        })
        .OrderByDescending(x => x.Score)
        .ToList();
        
        return scoredDoctors.First().Doctor;
    }

    /// <summary>
    /// Creates a new doctor profile
    /// </summary>
    public async Task<Doctor> CreateDoctorProfileAsync(Guid userId, string fullName, string licenseNumber, string specialization)
    {
        var doctor = new Doctor
        {
            UserId = userId,
            FullName = fullName,
            LicenseNumber = licenseNumber,
            PrimarySpecialization = specialization,
            AvailabilityStatus = "offline",
            CurrentActiveConversations = 0,
            AverageRating = 0,
            TotalConsultations = 0,
            IsVerified = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        return await _doctorRepository.AddAsync(doctor);
    }

    /// <summary>
    /// Calculates doctor score based on rating, workload, and experience
    /// </summary>
    private double CalculateDoctorScore(Doctor doctor)
    {
        // Rating weight: 40%
        var ratingScore = (double)doctor.AverageRating * 0.4;
        
        // Workload weight: 30% (inverse - lower workload is better)
        var workloadScore = (10 - Math.Min(doctor.CurrentActiveConversations, 10)) * 0.3;
        
        // Experience weight: 20%
        var experienceScore = Math.Min(doctor.YearsOfExperience ?? 0, 30) / 30.0 * 0.2;
        
        // Total consultations weight: 10%
        var consultationScore = Math.Min(doctor.TotalConsultations, 1000) / 1000.0 * 0.1;
        
        return ratingScore + workloadScore + experienceScore + consultationScore;
    }

    /// <summary>
    /// Gets all available doctors
    /// </summary>
    public async Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync()
    {
        return await _doctorRepository.GetAvailableDoctorsAsync();
    }

    /// <summary>
    /// Gets doctors by specialization
    /// </summary>
    public async Task<IEnumerable<Doctor>> GetDoctorsBySpecializationAsync(string specialization)
    {
        return await _doctorRepository.GetBySpecializationAsync(specialization);
    }

    /// <summary>
    /// Gets doctor by user ID
    /// </summary>
    public async Task<Doctor?> GetDoctorByUserIdAsync(Guid userId)
    {
        return await _doctorRepository.GetByUserIdAsync(userId);
    }

    /// <summary>
    /// Gets doctor by ID
    /// </summary>
    public async Task<Doctor?> GetDoctorByIdAsync(Guid doctorId)
    {
        return await _doctorRepository.GetByIdAsync(doctorId);
    }

    /// <summary>
    /// Updates doctor availability status
    /// </summary>
    public async Task UpdateDoctorAvailabilityAsync(Guid doctorId, string status)
    {
        await _doctorRepository.UpdateAvailabilityStatusAsync(doctorId, status);
    }

    /// <summary>
    /// Gets all conversations assigned to a doctor
    /// </summary>
    public async Task<IEnumerable<Conversation>> GetDoctorConversationsAsync(Guid doctorId)
    {
        return await _conversationRepository.GetByDoctorIdAsync(doctorId);
    }

    /// <summary>
    /// Updates doctor profile
    /// </summary>
    public async Task<Doctor> UpdateDoctorProfileAsync(Doctor doctor)
    {
        return await _doctorRepository.UpdateAsync(doctor);
    }

    /// <summary>
    /// Gets all doctors (for admin/listing purposes)
    /// </summary>
    public async Task<IEnumerable<Doctor>> GetAllDoctorsAsync()
    {
        return await _doctorRepository.GetAllAsync();
    }

    /// <summary>
    /// Increments active conversation count for a doctor
    /// </summary>
    public async Task IncrementActiveConversationsAsync(Guid doctorId)
    {
        var doctor = await _doctorRepository.GetByIdAsync(doctorId);
        
        if (doctor != null)
        {
            doctor.CurrentActiveConversations++;
            
            // Auto-set to busy if at capacity (e.g., 5 conversations)
            if (doctor.CurrentActiveConversations >= 5)
            {
                doctor.AvailabilityStatus = "busy";
            }
            
            await _doctorRepository.UpdateAsync(doctor);
        }
    }

    /// <summary>
    /// Decrements active conversation count for a doctor
    /// </summary>
    public async Task DecrementActiveConversationsAsync(Guid doctorId)
    {
        var doctor = await _doctorRepository.GetByIdAsync(doctorId);
        
        if (doctor != null && doctor.CurrentActiveConversations > 0)
        {
            doctor.CurrentActiveConversations--;
            
            // Auto-set to available if below capacity
            if (doctor.CurrentActiveConversations < 5 && doctor.AvailabilityStatus == "busy")
            {
                doctor.AvailabilityStatus = "available";
            }
            
            await _doctorRepository.UpdateAsync(doctor);
        }
    }
}
