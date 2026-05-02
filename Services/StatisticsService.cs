using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

public class StatisticsService
{
    public async Task<Dictionary<UserRole, int>> GetUserCountByRoleAsync()
    {
        using var db = DbClient.Instance.GetDb();
        var counts = await db.Users
            .Where(u => u.IsActive)
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key, Count = g.Count() })
            .ToListAsync();

        return counts.ToDictionary(x => x.Role, x => x.Count);
    }

    public async Task<ConversationStats> GetConversationStatsAsync()
    {
        using var db = DbClient.Instance.GetDb();
        var total = await db.Conversations.CountAsync();
        var active = await db.Conversations.CountAsync(c => c.Status == ConversationStatus.Active);
        var closed = await db.Conversations.CountAsync(c => c.Status == ConversationStatus.Closed);

        return new ConversationStats
        {
            TotalConversations = total,
            ActiveConversations = active,
            CompletedConversations = closed
        };
    }

    public async Task<DoctorPerformanceStats> GetDoctorPerformanceAsync(Guid doctorId)
    {
        using var db = DbClient.Instance.GetDb();
        var doctor = await db.DoctorProfiles
            .FirstOrDefaultAsync(d => d.UserId == doctorId);

        if (doctor == null)
        {
            return new DoctorPerformanceStats();
        }

        var totalConsultations = await db.Conversations
            .CountAsync(c => c.AssignedDoctorId == doctorId);

        var completedConsultations = await db.Conversations
            .CountAsync(c => c.AssignedDoctorId == doctorId &&
                           c.Status == ConversationStatus.Closed);

        return new DoctorPerformanceStats
        {
            DoctorId = doctorId,
            TotalConsultations = totalConsultations,
            CompletedConsultations = completedConsultations,
            AverageRating = doctor.AverageRating,
            TotalRatings = doctor.TotalRatings
        };
    }

    public async Task<PatientActivityStats> GetPatientActivityAsync(Guid patientId)
    {
        using var db = DbClient.Instance.GetDb();
        var totalConversations = await db.Conversations
            .CountAsync(c => c.PatientId == patientId);

        var totalMedicalRecords = await db.MedicalRecords
            .CountAsync(mr => mr.PatientId == patientId);

        var totalPrescriptions = await db.Prescriptions
            .CountAsync(p => p.PatientId == patientId);

        return new PatientActivityStats
        {
            PatientId = patientId,
            TotalConversations = totalConversations,
            TotalMedicalRecords = totalMedicalRecords,
            TotalPrescriptions = totalPrescriptions
        };
    }
}

public class ConversationStats
{
    public int TotalConversations { get; set; }
    public int ActiveConversations { get; set; }
    public int CompletedConversations { get; set; }
}

public class DoctorPerformanceStats
{
    public Guid DoctorId { get; set; }
    public int TotalConsultations { get; set; }
    public int CompletedConsultations { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
}

public class PatientActivityStats
{
    public Guid PatientId { get; set; }
    public int TotalConversations { get; set; }
    public int TotalMedicalRecords { get; set; }
    public int TotalPrescriptions { get; set; }
}
