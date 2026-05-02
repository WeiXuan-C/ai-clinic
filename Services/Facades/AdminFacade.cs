using ai_clinic.Models;

namespace ai_clinic.Services.Facades;

/// <summary>
/// Facade Pattern: Provides a unified interface for admin-related operations
/// Coordinates multiple subsystems: User management, Statistics, Support, Activity logs
/// </summary>
public class AdminFacade
{
    private readonly UserService _userService;
    private readonly DoctorProfileService _doctorProfileService;
    private readonly PatientProfileService _patientProfileService;
    private readonly StatisticsService _statisticsService;
    private readonly SupportTicketService _supportTicketService;
    private readonly ActivityLogService _activityLogService;

    public AdminFacade(
        UserService userService,
        DoctorProfileService doctorProfileService,
        PatientProfileService patientProfileService,
        StatisticsService statisticsService,
        SupportTicketService supportTicketService,
        ActivityLogService activityLogService)
    {
        _userService = userService;
        _doctorProfileService = doctorProfileService;
        _patientProfileService = patientProfileService;
        _statisticsService = statisticsService;
        _supportTicketService = supportTicketService;
        _activityLogService = activityLogService;
    }

    /// <summary>
    /// Get complete admin dashboard data
    /// Coordinates multiple services to gather system-wide statistics
    /// </summary>
    public async Task<AdminDashboardData> GetDashboardDataAsync(Guid adminId)
    {
        var userCountsTask = _statisticsService.GetUserCountByRoleAsync();
        var conversationStatsTask = _statisticsService.GetConversationStatsAsync();
        var openTicketsTask = _supportTicketService.GetOpenTicketsAsync();
        var recentLogsTask = _activityLogService.GetRecentLogsAsync(20);

        await Task.WhenAll(userCountsTask, conversationStatsTask, openTicketsTask, recentLogsTask);

        await _activityLogService.LogActivityAsync(adminId, "ViewAdminDashboard");

        return new AdminDashboardData
        {
            UserCounts = await userCountsTask,
            ConversationStats = await conversationStatsTask,
            OpenSupportTickets = await openTicketsTask,
            RecentActivityLogs = await recentLogsTask
        };
    }

    /// <summary>
    /// Verify a doctor's credentials
    /// Updates doctor profile and logs the action
    /// </summary>
    public async Task VerifyDoctorAsync(Guid doctorUserId, Guid adminId, bool isVerified)
    {
        var doctor = await _doctorProfileService.GetByUserIdAsync(doctorUserId);
        if (doctor != null)
        {
            doctor.IsVerified = isVerified;
            await _doctorProfileService.UpdateAsync(doctor);

            await _activityLogService.LogActivityAsync(
                adminId,
                "VerifyDoctor",
                $"Doctor ID: {doctorUserId}, Verified: {isVerified}");
        }
    }

    /// <summary>
    /// Suspend a user account
    /// Deactivates user and logs the action
    /// </summary>
    public async Task SuspendUserAsync(Guid userId, Guid adminId, string reason)
    {
        await _userService.DeactivateAsync(userId);

        await _activityLogService.LogActivityAsync(
            adminId,
            "SuspendUser",
            $"User ID: {userId}, Reason: {reason}");
    }

    /// <summary>
    /// Get system health overview
    /// Provides comprehensive system statistics
    /// </summary>
    public async Task<SystemHealthOverview> GetSystemHealthAsync()
    {
        var userCounts = await _statisticsService.GetUserCountByRoleAsync();
        var conversationStats = await _statisticsService.GetConversationStatsAsync();
        var openTickets = await _supportTicketService.GetOpenTicketsAsync();

        var totalUsers = userCounts.Values.Sum();
        var doctorCount = userCounts.GetValueOrDefault(UserRole.Doctor, 0);
        var patientCount = userCounts.GetValueOrDefault(UserRole.Patient, 0);

        return new SystemHealthOverview
        {
            TotalUsers = totalUsers,
            TotalDoctors = doctorCount,
            TotalPatients = patientCount,
            ActiveConversations = conversationStats.ActiveConversations,
            TotalConversations = conversationStats.TotalConversations,
            OpenSupportTickets = openTickets.Count,
            SystemStatus = "Healthy"
        };
    }

    /// <summary>
    /// Manage support ticket
    /// Responds to ticket and updates status
    /// </summary>
    public async Task RespondToTicketAsync(
        Guid ticketId, 
        Guid adminId, 
        string responseMessage,
        string newStatus = "In Progress")
    {
        var response = new SupportTicketResponse
        {
            TicketId = ticketId,
            ResponderId = adminId,
            Message = responseMessage
        };

        await _supportTicketService.AddResponseAsync(response);
        await _supportTicketService.UpdateStatusAsync(ticketId, newStatus);

        await _activityLogService.LogActivityAsync(
            adminId,
            "RespondToTicket",
            $"Ticket ID: {ticketId}");
    }
}

// DTOs for Facade responses
public class AdminDashboardData
{
    public Dictionary<UserRole, int> UserCounts { get; set; } = new();
    public ConversationStats ConversationStats { get; set; } = new();
    public List<SupportTicket> OpenSupportTickets { get; set; } = new();
    public List<ActivityLog> RecentActivityLogs { get; set; } = new();
}

public class SystemHealthOverview
{
    public int TotalUsers { get; set; }
    public int TotalDoctors { get; set; }
    public int TotalPatients { get; set; }
    public int ActiveConversations { get; set; }
    public int TotalConversations { get; set; }
    public int OpenSupportTickets { get; set; }
    public string SystemStatus { get; set; } = "Unknown";
}
