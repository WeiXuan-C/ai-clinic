using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

public class MedicalRecordService
{
    public async Task<List<MedicalRecord>> GetByPatientIdAsync(Guid patientId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.MedicalRecords
            .Include(mr => mr.CreatedByDoctor)
            .Include(mr => mr.Conversation)
            .Where(mr => mr.PatientId == patientId)
            .OrderByDescending(mr => mr.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<MedicalRecord>> GetByDoctorIdAsync(Guid doctorId)
    {
        try
        {
            using var db = DbClient.Instance.GetDb();
            
            Console.WriteLine($"[MedicalRecordService] Fetching medical records for doctor {doctorId}");
            
            var records = await db.MedicalRecords
                .Include(mr => mr.Patient)
                .Where(mr => mr.CreatedByDoctorId == doctorId)
                .OrderByDescending(mr => mr.CreatedAt)
                .AsNoTracking() // Prevent tracking to avoid circular references
                .ToListAsync();
            
            Console.WriteLine($"[MedicalRecordService] Found {records.Count} medical records");
            return records;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MedicalRecordService] Error in GetByDoctorIdAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<MedicalRecord?> GetByIdAsync(Guid recordId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.MedicalRecords
            .Include(mr => mr.Patient)
            .Include(mr => mr.CreatedByDoctor)
            .Include(mr => mr.Conversation)
            .FirstOrDefaultAsync(mr => mr.Id == recordId);
    }

    public async Task<MedicalRecord> CreateAsync(MedicalRecord record)
    {
        record.CreatedAt = DateTime.UtcNow;
        record.UpdatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.MedicalRecords.Add(record);
        await db.SaveChangesAsync();
        return record;
    }

    public async Task<MedicalRecord> UpdateAsync(MedicalRecord record)
    {
        record.UpdatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.MedicalRecords.Update(record);
        await db.SaveChangesAsync();
        return record;
    }

    public async Task<bool> DeleteAsync(Guid recordId)
    {
        using var db = DbClient.Instance.GetDb();
        var record = await db.MedicalRecords.FindAsync(recordId);
        if (record != null)
        {
            db.MedicalRecords.Remove(record);
            await db.SaveChangesAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Update export statistics for a medical record
    /// </summary>
    public async Task UpdateExportStatisticsAsync(Guid recordId)
    {
        using var db = DbClient.Instance.GetDb();
        var record = await db.MedicalRecords.FindAsync(recordId);
        if (record != null)
        {
            record.IsExported = true;
            record.ExportCount++;
            record.LastExportedAt = DateTime.UtcNow;
            record.UpdatedAt = DateTime.UtcNow;
            
            db.MedicalRecords.Update(record);
            await db.SaveChangesAsync();
        }
    }
}
