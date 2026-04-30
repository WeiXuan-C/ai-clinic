using ai_clinic.Interfaces;

namespace ai_clinic.UI.State;

/// <summary>
/// Scoped Doctor Profile State for Blazor (Redux-like pattern)
/// Manages doctor profile data, cache, and Supabase CRUD operations
/// </summary>
public class DoctorProfileState
{
    private readonly IDoctorRepository _repository;
    private Doctor? _currentProfile;
    private List<Doctor> _doctors = new();
    private bool _isLoading;
    private string? _errorMessage;

    public DoctorProfileState(IDoctorRepository repository)
    {
        _repository = repository;
    }

    public event Action? OnChange;

    public Doctor? CurrentProfile => _currentProfile;
    public Doctor? CurrentDoctor
    {
        get => _currentProfile;
        set
        {
            _currentProfile = value;
            NotifyStateChanged();
        }
    }
    public IReadOnlyList<Doctor> Doctors => _doctors.AsReadOnly();
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            NotifyStateChanged();
        }
    }
    public string? ErrorMessage => _errorMessage;
    public bool HasProfile => _currentProfile != null;

    public async Task<Doctor?> GetByIdAsync(Guid id)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var doctor = await _repository.GetByIdAsync(id);
            if (doctor != null)
            {
                _currentProfile = doctor;
                var index = _doctors.FindIndex(d => d.Id == id);
                if (index >= 0)
                    _doctors[index] = doctor;
                else
                    _doctors.Add(doctor);
            }
            return doctor;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<Doctor?> GetByUserIdAsync(Guid userId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var doctor = await _repository.GetByUserIdAsync(userId);
            _currentProfile = doctor;
            return doctor;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<IEnumerable<Doctor>> GetAllAsync()
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var doctors = await _repository.GetAllAsync();
            _doctors = doctors.ToList();
            return doctors;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<Doctor>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<IEnumerable<Doctor>> GetAvailableDoctorsAsync()
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var doctors = await _repository.GetAvailableDoctorsAsync();
            _doctors = doctors.ToList();
            return doctors;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<Doctor>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<IEnumerable<Doctor>> GetBySpecializationAsync(string specialization)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var doctors = await _repository.GetBySpecializationAsync(specialization);
            _doctors = doctors.ToList();
            return doctors;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<Doctor>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<IEnumerable<Doctor>> GetByOrganizationIdAsync(Guid organizationId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var doctors = await _repository.GetByOrganizationIdAsync(organizationId);
            _doctors = doctors.ToList();
            return doctors;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<Doctor>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task UpdateAvailabilityStatusAsync(Guid doctorId, string status)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            await _repository.UpdateAvailabilityStatusAsync(doctorId, status);
            
            // Update cache
            var index = _doctors.FindIndex(d => d.Id == doctorId);
            if (index >= 0)
            {
                var doctor = _doctors[index];
                doctor.AvailabilityStatus = status;
                _doctors[index] = doctor;
            }
            if (_currentProfile?.Id == doctorId)
            {
                _currentProfile.AvailabilityStatus = status;
            }
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<Doctor?> CreateAsync(Doctor doctor)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var created = await _repository.AddAsync(doctor);
            _currentProfile = created;
            _doctors.Add(created);
            return created;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<Doctor?> UpdateAsync(Doctor doctor)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var updated = await _repository.UpdateAsync(doctor);
            var index = _doctors.FindIndex(d => d.Id == doctor.Id);
            if (index >= 0)
                _doctors[index] = updated;
            if (_currentProfile?.Id == doctor.Id)
                _currentProfile = updated;
            return updated;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return null;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var success = await _repository.DeleteAsync(id);
            if (success)
            {
                _doctors.RemoveAll(d => d.Id == id);
                if (_currentProfile?.Id == id)
                    _currentProfile = null;
            }
            return success;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return false;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public void ClearCache()
    {
        _currentProfile = null;
        _doctors.Clear();
        _errorMessage = null;
        NotifyStateChanged();
    }

    public void SetAvailableDoctors(IEnumerable<Doctor> doctors)
    {
        _doctors = doctors.ToList();
        NotifyStateChanged();
    }

    public void UpdateAvailabilityStatus(Guid doctorId, string status)
    {
        var doctor = _doctors.FirstOrDefault(d => d.Id == doctorId);
        if (doctor != null)
        {
            doctor.AvailabilityStatus = status;
            doctor.UpdatedAt = DateTime.UtcNow;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
