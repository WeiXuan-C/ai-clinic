using Postgrest.Attributes;
using Postgrest.Models;

namespace AiClinic.Core.Entities;

[Table("doctor_ratings")]
public class DoctorRating : BaseModel
{
    private const int MinRating = 1;
    private const int MaxRating = 5;

    // Primary Key and Foreign Keys
    [PrimaryKey("id")]
    public Guid Id { get; private set; }

    [Column("doctor_id")]
    public Guid DoctorId { get; private set; }

    [Column("patient_id")]
    public Guid PatientId { get; private set; }

    [Column("conversation_id")]
    public Guid ConversationId { get; private set; }

    // Overall Rating
    [Column("rating")]
    public int Rating { get; private set; }

    [Column("review_text")]
    public string? ReviewText { get; private set; }

    // Detailed Ratings
    [Column("professionalism_rating")]
    public int? ProfessionalismRating { get; private set; }

    [Column("communication_rating")]
    public int? CommunicationRating { get; private set; }

    [Column("knowledge_rating")]
    public int? KnowledgeRating { get; private set; }

    [Column("response_time_rating")]
    public int? ResponseTimeRating { get; private set; }

    // Timestamps
    [Column("created_at")]
    public DateTime CreatedAt { get; private set; }

    // Public Methods (Business Logic)
    public void UpdateReview(string reviewText)
    {
        ReviewText = reviewText?.Trim();
    }

    public double CalculateAverageDetailedRating()
    {
        var ratings = new List<int>();

        if (ProfessionalismRating.HasValue) ratings.Add(ProfessionalismRating.Value);
        if (CommunicationRating.HasValue) ratings.Add(CommunicationRating.Value);
        if (KnowledgeRating.HasValue) ratings.Add(KnowledgeRating.Value);
        if (ResponseTimeRating.HasValue) ratings.Add(ResponseTimeRating.Value);

        return ratings.Count > 0 ? ratings.Average() : Rating;
    }

    public bool HasDetailedRatings()
    {
        return ProfessionalismRating.HasValue ||
               CommunicationRating.HasValue ||
               KnowledgeRating.HasValue ||
               ResponseTimeRating.HasValue;
    }

    // Private Helper Methods
    private void ValidateRating(int rating, string ratingName)
    {
        if (rating < MinRating || rating > MaxRating)
            throw new ArgumentException($"{ratingName} must be between {MinRating} and {MaxRating}");
    }

    // Public method for DAO initialization
    public void Initialize(Guid id, Guid doctorId, Guid patientId, Guid conversationId, int rating, DateTime createdAt)
    {
        ValidateRating(rating, "Overall rating");

        Id = id;
        DoctorId = doctorId;
        PatientId = patientId;
        ConversationId = conversationId;
        Rating = rating;
        CreatedAt = createdAt;
    }

    public void SetDetailedRatings(int? professionalism, int? communication, int? knowledge, int? responseTime)
    {
        if (professionalism.HasValue) ValidateRating(professionalism.Value, "Professionalism rating");
        if (communication.HasValue) ValidateRating(communication.Value, "Communication rating");
        if (knowledge.HasValue) ValidateRating(knowledge.Value, "Knowledge rating");
        if (responseTime.HasValue) ValidateRating(responseTime.Value, "Response time rating");

        ProfessionalismRating = professionalism;
        CommunicationRating = communication;
        KnowledgeRating = knowledge;
        ResponseTimeRating = responseTime;
    }
}
