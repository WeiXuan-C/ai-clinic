using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

/// <summary>
/// Service for managing Prescriptions
/// </summary>
public class PrescriptionService
{
    /// <summary>
    /// Get prescription by ID
    /// </summary>
    public async Task<Prescription?> GetByIdAsync(Guid prescriptionId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Include(p => p.ConsultationNote)
            .FirstOrDefaultAsync(p => p.Id == prescriptionId);
    }

    /// <summary>
    /// Get all prescriptions for a patient
    /// </summary>
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

    /// <summary>
    /// Get active prescriptions for a patient
    /// </summary>
    public async Task<List<Prescription>> GetActiveByPatientIdAsync(Guid patientId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Prescriptions
            .Include(p => p.Doctor)
            .Include(p => p.ConsultationNote)
            .Where(p => p.PatientId == patientId && p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get all prescriptions for a consultation note
    /// </summary>
    public async Task<List<Prescription>> GetByConsultationNoteIdAsync(Guid consultationNoteId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Doctor)
            .Where(p => p.ConsultationNoteId == consultationNoteId)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get all prescriptions by a doctor
    /// </summary>
    public async Task<List<Prescription>> GetByDoctorIdAsync(Guid doctorId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.ConsultationNote)
            .Where(p => p.DoctorId == doctorId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Create a new prescription
    /// </summary>
    public async Task<Prescription> CreateAsync(Prescription prescription)
    {
        prescription.CreatedAt = DateTime.UtcNow;
        prescription.UpdatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.Prescriptions.Add(prescription);
        await db.SaveChangesAsync();
        
        Console.WriteLine($"[PRESCRIPTION SERVICE] Prescription created: {prescription.Id}");
        Console.WriteLine($"[PRESCRIPTION SERVICE] Medication: {prescription.MedicationName}, Dosage: {prescription.Dosage}");
        
        return prescription;
    }

    /// <summary>
    /// Create multiple prescriptions at once
    /// </summary>
    public async Task<List<Prescription>> CreateBatchAsync(List<Prescription> prescriptions)
    {
        using var db = DbClient.Instance.GetDb();
        
        foreach (var prescription in prescriptions)
        {
            prescription.CreatedAt = DateTime.UtcNow;
            prescription.UpdatedAt = DateTime.UtcNow;
            db.Prescriptions.Add(prescription);
        }
        
        await db.SaveChangesAsync();
        
        Console.WriteLine($"[PRESCRIPTION SERVICE] {prescriptions.Count} prescriptions created");
        
        return prescriptions;
    }

    /// <summary>
    /// Update a prescription
    /// </summary>
    public async Task<Prescription> UpdateAsync(Prescription prescription)
    {
        prescription.UpdatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.Prescriptions.Update(prescription);
        await db.SaveChangesAsync();
        
        return prescription;
    }

    public async Task<bool> DeleteAsync(Guid prescriptionId)
    {
        using var db = DbClient.Instance.GetDb();
        var prescription = await db.Prescriptions.FindAsync(prescriptionId);
        if (prescription != null)
        {
            db.Prescriptions.Remove(prescription);
            await db.SaveChangesAsync();
            return true;
        }
        return false;
    }
    /// <summary>
    /// Deactivate a prescription (soft delete)
    /// </summary>
    public async Task DeactivateAsync(Guid prescriptionId)
    {
        using var db = DbClient.Instance.GetDb();
        var prescription = await db.Prescriptions.FindAsync(prescriptionId);
        
        if (prescription != null)
        {
            prescription.IsActive = false;
            prescription.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            
            Console.WriteLine($"[PRESCRIPTION SERVICE] Prescription deactivated: {prescriptionId}");
        }
    }

    /// <summary>
    /// Reactivate a prescription
    /// </summary>
    public async Task ReactivateAsync(Guid prescriptionId)
    {
        using var db = DbClient.Instance.GetDb();
        var prescription = await db.Prescriptions.FindAsync(prescriptionId);
        
        if (prescription != null)
        {
            prescription.IsActive = true;
            prescription.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            
            Console.WriteLine($"[PRESCRIPTION SERVICE] Prescription reactivated: {prescriptionId}");
        }
    }

    /// <summary>
    /// Get prescription statistics for a patient
    /// </summary>
    public async Task<PrescriptionStatistics> GetPatientStatisticsAsync(Guid patientId)
    {
        using var db = DbClient.Instance.GetDb();
        
        var prescriptions = await db.Prescriptions
            .Where(p => p.PatientId == patientId)
            .ToListAsync();

        return new PrescriptionStatistics
        {
            TotalPrescriptions = prescriptions.Count,
            ActivePrescriptions = prescriptions.Count(p => p.IsActive),
            InactivePrescriptions = prescriptions.Count(p => !p.IsActive),
            UniqueMedications = prescriptions.Select(p => p.MedicationName).Distinct().Count()
        };
    }
}

/// <summary>
/// DTO for prescription statistics
/// </summary>
public class PrescriptionStatistics
{
    public int TotalPrescriptions { get; set; }
    public int ActivePrescriptions { get; set; }
    public int InactivePrescriptions { get; set; }
    public int UniqueMedications { get; set; }
}
