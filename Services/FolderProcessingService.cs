using System.Globalization;
using System.Text.Json;
using CsvHelper;
using Microsoft.Extensions.Logging;
using VideoAnalysis.MCP.Abstractions;
using VideoAnalysis.MCP.Models;
using System.Diagnostics;

namespace VideoAnalysis.MCP.Services;

/// <summary>
/// Service for processing folders of videos
/// </summary>
public class FolderProcessingService : IFolderProcessingService
{
    private readonly IGoogleDriveService _googleDriveService;
    private readonly IVideoProcessingService _videoProcessingService;
    private readonly IVideoUrlConverter _urlConverter;
    private readonly IPromptGenerator _promptGenerator;
    private readonly ILogger<FolderProcessingService> _logger;

    // Common video file extensions
    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mov", ".mkv", ".wmv", ".flv", ".webm", ".m4v", ".3gp", ".mpg", ".mpeg"
    };

    public FolderProcessingService(
        IGoogleDriveService googleDriveService,
        IVideoProcessingService videoProcessingService,
        IVideoUrlConverter urlConverter,
        IPromptGenerator promptGenerator,
        ILogger<FolderProcessingService> logger)
    {
        _googleDriveService = googleDriveService;
        _videoProcessingService = videoProcessingService;
        _urlConverter = urlConverter;
        _promptGenerator = promptGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Processes all videos in a Google Drive folder and saves results to CSV
    /// </summary>
    public async Task<FolderProcessingResult> ProcessGoogleDriveFolderAsync(
        string folderUrl,
        string objectName = "Bird",
        string model = "OpenGVLab/InternVL3_5-14B-Instruct",
        string? outputCsvPath = null,
        bool useConsensus = false,
        int consensusRuns = 3)
    {
        var result = new FolderProcessingResult
        {
            FolderUrl = folderUrl,
            ProcessingStartTime = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting folder processing for URL: {FolderUrl}, Object: {ObjectName}, Model: {Model}",
                folderUrl, objectName, model);

            // Extract folder ID
            result.FolderId = _googleDriveService.ExtractFolderIdFromUrl(folderUrl);

            // Since we can't easily list files in a Google Drive folder without API authentication,
            // we'll implement a different approach: ask user to provide individual video URLs
            // or implement a manual file discovery method

            _logger.LogInformation("Processing folder ID: {FolderId}", result.FolderId);

            // Alternative approach: Parse URLs from the folder or use manual input
            var videoUrls = await DiscoverVideoUrlsFromFolder(folderUrl);

            if (!videoUrls.Any())
            {
                _logger.LogWarning("No video URLs discovered from folder. You may need to provide individual video URLs.");
                result.Results = new List<VideoProcessingResult>
                {
                    new VideoProcessingResult
                    {
                        HasBird = false,
                        Description = "Could not automatically discover videos in folder. Please provide individual video URLs or ensure folder is publicly accessible.",
                        Status = "Failed",
                        ErrorMessage = "Automatic folder discovery not available without Google Drive API authentication",
                        ProcessedAt = DateTime.UtcNow
                    }
                };

                await SaveResultsToCsvAsync(result.Results, outputCsvPath ?? GenerateDefaultCsvPath(result.FolderId));
                return result;
            }

            result.TotalVideos = videoUrls.Count;
            _logger.LogInformation("Found {VideoCount} videos to process", result.TotalVideos);

            // Generate output file path if not provided
            if (string.IsNullOrEmpty(outputCsvPath))
            {
                outputCsvPath = GenerateDefaultCsvPath(result.FolderId);
            }
            result.CsvFilePath = outputCsvPath;

            // Process each video
            var prompt = _promptGenerator.CreateVideoAnalysisPrompt(objectName);
            var processingTasks = new List<Task<VideoProcessingResult>>();

            foreach (var videoUrl in videoUrls)
            {
                processingTasks.Add(ProcessSingleVideoAsync(videoUrl, objectName, prompt, model, useConsensus, consensusRuns));

                // Add small delay to avoid overwhelming the API
                await Task.Delay(1000);
            }

            // Wait for all processing to complete
            var results = await Task.WhenAll(processingTasks);
            result.Results = results.ToList();

            // Update statistics
            result.ProcessedVideos = result.Results.Count;
            result.SuccessfulAnalyses = result.Results.Count(r => r.Status == "Success");
            result.FailedAnalyses = result.ProcessedVideos - result.SuccessfulAnalyses;

            // Save results to CSV
            await SaveResultsToCsvAsync(result.Results, outputCsvPath);

            stopwatch.Stop();
            result.ProcessingEndTime = DateTime.UtcNow;
            result.TotalProcessingTime = stopwatch.Elapsed;

            _logger.LogInformation("Folder processing completed. Processed {ProcessedVideos}/{TotalVideos} videos " +
                                 "({SuccessfulAnalyses} successful, {FailedAnalyses} failed) in {TotalTime}",
                result.ProcessedVideos, result.TotalVideos, result.SuccessfulAnalyses,
                result.FailedAnalyses, result.TotalProcessingTime);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.ProcessingEndTime = DateTime.UtcNow;
            result.TotalProcessingTime = stopwatch.Elapsed;

            _logger.LogError(ex, "Error processing folder: {FolderUrl}", folderUrl);

            // Add error result
            result.Results.Add(new VideoProcessingResult
            {
                HasBird = false,
                Description = $"Folder processing failed: {ex.Message}",
                Status = "Failed",
                ErrorMessage = ex.Message,
                ProcessedAt = DateTime.UtcNow
            });

            return result;
        }
    }

    /// <summary>
    /// Discovers video URLs from a folder using Google Drive embedded view
    /// This uses the same approach as the PowerShell script
    /// </summary>
    private async Task<List<string>> DiscoverVideoUrlsFromFolder(string folderUrl)
    {
        var videoUrls = new List<string>();

        try
        {
            _logger.LogInformation("Attempting to discover videos from folder URL: {FolderUrl}", folderUrl);

            // Extract folder ID and use GoogleDriveService to get video files
            var folderId = _googleDriveService.ExtractFolderIdFromUrl(folderUrl);

            // Use the updated GoogleDriveService that implements embedded folder view access
            var videoFiles = await _googleDriveService.GetVideoFilesFromFolderAsync(folderId);

            foreach (var videoFile in videoFiles)
            {
                videoUrls.Add(videoFile.WebViewUrl);
                _logger.LogInformation("Added video URL: {VideoUrl} (Name: {VideoName})", videoFile.WebViewUrl, videoFile.Name);
            }

            if (videoUrls.Count == 0)
            {
                _logger.LogWarning("No video files found in folder. This may be due to:");
                _logger.LogWarning("1. The folder is private and requires authentication");
                _logger.LogWarning("2. The folder doesn't contain video files");
                _logger.LogWarning("3. The embedded view doesn't expose file listings for this folder");
            }

            return videoUrls;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error discovering videos from folder: {FolderUrl}", folderUrl);
            return videoUrls;
        }
    }

    /// <summary>
    /// Processes a single video and returns the result
    /// </summary>
    private async Task<VideoProcessingResult> ProcessSingleVideoAsync(
        string videoUrl,
        string objectName,
        string prompt,
        string model,
        bool useConsensus,
        int consensusRuns)
    {
        var result = new VideoProcessingResult
        {
            VideoUrl = videoUrl,
            ProcessedAt = DateTime.UtcNow
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Processing video: ({VideoUrl})", videoUrl);

            // Convert to direct download URL if needed
            var processedUrl = _urlConverter.ConvertToDirectDownloadUrl(videoUrl);

            if (useConsensus)
            {
                // Use consensus analysis
                var consensusResult = await _videoProcessingService.AnalyzeVideoWithConsensusAsync(
                    processedUrl, prompt, model, 400, consensusRuns);

                result.HasBird = consensusResult.FinalDetection;
                result.Description = consensusResult.ConsensusDescription;
                result.ConfidenceScore = consensusResult.ConfidenceLevel;

                if (consensusResult.IndividualResults.Any())
                {
                    result.ProcessingTimeMs = (long)consensusResult.Metrics.AverageProcessingTimeMs;
                }
            }
            else
            {
                // Use single analysis
                var analysisResult = await _videoProcessingService.AnalyzeVideoUrlWithAnalyticsAsync(
                    processedUrl, prompt, model, 400);

                if (analysisResult.IsSuccess)
                {
                    result.HasBird = ParseBirdDetection(analysisResult.Content);
                    result.Description = analysisResult.Content;
                    result.ConfidenceScore = CalculateConfidenceFromDescription(analysisResult.Content);
                    result.ProcessingTimeMs = analysisResult.Timings.TotalTimeMs;
                }
                else
                {
                    result.Status = "Failed";
                    result.ErrorMessage = analysisResult.ErrorMessage;
                    result.Description = $"Analysis failed: {analysisResult.ErrorMessage}";
                }
            }

            stopwatch.Stop();

            _logger.LogInformation("Completed processing video: ({VideoUrl}), HasBird: {HasBird}, Time: {ProcessingTime}ms",
                videoUrl, result.HasBird, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
            result.Description = $"Processing error: {ex.Message}";
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

            _logger.LogError(ex, "Error processing video: ({VideoUrl})", videoUrl);
            return result;
        }
    }

    /// <summary>
    /// Parses bird detection from analysis description
    /// </summary>
    private bool ParseBirdDetection(string description)
    {
        if (string.IsNullOrEmpty(description))
            return false;

        var lowerDescription = description.ToLowerInvariant();

        var positiveIndicators = new[] { "bird", "detected", "visible", "present", "found", "spotted", "observed", "flying", "perched" };
        var negativeIndicators = new[] { "no bird", "not detected", "not visible", "not present", "not found", "absent" };

        var hasPositive = positiveIndicators.Any(indicator => lowerDescription.Contains(indicator));
        var hasNegative = negativeIndicators.Any(indicator => lowerDescription.Contains(indicator));

        return hasPositive && !hasNegative;
    }

    /// <summary>
    /// Calculates confidence score from description
    /// </summary>
    private double CalculateConfidenceFromDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
            return 0.0;

        var lowerDescription = description.ToLowerInvariant();

        // Look for confidence indicators
        if (lowerDescription.Contains("clearly") || lowerDescription.Contains("definitely"))
            return 0.9;
        if (lowerDescription.Contains("likely") || lowerDescription.Contains("appears"))
            return 0.7;
        if (lowerDescription.Contains("possibly") || lowerDescription.Contains("might"))
            return 0.5;
        if (lowerDescription.Contains("unclear") || lowerDescription.Contains("difficult"))
            return 0.3;

        return 0.6; // Default moderate confidence
    }

    /// <summary>
    /// Saves processing results to CSV file
    /// </summary>
    private async Task SaveResultsToCsvAsync(List<VideoProcessingResult> results, string csvPath)
    {
        try
        {
            _logger.LogInformation("Saving {ResultCount} results to CSV: {CsvPath}", results.Count, csvPath);

            // Parse JSON descriptions and update results
            foreach (var result in results)
            {
                result.Description = ParseJsonDescription(result.Description);
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(csvPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var writer = new StringWriter();
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

            await csv.WriteRecordsAsync(results);
            var csvContent = writer.ToString();

            await File.WriteAllTextAsync(csvPath, csvContent);

            _logger.LogInformation("Results saved successfully to: {CsvPath}", csvPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving results to CSV: {CsvPath}", csvPath);
            throw;
        }
    }

    /// <summary>
    /// Parses JSON description and extracts only the description property
    /// </summary>
    private string ParseJsonDescription(string description)
    {
        if (string.IsNullOrEmpty(description))
            return description;

        try
        {
            // Try to parse as JSON
            var analysisDescription = JsonSerializer.Deserialize<AnalysisDescription>(description, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // Return only the description property
            return analysisDescription?.Description ?? description;
        }
        catch (JsonException)
        {
            // If JSON parsing fails, return the original description
            _logger.LogDebug("Description is not valid JSON, using original: {Description}", description);
            return description;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing JSON description, using original: {Description}", description);
            return description;
        }
    }

    /// <summary>
    /// Generates a default CSV file path
    /// </summary>
    private string GenerateDefaultCsvPath(string folderId)
    {
        var fileName = $"video_analysis_{folderId}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return Path.Combine(Environment.CurrentDirectory, fileName);
    }
}