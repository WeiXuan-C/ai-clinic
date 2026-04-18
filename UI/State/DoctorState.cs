using AiClinic.Core.Entities;

namespace AiClinic.UI.State;

/// <summary>
/// Singleton Pattern Implementation
/// Global doctor state shared across the entire application
/// </summary>
public class DoctorState
{
    private Doctor? _currentDoctor;
    private List<Doctor> _availableDoctors;
    private bool _isLoading;

    public DoctorState()
    {
        _availableDoctors = new List<Doctor>();
    }

    /// <summary>
    /// Event triggered when doctor state changes
    /// </summary>
    public event Action? OnChange;

    /// <summary>
    /// Current doctor profile (for logged-in doctors)
    /// </summary>
    public Doctor? CurrentDoctor
    {
        get => _currentDoctor;
        set
        {
            _currentDoctor = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// List of available doctors
    /// </summary>
    public IReadOnlyList<Doctor> AvailableDoctors => _availableDoctors.AsReadOnly();

    /// <summary>
    /// Whether doctor data is currently loading
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Current doctor's availability status
    /// </summary>
    public string? AvailabilityStatus => _currentDoctor?.AvailabilityStatus;

    /// <summary>
    /// Whether current doctor is available
    /// </summary>
    public bool IsAvailable => _currentDoctor?.AvailabilityStatus == "available";

    /// <summary>
    /// Current doctor's active conversation count
    /// </summary>
    public int ActiveConversations => _currentDoctor?.CurrentActiveConversations ?? 0;

    /// <summary>
    /// Sets the list of available doctors
    /// </summary>
    public void SetAvailableDoctors(IEnumerable<Doctor> doctors)
    {
        _availableDoctors = doctors.ToList();
        NotifyStateChanged();
    }

    /// <summary>
    /// Adds a doctor to the available list
    /// </summary>
    public void AddDoctor(Doctor doctor)
    {
        if (!_availableDoctors.Any(d => d.Id == doctor.Id))
        {
            _availableDoctors.Add(doctor);
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Removes a doctor from the available list
    /// </summary>
    public void RemoveDoctor(Guid doctorId)
    {
        var doctor = _availableDoctors.FirstOrDefault(d => d.Id == doctorId);
        if (doctor != null)
        {
            _availableDoctors.Remove(doctor);
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Updates current doctor's availability status
    /// </summary>
    public void UpdateAvailabilityStatus(string status)
    {
        if (_currentDoctor != null)
        {
            _currentDoctor.AvailabilityStatus = status;
            NotifyStateChanged();
        }
    }

    /// <summary>
    /// Clears all doctor state
    /// </summary>
    public void Clear()
    {
        _currentDoctor = null;
        _availableDoctors.Clear();
        NotifyStateChanged();
    }

    /// <summary>
    /// Notifies subscribers that state has changed
    /// </summary>
    private void NotifyStateChanged()
    {
        OnChange?.Invoke();
    }
}
