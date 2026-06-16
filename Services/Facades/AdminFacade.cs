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
    private readonly UserSuspensionService _suspensionService;
    private readonly AiAssistantSettingsService _aiAssistantSettingsService;
    private readonly AiAssistantService _aiAssistantService;

    public AdminFacade(
        UserService userService,
        DoctorProfileService doctorProfileService,
        PatientProfileService patientProfileService,
        StatisticsService statisticsService,
        SupportTicketService supportTicketService,
        ActivityLogService activityLogService,
        UserSuspensionService suspensionService,
        AiAssistantSettingsService aiAssistantSettingsService,
        AiAssistantService aiAssistantService)
    {
        _userService = userService;
        _doctorProfileService = doctorProfileService;
        _patientProfileService = patientProfileService;
        _statisticsService = statisticsService;
        _supportTicketService = supportTicketService;
        _activityLogService = activityLogService;
        _suspensionService = suspensionService;
        _aiAssistantSettingsService = aiAssistantSettingsService;
        _aiAssistantService = aiAssistantService;
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
            doctor.IsActive = isVerified;
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
        string newStatus = "in_progress")
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

    /// <summary>
    /// Get all users with optional filtering
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<List<User>> GetAllUsersAsync()
    {
        return await _userService.GetAllUsersAsync();
    }

    /// <summary>
    /// Create a user account and matching role profile.
    /// </summary>
    public async Task<User> CreateUserAsync(
        Guid adminId,
        string email,
        string password,
        UserRole role,
        string? fullName = null)
    {
        var existing = await _userService.GetByEmailAsync(email);
        if (existing != null)
        {
            throw new InvalidOperationException("A user with this email already exists.");
        }

        var user = await _userService.CreateAsync(new User
        {
            Email = email,
            Role = role,
            IsActive = true,
            IsDeactivated = false
        }, password);

        if (role == UserRole.Patient)
        {
            await _patientProfileService.CreateAsync(new PatientProfile
            {
                UserId = user.Id,
                FullName = fullName
            });
        }
        else if (role == UserRole.Doctor)
        {
            await _doctorProfileService.CreateAsync(new DoctorProfile
            {
                UserId = user.Id,
                FullName = fullName ?? string.Empty,
                LicenseNumber = $"PENDING-{user.Id.ToString()[..8]}",
                PrimarySpecialization = "General Medicine",
                IsActive = true,
                IsVerified = false,
                AvailabilityStatus = DoctorAvailabilityStatus.Offline
            });
        }
        else if (role == UserRole.Admin)
        {
            using var db = ai_clinic.Data.DbClient.Instance.GetDb();
            db.AdminProfiles.Add(new AdminProfile
            {
                UserId = user.Id,
                FullName = fullName ?? email,
                ManageUsers = true,
                ManageDoctors = true,
                ManageTickets = true
            });
            await db.SaveChangesAsync();
        }

        await _activityLogService.LogActivityAsync(
            adminId,
            "CreateUser",
            $"Created {role} account: {email}");

        return user;
    }

    /// <summary>
    /// Update account fields and the role-specific display name.
    /// </summary>
    public async Task<User> UpdateUserAccountAsync(
        Guid adminId,
        Guid userId,
        string email,
        string? fullName,
        bool isActive)
    {
        var user = await _userService.GetByIdAsync(userId)
            ?? throw new InvalidOperationException("User not found.");

        user.Email = email;
        user.IsActive = isActive;
        if (isActive)
        {
            user.IsDeactivated = false;
            user.DeactivatedAt = null;
        }

        if (user.PatientProfile != null)
        {
            user.PatientProfile.FullName = fullName;
            await _patientProfileService.UpdateAsync(user.PatientProfile);
        }
        else if (user.DoctorProfile != null)
        {
            user.DoctorProfile.FullName = fullName ?? string.Empty;
            await _doctorProfileService.UpdateAsync(user.DoctorProfile);
        }

        await _userService.UpdateAsync(user);

        await _activityLogService.LogActivityAsync(
            adminId,
            "UpdateUser",
            $"Updated user: {userId}");

        return user;
    }

    /// <summary>
    /// Get doctors for verification
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<List<DoctorProfile>> GetDoctorsForVerificationAsync()
    {
        return await _doctorProfileService.GetAllAsync();
    }

    /// <summary>
    /// Update doctor profile and availability from the admin panel.
    /// </summary>
    public async Task UpdateDoctorProfileAsync(
        Guid adminId,
        Guid doctorUserId,
        string fullName,
        string licenseNumber,
        string primarySpecialization,
        int? yearsOfExperience,
        DoctorAvailabilityStatus availabilityStatus,
        bool isAcceptingPatients,
        bool isActive)
    {
        var doctor = await _doctorProfileService.GetByUserIdAsync(doctorUserId)
            ?? throw new InvalidOperationException("Doctor profile not found.");

        doctor.FullName = fullName;
        doctor.LicenseNumber = licenseNumber;
        doctor.PrimarySpecialization = primarySpecialization;
        doctor.YearsOfExperience = yearsOfExperience;
        doctor.AvailabilityStatus = availabilityStatus;
        doctor.IsAcceptingPatients = isAcceptingPatients;
        doctor.IsActive = isActive;

        await _doctorProfileService.UpdateAsync(doctor);

        await _activityLogService.LogActivityAsync(
            adminId,
            "UpdateDoctorProfile",
            $"Doctor ID: {doctorUserId}");
    }

    /// <summary>
    /// Get activity logs with optional filtering
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<List<ActivityLog>> GetActivityLogsAsync(string? actionFilter = null, int limit = 100)
    {
        if (string.IsNullOrEmpty(actionFilter))
        {
            return await _activityLogService.GetRecentLogsAsync(limit);
        }
        else
        {
            return await _activityLogService.GetByActionAsync(actionFilter, limit);
        }
    }

    /// <summary>
    /// Get support tickets with optional status filtering
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<List<SupportTicket>> GetSupportTicketsAsync(string? statusFilter = null)
    {
        if (string.IsNullOrEmpty(statusFilter))
        {
            return await _supportTicketService.GetAllAsync();
        }
        else
        {
            return await _supportTicketService.GetByStatusAsync(statusFilter);
        }
    }

    /// <summary>
    /// Update support ticket status
    /// Simplified interface for UI layer
    /// </summary>
    public async Task UpdateSupportTicketStatusAsync(Guid ticketId, string status, Guid adminId)
    {
        await _supportTicketService.UpdateStatusAsync(ticketId, status);
        
        await _activityLogService.LogActivityAsync(
            adminId,
            "UpdateTicketStatus",
            $"Ticket ID: {ticketId}, Status: {status}");
    }

    /// <summary>
    /// Unsuspend a user account
    /// Reactivates user and logs the action
    /// </summary>
    public async Task UnsuspendUserAsync(Guid userId, Guid adminId)
    {
        var lifted = await _suspensionService.LiftSuspensionAsync(userId, adminId);
        if (!lifted)
        {
            await _userService.ActivateAsync(userId);
        }

        await _activityLogService.LogActivityAsync(
            adminId,
            "UnsuspendUser",
            $"User ID: {userId}");
    }

    /// <summary>
    /// Delete a user account
    /// Permanently removes user and logs the action
    /// </summary>
    public async Task DeleteUserAsync(Guid userId, Guid adminId)
    {
        await _userService.DeleteAsync(userId);

        await _activityLogService.LogActivityAsync(
            adminId,
            "DeleteUser",
            $"User ID: {userId}");
    }

    /// <summary>
    /// Get dashboard statistics
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<AdminDashboardStats> GetDashboardStatsAsync()
    {
        var userCounts = await _statisticsService.GetUserCountByRoleAsync();
        var conversationStats = await _statisticsService.GetConversationStatsAsync();
        var allDoctors = await _doctorProfileService.GetAllAsync();
        var allTickets = await _supportTicketService.GetAllAsync();

        return new AdminDashboardStats
        {
            TotalUsers = userCounts.Values.Sum(),
            TotalDoctors = userCounts.GetValueOrDefault(UserRole.Doctor, 0),
            TotalPatients = userCounts.GetValueOrDefault(UserRole.Patient, 0),
            VerifiedDoctors = allDoctors.Count(d => d.IsVerified),
            PendingVerifications = allDoctors.Count(d => !d.IsVerified),
            ActiveConversations = conversationStats.ActiveConversations,
            TotalConversations = conversationStats.TotalConversations,
            OpenSupportTickets = allTickets.Count(t => t.Status == "open" || t.Status == "in_progress"),
            TotalSupportTickets = allTickets.Count,
            RecentRegistrations = 0 // TODO: Calculate recent registrations
        };
    }

    /// <summary>
    /// Get paginated users with filtering
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<PaginatedResult<User>> GetAllUsersAsync(
        int page = 1,
        int pageSize = 20,
        UserRole? roleFilter = null,
        string? searchTerm = null,
        bool? isActiveFilter = null)
    {
        var allUsers = await _userService.GetAllUsersAsync();

        // Apply filters
        var filtered = allUsers.AsEnumerable();

        if (roleFilter.HasValue)
        {
            filtered = filtered.Where(u => u.Role == roleFilter.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            filtered = filtered.Where(u => 
                u.Email.ToLower().Contains(term));
        }

        if (isActiveFilter.HasValue)
        {
            filtered = filtered.Where(u => u.IsActive == isActiveFilter.Value);
        }

        var totalCount = filtered.Count();
        var items = filtered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PaginatedResult<User>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    /// <summary>
    /// Suspend or unsuspend a user
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<bool> SuspendUserAsync(Guid userId, Guid adminId, bool suspend, string reason)
    {
        if (suspend)
        {
            if (!await _suspensionService.IsUserSuspendedAsync(userId))
            {
                await _suspensionService.SuspendUserAsync(userId, adminId, reason, DateTime.UtcNow.AddDays(7));
            }
        }
        else
        {
            await UnsuspendUserAsync(userId, adminId);
        }
        return true;
    }

    /// <summary>
    /// Delete user with reason
    /// Simplified interface for UI layer
    /// </summary>
    public async Task<bool> DeleteUserAsync(Guid userId, Guid adminId, string reason)
    {
        await DeleteUserAsync(userId, adminId);
        
        await _activityLogService.LogActivityAsync(
            adminId,
            "DeleteUser",
            $"User ID: {userId}, Reason: {reason}");
        
        return true;
    }

    #region User Suspension Management

    /// <summary>
    /// Suspend a user account with detailed suspension record
    /// Creates suspension record and deactivates user
    /// </summary>
    public async Task<UserSuspension> SuspendUserWithDetailsAsync(
        Guid userId,
        Guid adminId,
        string reason,
        DateTime? suspensionEnd = null)
    {
        return await _suspensionService.SuspendUserAsync(userId, adminId, reason, suspensionEnd);
    }

    /// <summary>
    /// Lift (remove) a user suspension
    /// Reactivates user and marks suspension as inactive
    /// </summary>
    public async Task<bool> LiftUserSuspensionAsync(Guid userId, Guid adminId)
    {
        return await _suspensionService.LiftSuspensionAsync(userId, adminId);
    }

    /// <summary>
    /// Get active suspension for a user
    /// Returns current suspension details if user is suspended
    /// </summary>
    public async Task<UserSuspension?> GetActiveSuspensionAsync(Guid userId)
    {
        return await _suspensionService.GetActiveSuspensionAsync(userId);
    }

    /// <summary>
    /// Get suspension history for a user
    /// Returns all past and current suspensions
    /// </summary>
    public async Task<List<UserSuspension>> GetUserSuspensionHistoryAsync(Guid userId)
    {
        return await _suspensionService.GetUserSuspensionHistoryAsync(userId);
    }

    /// <summary>
    /// Check if a user is currently suspended
    /// Auto-lifts expired suspensions
    /// </summary>
    public async Task<bool> IsUserSuspendedAsync(Guid userId)
    {
        return await _suspensionService.IsUserSuspendedAsync(userId);
    }

    /// <summary>
    /// Get all currently suspended users
    /// For admin dashboard monitoring
    /// </summary>
    public async Task<List<UserSuspension>> GetAllActiveSuspensionsAsync()
    {
        return await _suspensionService.GetAllActiveSuspensionsAsync();
    }

    /// <summary>
    /// Extend an existing suspension
    /// Updates suspension end date
    /// </summary>
    public async Task<bool> ExtendSuspensionAsync(Guid userId, DateTime newEndDate, Guid adminId)
    {
        return await _suspensionService.ExtendSuspensionAsync(userId, newEndDate, adminId);
    }

    #endregion

    #region AI Assistant Management

    /// <summary>
    /// Get all AI assistant settings
    /// Returns list of configured AI models and their settings
    /// </summary>
    public async Task<List<AiAssistantSetting>> GetAllAiSettingsAsync(Guid adminId)
    {
        await _activityLogService.LogActivityAsync(
            adminId,
            "ViewAiSettings",
            "Viewed AI assistant settings");

        return await _aiAssistantSettingsService.GetAllAsync();
    }

    /// <summary>
    /// Get the currently active AI assistant setting
    /// Returns the model configuration currently in use
    /// </summary>
    public async Task<AiAssistantSetting?> GetActiveAiSettingAsync()
    {
        return await _aiAssistantSettingsService.GetActiveSettingAsync();
    }

    /// <summary>
    /// Create a new AI assistant setting
    /// Validates and logs the creation action
    /// </summary>
    public async Task<AiAssistantSetting> CreateAiSettingAsync(
        AiAssistantSetting setting,
        Guid adminId)
    {
        setting.CreatedByAdminId = adminId;
        var created = await _aiAssistantSettingsService.CreateAsync(setting);

        await _activityLogService.LogActivityAsync(
            adminId,
            "CreateAiSetting",
            $"Created AI setting: {setting.ModelName}, Active: {setting.IsActive}");

        return created;
    }

    /// <summary>
    /// Update an existing AI assistant setting
    /// Validates and logs the update action
    /// </summary>
    public async Task<AiAssistantSetting?> UpdateAiSettingAsync(
        AiAssistantSetting setting,
        Guid adminId)
    {
        var updated = await _aiAssistantSettingsService.UpdateAsync(setting);

        if (updated != null)
        {
            await _activityLogService.LogActivityAsync(
                adminId,
                "UpdateAiSetting",
                $"Updated AI setting ID: {setting.Id}, Model: {setting.ModelName}");
        }

        return updated;
    }

    /// <summary>
    /// Delete an AI assistant setting
    /// Prevents deletion of active settings and logs the action
    /// </summary>
    public async Task<bool> DeleteAiSettingAsync(Guid settingId, Guid adminId)
    {
        try
        {
            var deleted = await _aiAssistantSettingsService.DeleteAsync(settingId);

            if (deleted)
            {
                await _activityLogService.LogActivityAsync(
                    adminId,
                    "DeleteAiSetting",
                    $"Deleted AI setting ID: {settingId}");
            }

            return deleted;
        }
        catch (InvalidOperationException ex)
        {
            // Log the attempted deletion of active setting
            await _activityLogService.LogActivityAsync(
                adminId,
                "DeleteAiSettingFailed",
                $"Failed to delete AI setting ID: {settingId}, Reason: {ex.Message}");
            
            throw;
        }
    }

    /// <summary>
    /// Activate a specific AI assistant setting
    /// Deactivates all others and syncs with AI service immediately
    /// </summary>
    public async Task<bool> ActivateAiSettingAsync(Guid settingId, Guid adminId)
    {
        var activated = await _aiAssistantSettingsService.ActivateSettingAsync(settingId);

        if (activated)
        {
            await _activityLogService.LogActivityAsync(
                adminId,
                "ActivateAiSetting",
                $"Activated AI setting ID: {settingId}");
            
            // Sync the AI service with the new settings immediately
            await _aiAssistantService.SyncWithAdminSettingsAsync();
        }

        return activated;
    }

    /// <summary>
    /// Helper method to get AI service instance
    /// </summary>
    private async Task<AiAssistantService?> GetAiAssistantServiceAsync()
    {
        try
        {
            // The AI service is injected elsewhere, but we can trigger sync through a static reference
            // For now, return null - the sync will happen on next request
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Get AI settings statistics
    /// Returns overview of AI configuration status
    /// </summary>
    public async Task<AiSettingsStats> GetAiSettingsStatsAsync()
    {
        return await _aiAssistantSettingsService.GetStatsAsync();
    }

    #endregion

    #region Reports & Analytics

    /// <summary>
    /// Get user count by role for reports
    /// Returns distribution of users across roles
    /// </summary>
    public async Task<Dictionary<UserRole, int>> GetUserCountByRoleAsync()
    {
        return await _statisticsService.GetUserCountByRoleAsync();
    }

    /// <summary>
    /// Get conversation statistics for reports
    /// Returns comprehensive consultation metrics
    /// </summary>
    public async Task<ConversationStats> GetConversationStatsAsync()
    {
        return await _statisticsService.GetConversationStatsAsync();
    }

    #endregion
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

public class AdminDashboardStats
{
    public int TotalUsers { get; set; }
    public int TotalDoctors { get; set; }
    public int VerifiedDoctors { get; set; }
    public int PendingVerifications { get; set; }
    public int TotalPatients { get; set; }
    public int ActiveConversations { get; set; }
    public int TotalConversations { get; set; }
    public int OpenSupportTickets { get; set; }
    public int TotalSupportTickets { get; set; }
    public int RecentRegistrations { get; set; }
}

public class PaginatedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
