using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

public class PrescriptionService
{
    public async Task<Prescription?> GetByIdAsync(Guid prescriptionId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.Patient)
            .Include(p => p.ConsultationNote)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId);
    }

    public async Task<List<Prescription>> GetByPatientIdAsync(Guid patientId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.ConsultationNote)
            .Where(p => p.PatientId == patientId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<Prescription> CreateAsync(Prescription prescription)
    {
        prescription.CreatedAt = DateTime.UtcNow;
        prescription.UpdatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync();
        return prescription;
    }

    public async Task<Prescription> UpdateAsync(Prescription prescription)
    {
        prescription.UpdatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.Prescriptions.Update(prescription);
        await db.SaveChangesAsync();
        return prescription;
    }
}
