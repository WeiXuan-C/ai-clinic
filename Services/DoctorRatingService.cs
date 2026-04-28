using AiClinic.Interfaces;
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
    public async Task<IEnumerable<IDoctorRating>> GetAllRatingsAsync()
    {
        return await _state.GetAllAsync();
    }

    /// <summary>
    /// Gets a rating by ID
    /// </summary>
    public async Task<IDoctorRating?> GetRatingByIdAsync(Guid id)
    {
        return await _state.GetByIdAsync(id);
    }

    /// <summary>
    /// Creates a new rating
    /// </summary>
    public async Task<IDoctorRating?> CreateRatingAsync(IDoctorRating rating)
    {
        var concreteRating = rating as DoctorRating ?? new DoctorRating
        {
            Id = rating.Id,
            DoctorId = rating.DoctorId,
            PatientId = rating.PatientId,
            ConversationId = rating.ConversationId,
            Rating = rating.Rating,
            ReviewText = rating.ReviewText,
            ProfessionalismRating = rating.ProfessionalismRating,
            CommunicationRating = rating.CommunicationRating,
            KnowledgeRating = rating.KnowledgeRating,
            ResponseTimeRating = rating.ResponseTimeRating,
            CreatedAt = rating.CreatedAt
        };
        return await _state.CreateAsync(concreteRating);
    }

    /// <summary>
    /// Updates a rating
    /// </summary>
    public async Task<IDoctorRating?> UpdateRatingAsync(IDoctorRating rating)
    {
        var concreteRating = rating as DoctorRating ?? new DoctorRating
        {
            Id = rating.Id,
            DoctorId = rating.DoctorId,
            PatientId = rating.PatientId,
            ConversationId = rating.ConversationId,
            Rating = rating.Rating,
            ReviewText = rating.ReviewText,
            ProfessionalismRating = rating.ProfessionalismRating,
            CommunicationRating = rating.CommunicationRating,
            KnowledgeRating = rating.KnowledgeRating,
            ResponseTimeRating = rating.ResponseTimeRating,
            CreatedAt = rating.CreatedAt
        };
        return await _state.UpdateAsync(concreteRating);
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
    public IReadOnlyList<IDoctorRating> GetCachedRatings()
    {
        return _state.Ratings.Cast<IDoctorRating>().ToList();
    }

    /// <summary>
    /// Gets the currently selected rating
    /// </summary>
    public IDoctorRating? GetSelectedRating()
    {
        return _state.SelectedRating;
    }

    /// <summary>
    /// Sets the selected rating
    /// </summary>
    public void SetSelectedRating(IDoctorRating? rating)
    {
        _state.SelectedRating = rating as DoctorRating;
    }

    // Controller-facing methods (adapters for backward compatibility)
    
    public async Task<IDoctorRating?> CreateRatingAsync(object request)
    {
        // Extract properties from request object dynamically
        var requestType = request.GetType();
        var doctorId = Guid.Parse(requestType.GetProperty("DoctorId")?.GetValue(request)?.ToString() ?? Guid.NewGuid().ToString());
        var patientId = Guid.Parse(requestType.GetProperty("PatientId")?.GetValue(request)?.ToString() ?? Guid.NewGuid().ToString());
        var conversationId = Guid.Parse(requestType.GetProperty("ConversationId")?.GetValue(request)?.ToString() ?? Guid.NewGuid().ToString());
        var rating = int.Parse(requestType.GetProperty("Rating")?.GetValue(request)?.ToString() ?? "5");
        var reviewText = requestType.GetProperty("ReviewText")?.GetValue(request)?.ToString();
        
        var doctorRating = new DoctorRating
        {
            Id = Guid.NewGuid(),
            DoctorId = doctorId,
            PatientId = patientId,
            ConversationId = conversationId,
            Rating = rating,
            ReviewText = reviewText,
            CreatedAt = DateTime.UtcNow
        };
        
        return await CreateRatingAsync((IDoctorRating)doctorRating);
    }
    
    public async Task<IDoctorRating?> GetRatingByIdAsync(string ratingId)
    {
        if (Guid.TryParse(ratingId, out var guid))
        {
            return await GetRatingByIdAsync(guid);
        }
        return null;
    }
    
    public async Task<IEnumerable<IDoctorRating>> GetRatingsByDoctorIdAsync(string doctorId)
    {
        if (Guid.TryParse(doctorId, out var guid))
        {
            return await _state.GetByDoctorIdAsync(guid);
        }
        return Enumerable.Empty<IDoctorRating>();
    }
    
    public async Task<double> GetAverageRatingAsync(string doctorId)
    {
        if (Guid.TryParse(doctorId, out var guid))
        {
            return await _state.GetAverageRatingAsync(guid);
        }
        return 0.0;
    }
    
    public async Task<IEnumerable<IDoctorRating>> GetRatingsByPatientIdAsync(string patientId)
    {
        if (Guid.TryParse(patientId, out var guid))
        {
            return await _state.GetByPatientIdAsync(guid);
        }
        return Enumerable.Empty<IDoctorRating>();
    }
    
    public async Task<IDoctorRating?> UpdateRatingAsync(string ratingId, object updates)
    {
        if (!Guid.TryParse(ratingId, out var guid))
        {
            return null;
        }
        
        var existing = await GetRatingByIdAsync(guid);
        if (existing == null)
        {
            return null;
        }
        
        // Extract properties from updates object dynamically
        var updatesType = updates.GetType();
        var rating = int.Parse(updatesType.GetProperty("Rating")?.GetValue(updates)?.ToString() ?? existing.Rating.ToString());
        var reviewText = updatesType.GetProperty("ReviewText")?.GetValue(updates)?.ToString() ?? existing.ReviewText;
        
        var updated = new DoctorRating
        {
            Id = guid,
            DoctorId = existing.DoctorId,
            PatientId = existing.PatientId,
            ConversationId = existing.ConversationId,
            Rating = rating,
            ReviewText = reviewText,
            ProfessionalismRating = existing.ProfessionalismRating,
            CommunicationRating = existing.CommunicationRating,
            KnowledgeRating = existing.KnowledgeRating,
            ResponseTimeRating = existing.ResponseTimeRating,
            CreatedAt = existing.CreatedAt
        };
        
        return await UpdateRatingAsync(updated);
    }
    
    public async Task<bool> DeleteRatingAsync(string ratingId)
    {
        if (Guid.TryParse(ratingId, out var guid))
        {
            return await DeleteRatingAsync(guid);
        }
        return false;
    }
}
