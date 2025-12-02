using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using VideoAnalysis.MCP.Abstractions;
using VideoAnalysis.MCP.Models;
using System.Text.Json;

namespace VideoAnalysis.MCP.Services;

/// <summary>
/// Service for interacting with Google Drive folders and files
/// </summary>
public class GoogleDriveService : IGoogleDriveService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<GoogleDriveService> _logger;
    private const string GOOGLE_DRIVE_API_BASE = "https://www.googleapis.com/drive/v3";

    public GoogleDriveService(ILogger<GoogleDriveService> logger)
    {
        _httpClient = new HttpClient();
        _logger = logger;
    }

    /// <summary>
    /// Extracts folder ID from various Google Drive folder URL formats
    /// </summary>
    public string ExtractFolderIdFromUrl(string folderUrl)
    {
        try
        {
            _logger.LogInformation("Extracting folder ID from URL: {FolderUrl}", folderUrl);

            // Handle different Google Drive URL formats
            var patterns = new[]
            {
                @"drive\.google\.com/drive/folders/([a-zA-Z0-9_-]+)",
                @"drive\.google\.com/drive/u/\d+/folders/([a-zA-Z0-9_-]+)",
                @"folders/([a-zA-Z0-9_-]+)"
            };

            foreach (var pattern in patterns)
            {
                var match = Regex.Match(folderUrl, pattern);
                if (match.Success)
                {
                    var folderId = match.Groups[1].Value;
                    _logger.LogInformation("Extracted folder ID: {FolderId}", folderId);
                    return folderId;
                }
            }

            // If no pattern matches, assume the string is already a folder ID
            if (Regex.IsMatch(folderUrl, @"^[a-zA-Z0-9_-]+$"))
            {
                _logger.LogInformation("Input appears to be a folder ID: {FolderId}", folderUrl);
                return folderUrl;
            }

            throw new ArgumentException($"Could not extract folder ID from URL: {folderUrl}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error extracting folder ID from URL: {FolderUrl}", folderUrl);
            throw;
        }
    }

    /// <summary>
    /// Gets all video files from a Google Drive folder using embedded folder view
    /// This method attempts to extract file IDs from the public folder HTML
    /// </summary>
    public async Task<List<GoogleDriveVideoFile>> GetVideoFilesFromFolderAsync(string folderId)
    {
        try
        {
            _logger.LogInformation("Getting video files from folder: {FolderId}", folderId);

            var videoFiles = new List<GoogleDriveVideoFile>();

            // Try to access the embedded folder view (same approach as PowerShell)
            var embeddedUrl = $"https://drive.google.com/embeddedfolderview?id={folderId}";

            _logger.LogInformation("Attempting to access embedded folder view: {EmbeddedUrl}", embeddedUrl);

            var response = await _httpClient.GetAsync(embeddedUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to access embedded folder view. Status: {StatusCode}", response.StatusCode);
                return videoFiles;
            }

            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Retrieved folder content. Length: {ContentLength}", content.Length);

            // Extract file IDs using the same pattern as PowerShell: /file/d/([^/]+)/
            var fileIdPattern = @"/file/d/([^/]+)/";
            var matches = Regex.Matches(content, fileIdPattern);

            _logger.LogInformation("Found {MatchCount} potential file matches", matches.Count);

            foreach (Match match in matches)
            {
                var fileId = match.Groups[1].Value;

                // Filter out the folder ID itself and ensure valid file ID format
                if (fileId != folderId && Regex.IsMatch(fileId, @"^[a-zA-Z0-9_-]{25,}$"))
                {
                    var videoFile = new GoogleDriveVideoFile
                    {
                        Id = fileId,
                        Name = $"DriveVideo_{fileId[..8]}",
                        WebViewUrl = $"https://drive.google.com/file/d/{fileId}/view",
                        CreatedTime = DateTime.UtcNow
                    };

                    videoFiles.Add(videoFile);
                    _logger.LogInformation("Found video file - ID: {FileId}, URL: {WebViewUrl}", fileId, videoFile.WebViewUrl);
                }
            }

            _logger.LogInformation("Successfully extracted {VideoCount} video files from folder", videoFiles.Count);
            return videoFiles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting video files from folder: {FolderId}", folderId);
            throw;
        }
    }
}