using AiClinic.Interfaces;

namespace AiClinic.UI.State;

/// <summary>
/// Scoped Patient Profile State for Blazor (Redux-like pattern)
/// Manages patient profile data, cache, and Supabase CRUD operations
/// </summary>
public class PatientProfileState
{
    private readonly IPatientProfileRepository _repository;
    private PatientProfile? _currentProfile;
    private bool _isLoading;
    private string? _errorMessage;

    public PatientProfileState(IPatientProfileRepository repository)
    {
        _repository = repository;
    }

    public event Action? OnChange;

    public PatientProfile? CurrentProfile => _currentProfile;
    public bool IsLoading => _isLoading;
    public string? ErrorMessage => _errorMessage;
    public bool HasProfile => _currentProfile != null;

    public async Task<PatientProfile?> GetByIdAsync(Guid id)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var profile = await _repository.GetByIdAsync(id);
            _currentProfile = profile;
            return profile;
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

    public async Task<PatientProfile?> GetByUserIdAsync(Guid userId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var profile = await _repository.GetByUserIdAsync(userId);
            _currentProfile = profile;
            return profile;
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

    public async Task<IEnumerable<PatientProfile>> GetAllAsync()
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var profiles = await _repository.GetAllAsync();
            return profiles;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<PatientProfile>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<PatientProfile?> CreateAsync(PatientProfile profile)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var created = await _repository.AddAsync(profile);
            _currentProfile = created;
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

    public async Task<PatientProfile?> UpdateAsync(PatientProfile profile)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var updated = await _repository.UpdateAsync(profile);
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
            if (success && _currentProfile?.Id == id)
                _currentProfile = null;
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
        _errorMessage = null;
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
