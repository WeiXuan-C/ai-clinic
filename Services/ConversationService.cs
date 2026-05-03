using ai_clinic.Data;
using ai_clinic.Models;
using Microsoft.EntityFrameworkCore;

namespace ai_clinic.Services;

/// <summary>
/// Service for managing Conversations between patients and doctors
/// </summary>
public class ConversationService
{
    /// <summary>
    /// Get conversation by ID with all related data
    /// </summary>
    public async Task<Conversation?> GetByIdAsync(Guid conversationId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Conversations
            .Include(c => c.Patient)
            .Include(c => c.AssignedDoctor)
            .Include(c => c.Messages)
            .Include(c => c.Documents)
            .FirstOrDefaultAsync(c => c.Id == conversationId);
    }

    /// <summary>
    /// Get all conversations for a patient with last message preview
    /// </summary>
    public async Task<List<ConversationListItem>> GetConversationListByPatientIdAsync(Guid patientId)
    {
        using var db = DbClient.Instance.GetDb();
        
        var conversations = await db.Conversations
            .Include(c => c.AssignedDoctor)
                .ThenInclude(d => d!.DoctorProfile)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

        return conversations.Select(c => new ConversationListItem
        {
            Id = c.Id,
            Title = c.Title ?? "New Consultation",
            LastMessageAt = c.LastMessageAt,
            Status = c.Status,
            IsAiConversation = c.AssignedDoctorId == null,
            DoctorName = c.AssignedDoctor?.DoctorProfile?.FullName,
            UnreadCount = c.Messages.Count(m => !m.IsRead && m.SenderType != MessageSenderType.Patient),
            LastMessagePreview = c.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.Content
        }).ToList();
    }

    /// <summary>
    /// Get all conversations for a patient
    /// </summary>
    public async Task<List<Conversation>> GetByPatientIdAsync(Guid patientId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Conversations
            .Include(c => c.AssignedDoctor)
            .Where(c => c.PatientId == patientId)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get all conversations for a doctor
    /// </summary>
    public async Task<List<Conversation>> GetByDoctorIdAsync(Guid doctorId)
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Conversations
            .Include(c => c.Patient)
            .Where(c => c.AssignedDoctorId == doctorId)
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();
    }

    /// <summary>
    /// Create a new conversation with AI assistant
    /// </summary>
    public async Task<Conversation> CreateAiConversationAsync(Guid patientId, string? initialMessage = null)
    {
        var conversation = new Conversation
        {
            PatientId = patientId,
            AssignedDoctorId = null, // AI conversation
            Title = "AI Consultation",
            Status = ConversationStatus.Active,
            StartedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        using var db = DbClient.Instance.GetDb();
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();

        // Add initial message if provided
        if (!string.IsNullOrWhiteSpace(initialMessage))
        {
            var message = new Message
            {
                ConversationId = conversation.Id,
                SenderId = patientId,
                SenderType = MessageSenderType.Patient,
                Content = initialMessage,
                CreatedAt = DateTime.UtcNow
            };
            db.Messages.Add(message);
            await db.SaveChangesAsync();
        }

        return conversation;
    }

    /// <summary>
    /// Create a new conversation with a specific doctor
    /// </summary>
    public async Task<Conversation> CreateDoctorConversationAsync(Guid patientId, Guid doctorId, string? initialMessage = null)
    {
        using var db = DbClient.Instance.GetDb();
        
        // Verify doctor exists and is active
        var doctor = await db.DoctorProfiles
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.UserId == doctorId && d.IsActive);
        
        if (doctor == null)
        {
            throw new InvalidOperationException("Doctor not found or inactive");
        }

        var conversation = new Conversation
        {
            PatientId = patientId,
            AssignedDoctorId = doctorId,
            Title = $"Consultation with Dr. {doctor.FullName}",
            Status = ConversationStatus.Active,
            StartedAt = DateTime.UtcNow,
            LastMessageAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();

        // Add initial message if provided
        if (!string.IsNullOrWhiteSpace(initialMessage))
        {
            var message = new Message
            {
                ConversationId = conversation.Id,
                SenderId = patientId,
                SenderType = MessageSenderType.Patient,
                Content = initialMessage,
                CreatedAt = DateTime.UtcNow
            };
            db.Messages.Add(message);
            await db.SaveChangesAsync();
        }

        return conversation;
    }

    /// <summary>
    /// Create a new conversation
    /// </summary>
    public async Task<Conversation> CreateAsync(Conversation conversation)
    {
        conversation.CreatedAt = DateTime.UtcNow;
        conversation.UpdatedAt = DateTime.UtcNow;

        using var db = DbClient.Instance.GetDb();
        db.Conversations.Add(conversation);
        await db.SaveChangesAsync();
        return conversation;
    }

    /// <summary>
    /// Assign a doctor to a conversation
    /// </summary>
    public async Task AssignDoctorAsync(Guid conversationId, Guid doctorId)
    {
        using var db = DbClient.Instance.GetDb();
        var conversation = await db.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.AssignedDoctorId = doctorId;
            conversation.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Update conversation status
    /// </summary>
    public async Task UpdateStatusAsync(Guid conversationId, ConversationStatus status)
    {
        using var db = DbClient.Instance.GetDb();
        var conversation = await db.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.Status = status;
            if (status == ConversationStatus.Closed)
            {
                conversation.ClosedAt = DateTime.UtcNow;
            }
            conversation.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Update conversation title
    /// </summary>
    public async Task UpdateTitleAsync(Guid conversationId, string title)
    {
        using var db = DbClient.Instance.GetDb();
        var conversation = await db.Conversations.FindAsync(conversationId);
        if (conversation != null)
        {
            conversation.Title = title;
            conversation.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Get active conversations (not closed or completed)
    /// </summary>
    public async Task<List<Conversation>> GetActiveConversationsAsync()
    {
        using var db = DbClient.Instance.GetDb();
        return await db.Conversations
            .Include(c => c.Patient)
            .Include(c => c.AssignedDoctor)
            .Where(c => c.Status == ConversationStatus.Active)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Get available doctors for consultation
    /// </summary>
    public async Task<List<DoctorListItem>> GetAvailableDoctorsAsync()
    {
        using var db = DbClient.Instance.GetDb();
        
        // SQLite 不支持 decimal 类型的 ORDER BY，所以先获取数据再在内存中排序
        var doctors = await db.DoctorProfiles
            .Include(d => d.User)
            .Where(d => d.IsActive && d.IsAcceptingPatients)
            .ToListAsync();

        // 在内存中按评分排序
        return doctors
            .OrderByDescending(d => d.AverageRating)
            .Select(d => new DoctorListItem
            {
                UserId = d.UserId,
                FullName = d.FullName,
                PrimarySpecialization = d.PrimarySpecialization,
                YearsOfExperience = d.YearsOfExperience,
                AverageRating = d.AverageRating,
                TotalRatings = d.TotalRatings,
                AvailabilityStatus = d.AvailabilityStatus,
                ProfilePhotoUrl = d.ProfilePhotoUrl
            }).ToList();
    }
}

/// <summary>
/// DTO for conversation list display
/// </summary>
public class ConversationListItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime LastMessageAt { get; set; }
    public ConversationStatus Status { get; set; }
    public bool IsAiConversation { get; set; }
    public string? DoctorName { get; set; }
    public int UnreadCount { get; set; }
    public string? LastMessagePreview { get; set; }
}

/// <summary>
/// DTO for doctor list display
/// </summary>
public class DoctorListItem
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PrimarySpecialization { get; set; } = string.Empty;
    public int? YearsOfExperience { get; set; }
    public decimal AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public DoctorAvailabilityStatus AvailabilityStatus { get; set; }
    public string? ProfilePhotoUrl { get; set; }
}
