using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;

namespace Inked.WebApp.Services;

public class SubmissionService
{
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;
    private readonly HttpClient _httpClient;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(HttpClient httpClient, IConfiguration configuration, ILogger<SubmissionService> logger,
        IWebHostEnvironment env)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
        _env = env;
    }

    public async Task<(Guid? SubmissionId, string? ErrorMessage)> CreateSubmissionAsync(SubmissionModel submission,
        IBrowserFile? selectedFile)
    {
        _logger.LogInformation("HandleValidSubmit method called.");

        if (selectedFile is null)
        {
            _logger.LogError("No file selected for submission.");
            return (null, "No file selected for submission.");
        }

        var content = new MultipartFormDataContent();
        content.Add(new StringContent(submission.Title), "Title");
        content.Add(new StringContent(submission.Description), "Description");
        content.Add(new StringContent(submission.Artist), "Artist");

        // Read the file into a byte array
        byte[] fileBytes;
        using (var stream =
               selectedFile.OpenReadStream(_configuration.GetValue<long>("Kestrel:Limits:MaxRequestBodySize")))
        {
            using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream);
                fileBytes = memoryStream.ToArray();
            }
        }

        // Create a new stream from the byte array
        using (var fileContent = new ByteArrayContent(fileBytes))
        {
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(selectedFile.ContentType);
            content.Add(fileContent, "Art", selectedFile.Name);
            var response = await _httpClient.PostAsync("/api/submission", content);

            if (response.IsSuccessStatusCode)
            {
                var submissionId = await response.Content.ReadFromJsonAsync<Guid>();
                return (submissionId, null);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();
                return (null, problemDetails?.Detail);
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                return (null, "Authentication failed. Please log in again.");
            }

            // Log the response content for debugging purposes
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Unexpected response content: {Content}", responseContent);

            return (null, "An unexpected error occurred.");
        }
    }

    public async Task<bool> IsTitleUniqueAsync(string title)
    {
        var response = await _httpClient.GetAsync($"/api/submission/check-title/{title}");
        _logger.LogInformation("Checking if title is unique: {Title}", title);
        if (response.IsSuccessStatusCode)
        {
            var isUnique = await response.Content.ReadFromJsonAsync<bool>();
            return isUnique;
        }

        return false;
    }

    public async Task<SubmissionModel?> GetSubmissionByIdAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"/api/submission/{id}");
        if (response.IsSuccessStatusCode)
        {
            var submission = await response.Content.ReadFromJsonAsync<SubmissionModel>();
            if (submission != null)
            {
                submission.ArtUrl = $"{_httpClient.BaseAddress}api/submission/art/{id}";
            }

            return submission;
        }

        return null;
    }

    // New Method to Save File and Generate Temporary URL
    public async Task<string> SaveFileForPreviewAsync(IBrowserFile selectedFile)
    {
        // Get the upload directory path
        var uploadDir = Path.Combine(_env.WebRootPath, "uploads");

        // Ensure the upload directory exists
        if (!Directory.Exists(uploadDir))
        {
            Directory.CreateDirectory(uploadDir);
        }

        // Generate a unique file name
        var fileName = Path.GetFileName(selectedFile.Name);
        var filePath = Path.Combine(uploadDir, fileName);

        // Save the file to the server
        using (var fileStream = new FileStream(filePath, FileMode.Create))
        {
            await selectedFile.OpenReadStream().CopyToAsync(fileStream);
        }

        // Return the temporary URL
        var fileUrl = $"/uploads/{fileName}"; // The URL to access the file
        _logger.LogInformation("File successfully uploaded for preview. URL: {FileUrl}", fileUrl);
        return fileUrl;
    }

    public async Task<List<SubmissionSummaryViewModel>?> GetAllSubmissionsAsync(
        string? sortBy = null, string? sortOrder = null, int? cardTypeId = null, int? themeId = null)
    {
        var queryParams = new List<string>();
        if (!string.IsNullOrEmpty(sortBy))
        {
            queryParams.Add($"sortBy={Uri.EscapeDataString(sortBy)}");
        }
        if (!string.IsNullOrEmpty(sortOrder))
        {
            queryParams.Add($"sortOrder={Uri.EscapeDataString(sortOrder)}");
        }
        if (cardTypeId.HasValue)
        {
            queryParams.Add($"cardTypeId={cardTypeId.Value}");
        }
        if (themeId.HasValue)
        {
            queryParams.Add($"themeId={themeId.Value}");
        }

        var queryString = queryParams.Any() ? $"?{string.Join("&", queryParams)}" : string.Empty;
        var requestUrl = $"/api/submission{queryString}"; // Ensure this matches the API endpoint for all submissions

        _logger.LogInformation("Fetching all submissions from URL: {RequestUrl}", requestUrl);

        try
        {
            var response = await _httpClient.GetAsync(requestUrl);
            if (response.IsSuccessStatusCode)
            {
                var submissions = await response.Content.ReadFromJsonAsync<List<SubmissionSummaryViewModel>>();
                return submissions;
            }

            _logger.LogError("Failed to fetch submissions. Status code: {StatusCode}, Reason: {ReasonPhrase}",
                response.StatusCode, response.ReasonPhrase);
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogError("Error content: {ErrorContent}", errorContent);
            return null; // Or throw an exception / return a result object with error info
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception occurred while fetching all submissions.");
            return null; // Or throw an exception / return a result object with error info
        }
    }
}

public class SubmissionModel
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required(ErrorMessage = "Title is required.")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Description is required.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Artist is required.")]
    public string Artist { get; set; } = string.Empty;

    public string ArtUrl { get; set; } = string.Empty;

    // The Author property is intentionally not validated with [Required]
    // as it's automatically populated from the authenticated user's identity.
    // It's not expected to be provided by the user directly in the form.
    public string Author { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select a card type.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid card type.")]
    public int CardTypeId { get; set; }

    [Required(ErrorMessage = "Please select at least one theme.")]
    [MinLength(1, ErrorMessage = "Please select at least one theme.")]
    public List<int> Themes { get; set; } = new();
}
