using Microsoft.EntityFrameworkCore;
using ai_clinic.Models;

namespace ai_clinic.Data;

/// <summary>
/// SINGLETON PATTERN - Database Singleton
/// Global unique database access entry point
/// </summary>
public sealed class DbClient
{
    // Private static instance - only created once
    private static readonly Lazy<DbClient> _instance = new Lazy<DbClient>(() => new DbClient());

    // Database connection string
    private readonly string _connectionString;

    // Private constructor - prevents external new DbClient()
    private DbClient()
    {
        _connectionString = "Data Source=ai-clinic.db";
    }

    /// <summary>
    /// Gets singleton instance - global unique entry point
    /// Usage: using var db = DbClient.Instance.GetDb();
    /// </summary>
    public static DbClient Instance => _instance.Value;

    /// <summary>
    /// Gets database context
    /// Usage: using var db = DbContext.Instance.GetDb();
    /// </summary>
    public AiClinicDbContext GetDb()
    {
        var options = new DbContextOptionsBuilder<AiClinicDbContext>()
            .UseSqlite(_connectionString)
            .Options;

        return new AiClinicDbContext(options);
    }
}

/// <summary>
/// Entity Framework DbContext - 数据库操作类
/// </summary>
public class AiClinicDbContext : Microsoft.EntityFrameworkCore.DbContext
{
    public AiClinicDbContext(DbContextOptions<AiClinicDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }
    public DbSet<PatientProfile> PatientProfiles { get; set; }
    public DbSet<DoctorProfile> DoctorProfiles { get; set; }
    public DbSet<AdminProfile> AdminProfiles { get; set; }
    public DbSet<Conversation> Conversations { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<Document> Documents { get; set; }
    public DbSet<DoctorRating> DoctorRatings { get; set; }
    public DbSet<ActivityLog> ActivityLogs { get; set; }
    public DbSet<SupportTicket> SupportTickets { get; set; }
    public DbSet<SupportTicketAttachment> SupportTicketAttachments { get; set; }
    public DbSet<SupportTicketResponse> SupportTicketResponses { get; set; }
    public DbSet<MedicalRecord> MedicalRecords { get; set; }
    public DbSet<ConsultationNote> ConsultationNotes { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }
    public DbSet<AiAssistantSetting> AiAssistantSettings { get; set; }
    public DbSet<UserSuspension> UserSuspensions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User entity configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Role).HasConversion<string>();
        });

        // PatientProfile - One-to-One with User
        modelBuilder.Entity<PatientProfile>(entity =>
        {
            entity.HasOne(p => p.User)
                .WithOne(u => u.PatientProfile)
                .HasForeignKey<PatientProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DoctorProfile - One-to-One with User
        modelBuilder.Entity<DoctorProfile>(entity =>
        {
            entity.HasOne(d => d.User)
                .WithOne(u => u.DoctorProfile)
                .HasForeignKey<DoctorProfile>(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.LicenseNumber).IsUnique();
            entity.Property(e => e.AvailabilityStatus).HasConversion<string>();
            entity.Property(e => e.AverageRating).HasPrecision(3, 2);
        });

        // AdminProfile - One-to-One with User
        modelBuilder.Entity<AdminProfile>(entity =>
        {
            entity.HasOne(a => a.User)
                .WithOne(u => u.AdminProfile)
                .HasForeignKey<AdminProfile>(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Conversation entity configuration
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.AiConfidenceScore).HasPrecision(5, 4);

            entity.HasOne(c => c.Patient)
                .WithMany(u => u.PatientConversations)
                .HasForeignKey(c => c.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(c => c.AssignedDoctor)
                .WithMany(u => u.DoctorConversations)
                .HasForeignKey(c => c.AssignedDoctorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Message entity configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.Property(e => e.SenderType).HasConversion<string>();
            entity.Property(e => e.AiConfidenceScore).HasPrecision(5, 4);

            entity.HasOne(m => m.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Document entity configuration
        modelBuilder.Entity<Document>(entity =>
        {
            entity.Property(e => e.FileType).HasConversion<string>();

            entity.HasOne(d => d.Conversation)
                .WithMany(c => c.Documents)
                .HasForeignKey(d => d.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(d => d.UploadedByUser)
                .WithMany(u => u.Documents)
                .HasForeignKey(d => d.UploadedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // DoctorRating entity configuration
        modelBuilder.Entity<DoctorRating>(entity =>
        {
            entity.HasIndex(e => new { e.ConversationId, e.PatientId }).IsUnique();

            entity.HasOne(dr => dr.Doctor)
                .WithMany()
                .HasForeignKey(dr => dr.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(dr => dr.Patient)
                .WithMany()
                .HasForeignKey(dr => dr.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(dr => dr.Conversation)
                .WithMany(c => c.Ratings)
                .HasForeignKey(dr => dr.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ActivityLog entity configuration
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasOne(al => al.User)
                .WithMany()
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // SupportTicket entity configuration
        modelBuilder.Entity<SupportTicket>(entity =>
        {
            entity.HasOne(st => st.User)
                .WithMany()
                .HasForeignKey(st => st.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SupportTicketAttachment entity configuration
        modelBuilder.Entity<SupportTicketAttachment>(entity =>
        {
            entity.HasOne(sta => sta.Ticket)
                .WithMany(st => st.Attachments)
                .HasForeignKey(sta => sta.TicketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // SupportTicketResponse entity configuration
        modelBuilder.Entity<SupportTicketResponse>(entity =>
        {
            entity.HasOne(str => str.Ticket)
                .WithMany(st => st.Responses)
                .HasForeignKey(str => str.TicketId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(str => str.Responder)
                .WithMany()
                .HasForeignKey(str => str.ResponderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MedicalRecord entity configuration
        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasOne(mr => mr.Patient)
                .WithMany()
                .HasForeignKey(mr => mr.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(mr => mr.Conversation)
                .WithMany()
                .HasForeignKey(mr => mr.ConversationId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(mr => mr.CreatedByDoctor)
                .WithMany()
                .HasForeignKey(mr => mr.CreatedByDoctorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ConsultationNote entity configuration
        modelBuilder.Entity<ConsultationNote>(entity =>
        {
            entity.HasOne(cn => cn.Conversation)
                .WithMany()
                .HasForeignKey(cn => cn.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cn => cn.Doctor)
                .WithMany()
                .HasForeignKey(cn => cn.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cn => cn.Patient)
                .WithMany()
                .HasForeignKey(cn => cn.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cn => cn.Prescription)
                .WithMany()
                .HasForeignKey(cn => cn.PrescriptionId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Prescription entity configuration
        modelBuilder.Entity<Prescription>(entity =>
        {
            entity.HasOne(p => p.ConsultationNote)
                .WithMany(cn => cn.Prescriptions)
                .HasForeignKey(p => p.ConsultationNoteId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Patient)
                .WithMany()
                .HasForeignKey(p => p.PatientId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Doctor)
                .WithMany()
                .HasForeignKey(p => p.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AiAssistantSetting entity configuration
        modelBuilder.Entity<AiAssistantSetting>(entity =>
        {
            entity.HasOne(aas => aas.CreatedByAdmin)
                .WithMany()
                .HasForeignKey(aas => aas.CreatedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // UserSuspension entity configuration
        modelBuilder.Entity<UserSuspension>(entity =>
        {
            entity.HasOne(us => us.User)
                .WithMany()
                .HasForeignKey(us => us.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(us => us.SuspendedByAdmin)
                .WithMany()
                .HasForeignKey(us => us.SuspendedByAdminId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(us => us.LiftedByAdmin)
                .WithMany()
                .HasForeignKey(us => us.LiftedByAdminId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
