using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;
using ai_clinic.Services.Facades;
using ai_clinic.Models;

namespace ai_clinic.UI.Pages.Patient;

/// <summary>
/// Patient Medical Records page
/// Uses Facade Pattern - only calls PatientFacade, never services directly
/// </summary>
public partial class Records : ComponentBase
{
    [Inject] private NavigationManager Navigation { get; set; } = null!;
    [Inject] private AuthFacade AuthFacade { get; set; } = null!;
    [Inject] private PatientFacade PatientFacade { get; set; } = null!;
    [Inject] private IJSRuntime JS { get; set; } = null!;

    private PatientRecordsData? recordsData;
    private List<RecordItem> allRecords = new();
    private List<RecordItem> filteredRecords = new();
    private List<TimelineItem> timeline = new();
    
    private string selectedFilter = "All";
    private string searchQuery = "";
    private bool isLoading = true;
    private bool showUploadModal = false;
    private bool showExportDialog = false;
    private bool isUploading = false;
    private bool isExporting = false;
    private string? errorMessage;
    private string? successMessage;
    private string? exportErrorMessage;

    // Upload form data
    private string uploadTitle = "";
    private string uploadType = "";
    private string uploadDescription = "";
    private IBrowserFile? selectedFile;
    private readonly long maxFileSize = 10 * 1024 * 1024; // 10MB

    // Export form data
    private string exportRange = "all";
    private DateTime? exportStartDate;
    private DateTime? exportEndDate;

    protected override async Task OnInitializedAsync()
    {
        if (!AuthFacade.IsAuthenticated || AuthFacade.CurrentUser?.Role != UserRole.Patient)
        {
            Navigation.NavigateTo("/auth/signin");
            return;
        }

        await LoadRecords();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                await JS.InvokeVoidAsync("lucide.createIcons");
            }
            catch
            {
                // Ignore if lucide is not available
            }
        }
    }

    /// <summary>
    /// Load all medical records through Facade
    /// </summary>
    private async Task LoadRecords()
    {
        isLoading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            // Call Facade - it coordinates all services
            recordsData = await PatientFacade.GetPatientRecordsAsync(AuthFacade.CurrentUser!.Id);
            
            // Transform to UI model
            allRecords = TransformToRecordItems(recordsData);
            filteredRecords = allRecords;

            // Load timeline
            timeline = await PatientFacade.GetMedicalTimelineAsync(AuthFacade.CurrentUser!.Id);
        }
        catch (Exception ex)
        {
            errorMessage = $"Error loading records: {ex.Message}";
            Console.WriteLine($"[ERROR] {ex}");
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Transform database models to UI display model
    /// </summary>
    private List<RecordItem> TransformToRecordItems(PatientRecordsData data)
    {
        var items = new List<RecordItem>();

        // Medical Records
        foreach (var record in data.MedicalRecords)
        {
            items.Add(new RecordItem
            {
                Id = record.Id,
                Title = record.Title,
                Type = record.RecordType,
                Date = record.RecordDate,
                Description = record.Content, // Use Content field
                FileSize = 0, // Medical records don't have file size
                RecordCategory = "medical_record"
            });
        }

        // Prescriptions
        foreach (var prescription in data.Prescriptions)
        {
            items.Add(new RecordItem
            {
                Id = prescription.Id,
                Title = $"Prescription - {prescription.MedicationName}",
                Type = "Prescription",
                Date = prescription.CreatedAt, // Use CreatedAt field
                Description = $"{prescription.Dosage} - {prescription.Instructions}",
                FileSize = 0,
                RecordCategory = "prescription"
            });
        }

        // Documents
        foreach (var document in data.Documents)
        {
            items.Add(new RecordItem
            {
                Id = document.Id,
                Title = document.Title ?? document.FileName,
                Type = document.DocumentTypeString ?? document.FileType.ToString(),
                Date = document.CreatedAt,
                Description = document.Description ?? "",
                FileSize = document.FileSizeBytes,
                RecordCategory = "document"
            });
        }

        return items.OrderByDescending(r => r.Date).ToList();
    }

    /// <summary>
    /// Filter records by type
    /// </summary>
    private void FilterRecords(string filter)
    {
        selectedFilter = filter;
        ApplyFilters();
    }

    /// <summary>
    /// Handle search input
    /// </summary>
    private void OnSearchChanged(ChangeEventArgs e)
    {
        searchQuery = e.Value?.ToString() ?? "";
        ApplyFilters();
    }

    /// <summary>
    /// Apply filters and search
    /// </summary>
    private void ApplyFilters()
    {
        filteredRecords = allRecords.Where(r =>
        {
            // Filter by type
            bool matchesFilter = selectedFilter == "All" ||
                (selectedFilter == "Lab Results" && r.Type == "Lab Result") ||
                (selectedFilter == "Prescriptions" && r.Type == "Prescription") ||
                (selectedFilter == "Imaging" && r.Type == "Imaging") ||
                (selectedFilter == "Visit Notes" && r.Type == "Visit Note") ||
                (selectedFilter == "Immunizations" && r.Type == "Immunization");

            // Search query
            bool matchesSearch = string.IsNullOrWhiteSpace(searchQuery) ||
                r.Title.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                r.Description.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);

            return matchesFilter && matchesSearch;
        }).ToList();

        StateHasChanged();
    }

    /// <summary>
    /// Show upload modal
    /// </summary>
    private void ShowUploadDialog()
    {
        showUploadModal = true;
        uploadTitle = "";
        uploadType = "";
        uploadDescription = "";
        selectedFile = null;
        errorMessage = null;
        StateHasChanged();
    }

    /// <summary>
    /// Close upload modal
    /// </summary>
    private void CloseUploadDialog()
    {
        showUploadModal = false;
        StateHasChanged();
    }

    /// <summary>
    /// Handle file selection
    /// </summary>
    private void OnFileSelected(InputFileChangeEventArgs e)
    {
        selectedFile = e.File;
        if (string.IsNullOrWhiteSpace(uploadTitle))
        {
            uploadTitle = selectedFile.Name;
        }
        StateHasChanged();
    }

    /// <summary>
    /// Upload document through Facade
    /// </summary>
    private async Task UploadDocument()
    {
        if (selectedFile == null || string.IsNullOrWhiteSpace(uploadTitle) || string.IsNullOrWhiteSpace(uploadType))
        {
            errorMessage = "Please fill in all required fields and select a file.";
            return;
        }

        if (selectedFile.Size > maxFileSize)
        {
            errorMessage = $"File size exceeds maximum allowed size of {maxFileSize / 1024 / 1024}MB.";
            return;
        }

        isUploading = true;
        errorMessage = null;
        StateHasChanged();

        try
        {
            // Read file data
            using var stream = selectedFile.OpenReadStream(maxFileSize);
            using var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            var fileData = memoryStream.ToArray();

            // Upload through Facade
            await PatientFacade.UploadMedicalDocumentAsync(
                AuthFacade.CurrentUser!.Id,
                uploadTitle,
                uploadType,
                fileData,
                selectedFile.Name,
                uploadDescription
            );

            successMessage = "Document uploaded successfully!";
            showUploadModal = false;
            
            // Reload records
            await LoadRecords();
        }
        catch (Exception ex)
        {
            errorMessage = $"Error uploading document: {ex.Message}";
            Console.WriteLine($"[ERROR] {ex}");
        }
        finally
        {
            isUploading = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Download record through Facade
    /// </summary>
    private async Task DownloadRecord(Guid recordId, string recordCategory)
    {
        try
        {
            if (recordCategory == "document")
            {
                var fileData = await PatientFacade.DownloadMedicalDocumentAsync(AuthFacade.CurrentUser!.Id, recordId);
                if (fileData != null)
                {
                    // Trigger browser download
                    var record = allRecords.FirstOrDefault(r => r.Id == recordId);
                    var fileName = record?.Title ?? "document";
                    await JS.InvokeVoidAsync("downloadFile", fileName, Convert.ToBase64String(fileData));
                }
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error downloading record: {ex.Message}";
            Console.WriteLine($"[ERROR] {ex}");
        }
    }

    /// <summary>
    /// Delete record through Facade
    /// </summary>
    private async Task DeleteRecord(Guid recordId, string recordCategory)
    {
        if (!await JS.InvokeAsync<bool>("confirm", "Are you sure you want to delete this record?"))
        {
            return;
        }

        try
        {
            var success = await PatientFacade.DeleteMedicalRecordAsync(
                AuthFacade.CurrentUser!.Id,
                recordId,
                recordCategory
            );

            if (success)
            {
                successMessage = "Record deleted successfully!";
                await LoadRecords();
            }
            else
            {
                errorMessage = "Failed to delete record.";
            }
        }
        catch (Exception ex)
        {
            errorMessage = $"Error deleting record: {ex.Message}";
            Console.WriteLine($"[ERROR] {ex}");
        }
    }

    /// <summary>
    /// Show export dialog
    /// </summary>
    private void ShowExportDialog()
    {
        showExportDialog = true;
        exportRange = "all";
        exportStartDate = null;
        exportEndDate = null;
        exportErrorMessage = null;
        StateHasChanged();
    }

    /// <summary>
    /// Close export dialog
    /// </summary>
    private void CloseExportDialog()
    {
        showExportDialog = false;
        exportErrorMessage = null;
        StateHasChanged();
    }

    /// <summary>
    /// Export all records
    /// </summary>
    private async Task ExportRecords()
    {
        try
        {
            exportErrorMessage = null;

            // Validate date range if custom
            if (exportRange == "custom")
            {
                if (!exportStartDate.HasValue || !exportEndDate.HasValue)
                {
                    exportErrorMessage = "Please select both start and end dates.";
                    StateHasChanged();
                    return;
                }

                if (exportStartDate.Value > exportEndDate.Value)
                {
                    exportErrorMessage = "Start date must be before end date.";
                    StateHasChanged();
                    return;
                }
            }

            isExporting = true;
            StateHasChanged();

            // Export medical records to PDF through Facade
            DateTime? startDate = exportRange == "custom" ? exportStartDate : null;
            DateTime? endDate = exportRange == "custom" ? exportEndDate : null;

            var pdfBytes = await PatientFacade.ExportMedicalRecordsToPdfAsync(
                AuthFacade.CurrentUser!.Id,
                startDate,
                endDate);

            // Download the PDF file
            var dateRangeStr = exportRange == "custom" 
                ? $"_{exportStartDate:yyyyMMdd}_to_{exportEndDate:yyyyMMdd}"
                : "";
            var fileName = $"Medical_Records{dateRangeStr}_{DateTime.Now:yyyyMMdd}.pdf";
            
            await JS.InvokeVoidAsync("downloadFileFromBytes", fileName, "application/pdf", pdfBytes);

            successMessage = "Medical records exported successfully!";
            CloseExportDialog();
            StateHasChanged();
        }
        catch (Exception ex)
        {
            exportErrorMessage = $"Error exporting records: {ex.Message}";
            Console.WriteLine($"[ERROR] {ex}");
            StateHasChanged();
        }
        finally
        {
            isExporting = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// Get icon class for record type
    /// </summary>
    private string GetRecordIconClass(string type)
    {
        return type switch
        {
            "Lab Result" => "lab",
            "Prescription" => "prescription",
            "Visit Note" => "visit",
            "Imaging" => "imaging",
            "Immunization" => "immunization",
            _ => "other"
        };
    }

    /// <summary>
    /// Get badge class for record type
    /// </summary>
    private string GetBadgeClass(string type)
    {
        return type switch
        {
            "Lab Result" => "badge-lab",
            "Prescription" => "badge-prescription",
            "Visit Note" => "badge-visit",
            "Imaging" => "badge-imaging",
            "Immunization" => "badge-immunization",
            _ => "badge-other"
        };
    }

    /// <summary>
    /// Format file size
    /// </summary>
    private string FormatFileSize(long bytes)
    {
        if (bytes == 0) return "";
        
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        
        return $"{size:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Format date
    /// </summary>
    private string FormatDate(DateTime date)
    {
        return date.ToString("MMM dd, yyyy");
    }

    /// <summary>
    /// UI model for record display
    /// </summary>
    private class RecordItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Description { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string RecordCategory { get; set; } = string.Empty; // medical_record, prescription, document
    }
}
