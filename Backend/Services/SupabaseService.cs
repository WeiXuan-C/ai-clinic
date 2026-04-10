using ai_clinic.Backend.Models;
using Supabase;

namespace ai_clinic.Backend.Services;

/// <summary>
/// Example service demonstrating Supabase operations
/// </summary>
public class SupabaseService
{
    private readonly Client _supabase;

    public SupabaseService(Client supabase)
    {
        _supabase = supabase;
    }

    public async Task<List<Patient>> GetPatientsAsync()
    {
        var response = await _supabase
            .From<Patient>()
            .Get();
        
        return response.Models;
    }

    public async Task<Patient?> GetPatientByIdAsync(int id)
    {
        var response = await _supabase
            .From<Patient>()
            .Where(p => p.Id == id)
            .Single();
        
        return response;
    }

    public async Task<Patient> CreatePatientAsync(Patient patient)
    {
        var response = await _supabase
            .From<Patient>()
            .Insert(patient);
        
        return response.Models.First();
    }

    public async Task<Patient> UpdatePatientAsync(Patient patient)
    {
        var response = await _supabase
            .From<Patient>()
            .Update(patient);
        
        return response.Models.First();
    }

    public async Task DeletePatientAsync(int id)
    {
        await _supabase
            .From<Patient>()
            .Where(p => p.Id == id)
            .Delete();
    }
}
