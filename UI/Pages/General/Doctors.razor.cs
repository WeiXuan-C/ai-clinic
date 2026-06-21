using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using ai_clinic.Services.Facades;
using ai_clinic.Models;

namespace ai_clinic.UI.Pages.General;

/// <summary>
/// General doctors directory page for browsing available doctors
/// Accessible to both anonymous and authenticated users
/// </summary>
public partial class Doctors : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private DoctorFacade DoctorFacade { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private List<DoctorCardInfo> doctors = new();
    private List<DoctorCardInfo> filteredDoctors = new();
    private bool isLoading = true;
    private string searchQuery = "";
    private string selectedSpecialization = "All";
    private string selectedAvailability = "All";
    private List<string> specializations = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadDoctors();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await JS.InvokeVoidAsync("initializeLucide");
            }
            catch
            {
                // Ignore if lucide is not available
            }
        }
    }

    /// <summary>
    /// Loads all available doctors from the database
    /// </summary>
    private async Task LoadDoctors()
    {
        isLoading = true;
        StateHasChanged();

        try
        {
            var doctorProfiles = await DoctorFacade.GetAllActiveDoctorsAsync();
            
            doctors = doctorProfiles
                .Where(d => d.IsActive) // Only show active doctors
                .Select(d => new DoctorCardInfo
                {
                    UserId = d.UserId,
                    FullName = d.FullName,
                    Title = d.Title ?? "Dr.",
                    Specialization = d.PrimarySpecialization,
                    YearsOfExperience = d.YearsOfExperience ?? 0,
                    AverageRating = d.AverageRating,
                    TotalRatings = d.TotalRatings,
                    AvailabilityStatus = d.AvailabilityStatus,
                    IsAcceptingPatients = d.IsAcceptingPatients,
                    ProfilePhotoUrl = d.ProfilePhotoUrl,
                    LanguagesSpoken = ParseJsonArray(d.LanguagesSpoken),
                    MedicalExpertise = ParseJsonArray(d.MedicalExpertiseTags)
                })
                .OrderByDescending(d => d.AverageRating)
                .ThenByDescending(d => d.YearsOfExperience)
                .ToList();

            // Extract unique specializations for filter
            specializations = doctors
                .Select(d => d.Specialization)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            filteredDoctors = doctors;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading doctors: {ex.Message}");
            doctors = new List<DoctorCardInfo>();
            filteredDoctors = new List<DoctorCardInfo>();
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Parses JSON array string to list
    /// Handles multiple formats: JSON array, comma-separated, or empty
    /// </summary>
    private List<string> ParseJsonArray(string? jsonArray)
    {
        if (string.IsNullOrWhiteSpace(jsonArray))
            return new List<string>();

        try
        {
            // Trim whitespace
            jsonArray = jsonArray.Trim();
            
            // If it starts with '[', assume it's JSON array format
            if (jsonArray.StartsWith('['))
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(jsonArray) ?? new List<string>();
            }
            
            // Otherwise, treat as comma-separated values
            return jsonArray
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing JSON array '{jsonArray}': {ex.Message}");
            
            // Last fallback: try to return as single-item list
            try
            {
                return new List<string> { jsonArray.Trim() };
            }
            catch
            {
                return new List<string>();
            }
        }
    }

    /// <summary>
    /// Filters doctors based on search query and filters
    /// </summary>
    private void ApplyFilters()
    {
        filteredDoctors = doctors.Where(d =>
        {
            // Search query filter
            bool matchesSearch = string.IsNullOrWhiteSpace(searchQuery) ||
                d.FullName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                d.Specialization.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                d.MedicalExpertise.Any(e => e.Contains(searchQuery, StringComparison.OrdinalIgnoreCase));

            // Specialization filter
            bool matchesSpecialization = selectedSpecialization == "All" ||
                d.Specialization == selectedSpecialization;

            // Availability filter
            bool matchesAvailability = selectedAvailability == "All" ||
                (selectedAvailability == "Available" && d.AvailabilityStatus == DoctorAvailabilityStatus.Available) ||
                (selectedAvailability == "Accepting" && d.IsAcceptingPatients);

            return matchesSearch && matchesSpecialization && matchesAvailability;
        }).ToList();

        StateHasChanged();
    }

    /// <summary>
    /// Handles search input change
    /// </summary>
    private void OnSearchChanged(ChangeEventArgs e)
    {
        searchQuery = e.Value?.ToString() ?? "";
        ApplyFilters();
    }

    /// <summary>
    /// Handles specialization filter change
    /// </summary>
    private void OnSpecializationChanged(ChangeEventArgs e)
    {
        selectedSpecialization = e.Value?.ToString() ?? "All";
        ApplyFilters();
    }

    /// <summary>
    /// Handles availability filter change
    /// </summary>
    private void OnAvailabilityChanged(ChangeEventArgs e)
    {
        selectedAvailability = e.Value?.ToString() ?? "All";
        ApplyFilters();
    }

    /// <summary>
    /// Navigates to consultation with selected doctor
    /// </summary>
    private void ConsultWithDoctor(Guid doctorId)
    {
        if (!AuthFacade.IsAuthenticated)
        {
            // Redirect to sign in with return URL
            Navigation.NavigateTo($"/auth/signin?returnUrl=/general/doctors");
            return;
        }

        if (AuthFacade.CurrentUser?.Role == UserRole.Patient)
        {
            // Navigate to patient consultation and pass doctor ID
            Navigation.NavigateTo($"/patient/consultation?doctorId={doctorId}");
        }
        else
        {
            // Other user types should sign in as patient
            Navigation.NavigateTo("/auth/signin");
        }
    }

    /// <summary>
    /// Gets availability status display text
    /// </summary>
    private string GetAvailabilityText(DoctorAvailabilityStatus status)
    {
        return status switch
        {
            DoctorAvailabilityStatus.Available => "Available Now",
            DoctorAvailabilityStatus.Busy => "Busy",
            DoctorAvailabilityStatus.Offline => "Offline",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Gets availability status CSS class
    /// </summary>
    private string GetAvailabilityClass(DoctorAvailabilityStatus status)
    {
        return status switch
        {
            DoctorAvailabilityStatus.Available => "status-available",
            DoctorAvailabilityStatus.Busy => "status-busy",
            DoctorAvailabilityStatus.Offline => "status-offline",
            _ => ""
        };
    }

    /// <summary>
    /// Doctor card information class
    /// </summary>
    private class DoctorCardInfo
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Title { get; set; } = "Dr.";
        public string Specialization { get; set; } = string.Empty;
        public int YearsOfExperience { get; set; }
        public decimal AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public DoctorAvailabilityStatus AvailabilityStatus { get; set; }
        public bool IsAcceptingPatients { get; set; }
        public string? ProfilePhotoUrl { get; set; }
        public List<string> LanguagesSpoken { get; set; } = new();
        public List<string> MedicalExpertise { get; set; } = new();
    }
}

