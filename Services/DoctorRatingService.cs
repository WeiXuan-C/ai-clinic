using AiClinic.UI.State;

namespace AiClinic.Services;

/// <summary>
/// Doctor Rating Service - Business Logic Layer
/// Handles doctor rating operations through state management
/// </summary>
public class DoctorRatingService
{
    private readonly DoctorRatingState _state;

    public DoctorRatingService(DoctorRatingState state)
    {
        _state = state;
    }

    /// <summary>
    /// Gets all doctor ratings
    /// </summary>
    public async Task<IEnumerable<DoctorRating>> GetAllRatingsAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Gets a rating by ID
    /// </summary>
    public async Task<DoctorRating?> GetRatingByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Creates a new rating
    /// </summary>
    public async Task<DoctorRating?> CreateRatingAsync(DoctorRating rating)
    {
        return await _state.CreateAsync(rating);
    }

    /// <summary>
    /// Updates a rating
    /// </summary>
    public async Task<DoctorRating?> UpdateRatingAsync(DoctorRating rating)
    {
        return await _state.UpdateAsync(rating);
    }

    /// <summary>
    /// Deletes a rating
    /// </summary>
    public async Task<bool> DeleteRatingAsync(Guid id)
    {
        return await _state.DeleteAsync(id);
    }

    /// <summary>
    /// Gets cached ratings from state
    /// </summary>
    public IReadOnlyList<DoctorRating> GetCachedRatings()
    {
        return _state.Ratings;
    }

    /// <summary>
    /// Gets the currently selected rating
    /// </summary>
    public DoctorRating? GetSelectedRating()
    {
        return _state.SelectedRating;
    }

    /// <summary>
    /// Sets the selected rating
    /// </summary>
    public void SetSelectedRating(DoctorRating? rating)
    {
        _state.SelectedRating = rating;
    }
}
