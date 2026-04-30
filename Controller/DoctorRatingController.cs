using ai_clinic.Interfaces;

namespace ai_clinic.Controller;

public class DoctorRatingController(Services.DoctorRatingService ratingService)
{
    public Task<IDoctorRating?> CreateRatingAsync(CreateRatingRequest request)
    {
        return ratingService.CreateRatingAsync(request);
    }

    public Task<IDoctorRating?> GetRatingByIdAsync(string ratingId)
    {
        return ratingService.GetRatingByIdAsync(ratingId);
    }

    public Task<IEnumerable<IDoctorRating>> GetRatingsByDoctorIdAsync(string doctorId)
    {
        return ratingService.GetRatingsByDoctorIdAsync(doctorId);
    }

    public Task<double> GetAverageRatingAsync(string doctorId)
    {
        return ratingService.GetAverageRatingAsync(doctorId);
    }

    public Task<IEnumerable<IDoctorRating>> GetRatingsByPatientIdAsync(string patientId)
    {
        return ratingService.GetRatingsByPatientIdAsync(patientId);
    }

    public Task<IDoctorRating?> UpdateRatingAsync(string ratingId, UpdateRatingRequest request)
    {
        return ratingService.UpdateRatingAsync(ratingId, request);
    }

    public Task<bool> DeleteRatingAsync(string ratingId)
    {
        return ratingService.DeleteRatingAsync(ratingId);
    }
}

public record CreateRatingRequest(string DoctorId, string PatientId, int Rating, string? Comment);
public record UpdateRatingRequest(int Rating, string? Comment);
