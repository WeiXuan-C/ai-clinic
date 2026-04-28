namespace AiClinic.Controller;

public class DoctorRatingController
{
    private readonly Interfaces.DoctorRatingInterface _ratingService;

    public DoctorRatingController(Interfaces.DoctorRatingInterface ratingService)
    {
        _ratingService = ratingService;
    }

    public async Task<object> CreateRatingAsync(CreateRatingRequest request)
    {
        return await _ratingService.CreateRatingAsync(request);
    }

    public async Task<object?> GetRatingByIdAsync(string ratingId)
    {
        return await _ratingService.GetRatingByIdAsync(ratingId);
    }

    public async Task<object> GetRatingsByDoctorIdAsync(string doctorId)
    {
        return await _ratingService.GetRatingsByDoctorIdAsync(doctorId);
    }

    public async Task<double> GetAverageRatingAsync(string doctorId)
    {
        return await _ratingService.GetAverageRatingAsync(doctorId);
    }

    public async Task<object> GetRatingsByPatientIdAsync(string patientId)
    {
        return await _ratingService.GetRatingsByPatientIdAsync(patientId);
    }

    public async Task<object> UpdateRatingAsync(string ratingId, UpdateRatingRequest request)
    {
        return await _ratingService.UpdateRatingAsync(ratingId, request);
    }

    public async Task DeleteRatingAsync(string ratingId)
    {
        await _ratingService.DeleteRatingAsync(ratingId);
    }
}

public record CreateRatingRequest(string DoctorId, string PatientId, int Rating, string? Comment);
public record UpdateRatingRequest(int Rating, string? Comment);
