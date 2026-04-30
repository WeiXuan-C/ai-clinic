using ai_clinic.Interfaces;

namespace ai_clinic.UI.State;

/// <summary>
/// Scoped Doctor Rating State for Blazor (Redux-like pattern)
/// Manages doctor rating data, cache, and Supabase CRUD operations
/// </summary>
public class DoctorRatingState
{
    private readonly IDoctorRatingRepository _repository;
    private List<DoctorRating> _ratings = new();
    private DoctorRating? _selectedRating;
    private bool _isLoading;
    private string? _errorMessage;

    public DoctorRatingState(IDoctorRatingRepository repository)
    {
        _repository = repository;
    }

    public event Action? OnChange;

    public IReadOnlyList<DoctorRating> Ratings => _ratings.AsReadOnly();
    public DoctorRating? SelectedRating
    {
        get => _selectedRating;
        set
        {
            _selectedRating = value;
            NotifyStateChanged();
        }
    }
    public bool IsLoading => _isLoading;
    public string? ErrorMessage => _errorMessage;

    public async Task<IEnumerable<DoctorRating>> GetAllAsync()
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var ratings = await _repository.GetAllAsync();
            _ratings = ratings.ToList();
            return ratings;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<DoctorRating>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<DoctorRating?> GetByIdAsync(Guid id)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var rating = await _repository.GetByIdAsync(id);
            if (rating != null)
            {
                var index = _ratings.FindIndex(r => r.Id == id);
                if (index >= 0)
                    _ratings[index] = rating;
                else
                    _ratings.Add(rating);
            }
            return rating;
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

    public async Task<DoctorRating?> CreateAsync(DoctorRating rating)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var created = await _repository.AddAsync(rating);
            _ratings.Add(created);
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

    public async Task<DoctorRating?> UpdateAsync(DoctorRating rating)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var updated = await _repository.UpdateAsync(rating);
            var index = _ratings.FindIndex(r => r.Id == rating.Id);
            if (index >= 0)
                _ratings[index] = updated;
            if (_selectedRating?.Id == rating.Id)
                _selectedRating = updated;
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
                _ratings.RemoveAll(r => r.Id == id);
                if (_selectedRating?.Id == id)
                    _selectedRating = null;
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
        _ratings.Clear();
        _selectedRating = null;
        _errorMessage = null;
        NotifyStateChanged();
    }

    public async Task<IEnumerable<DoctorRating>> GetByDoctorIdAsync(Guid doctorId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var ratings = await _repository.GetByDoctorIdAsync(doctorId);
            return ratings;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<DoctorRating>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<IEnumerable<DoctorRating>> GetByPatientIdAsync(Guid patientId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            var ratings = await _repository.GetByPatientIdAsync(patientId);
            return ratings;
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return Enumerable.Empty<DoctorRating>();
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    public async Task<double> GetAverageRatingAsync(Guid doctorId)
    {
        try
        {
            _isLoading = true;
            _errorMessage = null;
            NotifyStateChanged();

            return await _repository.GetAverageRatingAsync(doctorId);
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
            return 0.0;
        }
        finally
        {
            _isLoading = false;
            NotifyStateChanged();
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
