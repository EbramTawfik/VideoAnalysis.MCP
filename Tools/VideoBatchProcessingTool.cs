using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using VideoAnalysis.MCP.Abstractions;
using VideoAnalysis.MCP.Models;
using System.Globalization;
using CsvHelper;

namespace VideoAnalysis.MCP.Tools;

/// <summary>
/// Enhanced tool for processing individual video URLs and saving results to CSV
/// This is a practical solution when Google Drive API access is limited
/// </summary>
[McpServerToolType]
public class VideoBatchProcessingTool
{
    private readonly IVideoProcessingService _videoProcessingService;
    private readonly IVideoUrlConverter _urlConverter;
    private readonly IPromptGenerator _promptGenerator;
    private readonly ILogger<VideoBatchProcessingTool> _logger;

    public VideoBatchProcessingTool(
        IVideoProcessingService videoProcessingService,
        IVideoUrlConverter urlConverter,
        IPromptGenerator promptGenerator,
        ILogger<VideoBatchProcessingTool> logger)
    {
        _videoProcessingService = videoProcessingService;
        _urlConverter = urlConverter;
        _promptGenerator = promptGenerator;
        _logger = logger;
    }

    /// <summary>
    /// Processes multiple video URLs (from Google Drive or other sources) and saves results to CSV.
    /// This tool is perfect for analyzing videos from a Google Drive folder when you have the individual file URLs.
    /// For Google Drive files, use sharing URLs like: https://drive.google.com/file/d/FILE_ID/view
    /// </summary>
    /// <param name="videoUrls">List of video URLs, separated by newlines. Can be Google Drive sharing URLs, direct download links, or any supported video URLs.</param>
    /// <param name="objectName">Name of the object to detect and analyze (e.g., "Bird", "Car", "Person")</param>
    /// <param name="outputCsvPath">Optional path for output CSV file. If not provided, creates a file in current directory with timestamp.</param>
    /// <param name="model">Vision AI model to use</param>
    /// <param name="useConsensus">Whether to use consensus analysis with multiple runs for more reliable results</param>
    /// <param name="consensusRuns">Number of analysis runs for consensus (3-5 recommended, only used if useConsensus is true)</param>
    /// <returns>Summary of batch processing results and CSV file location</returns>
    [McpServerTool, Description("Processes multiple video URLs and saves bird detection results to CSV. Perfect for analyzing videos from Google Drive folders when you have individual file URLs. Supports consensus analysis for improved reliability.")]
    public async Task<string> ProcessVideoBatchAsync(
        [Description("List of video URLs separated by newlines. For Google Drive files, use sharing URLs like: https://drive.google.com/file/d/FILE_ID/view")] string videoUrls,
        [Description("Name of the object to detect and analyze (e.g., Bird, Car, Person)")] string objectName = "Bird",
        [Description("Optional path for output CSV file. If not provided, creates a file in current directory.")] string? outputCsvPath = null,
        [Description("Vision AI model to use")] string model = "OpenGVLab/InternVL3_5-14B-Instruct",
        [Description("Whether to use consensus analysis with multiple runs for more reliable results")] bool useConsensus = false,
        [Description("Number of analysis runs for consensus (3-5 recommended, only used if useConsensus is true)")] int consensusRuns = 3)
    {
        try
        {
            _logger.LogInformation("Processing video batch - Object: {ObjectName}, Consensus: {UseConsensus}", objectName, useConsensus);

            // Parse video URLs
            var urls = ParseVideoUrls(videoUrls);

            if (!urls.Any())
            {
                return "❌ No valid video URLs provided. Please provide video URLs separated by newlines.\n\n" +
                       "**Example format:**\n" +
                       "https://drive.google.com/file/d/1ABC123.../view\n" +
                       "https://drive.google.com/file/d/2DEF456.../view\n" +
                       "https://drive.google.com/file/d/3GHI789.../view";
            }

            // Generate output file path if not provided
            if (string.IsNullOrEmpty(outputCsvPath))
            {
                outputCsvPath = GenerateDefaultCsvPath(objectName);
            }

            _logger.LogInformation("Processing {VideoCount} videos", urls.Count);

            // Process each video
            var prompt = _promptGenerator.CreateVideoAnalysisPrompt(objectName);
            var results = new List<VideoProcessingResult>();
            var startTime = DateTime.UtcNow;

            int processed = 0;
            foreach (var videoUrl in urls)
            {
                processed++;
                _logger.LogInformation("Processing video {Current}/{Total}: {VideoUrl}", processed, urls.Count, videoUrl);

                try
                {
                    var result = await ProcessSingleVideoAsync(videoUrl, objectName, prompt, model, useConsensus, consensusRuns);
                    results.Add(result);

                    // Add small delay to avoid overwhelming the API
                    if (processed < urls.Count)
                    {
                        await Task.Delay(1000);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing video {VideoUrl}", videoUrl);
                    results.Add(new VideoProcessingResult
                    {
                        VideoName = ExtractVideoNameFromUrl(videoUrl),
                        VideoUrl = videoUrl,
                        HasBird = false,
                        Description = $"Processing error: {ex.Message}",
                        Status = "Failed",
                        ErrorMessage = ex.Message,
                        ProcessedAt = DateTime.UtcNow
                    });
                }
            }

            // Save results to CSV
            await SaveResultsToCsvAsync(results, outputCsvPath);

            var endTime = DateTime.UtcNow;
            var totalTime = endTime - startTime;

            // Generate summary
            var summary = GenerateBatchProcessingSummary(results, outputCsvPath, totalTime, objectName);

            _logger.LogInformation("Batch processing completed - {ProcessedCount}/{TotalCount} videos, CSV: {CsvPath}",
                results.Count, urls.Count, outputCsvPath);

            return summary;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error in batch video processing");
            return $"❌ Error processing video batch: {ex.Message}";
        }
    }

    /// <summary>
    /// Provides sample Google Drive URLs for testing (these would need to be replaced with actual video URLs)
    /// </summary>
    [McpServerTool, Description("Provides guidance on how to get individual video URLs from a Google Drive folder for batch processing.")]
    public async Task<string> GetGoogleDriveUrlGuidanceAsync(
        [Description("The Google Drive folder URL (e.g., https://drive.google.com/drive/folders/1o62Ys4-ecWBg-stSHgAOqPIobwK6SWN7)")] string folderUrl)
    {
        await Task.Delay(100); // Simulate async operation

        return $"📁 **Google Drive Folder Processing Guide**\n\n" +
               $"**Folder URL:** {folderUrl}\n\n" +
               $"Unfortunately, automatic folder listing requires Google Drive API authentication. Here's how to get individual video URLs for batch processing:\n\n" +
               $"**Method 1: Manual Collection**\n" +
               $"1. Open the Google Drive folder\n" +
               $"2. For each video file:\n" +
               $"   - Right-click → Get link\n" +
               $"   - Ensure it's set to 'Anyone with the link can view'\n" +
               $"   - Copy the sharing URL\n" +
               $"3. Collect all URLs in a text format like:\n" +
               $"   ```\n" +
               $"   https://drive.google.com/file/d/FILE_ID_1/view\n" +
               $"   https://drive.google.com/file/d/FILE_ID_2/view\n" +
               $"   https://drive.google.com/file/d/FILE_ID_3/view\n" +
               $"   ```\n\n" +
               $"**Method 2: Use Google Drive API (Advanced)**\n" +
               $"1. Set up Google Cloud Project\n" +
               $"2. Enable Google Drive API\n" +
               $"3. Create service account or OAuth2 credentials\n" +
               $"4. Implement authenticated folder listing\n\n" +
               $"**Next Steps:**\n" +
               $"Once you have the individual video URLs, use the `ProcessVideoBatchAsync` tool with all URLs to analyze them for bird detection and generate a CSV report.\n\n" +
               $"**Example Usage:**\n" +
               $"```\n" +
               $"ProcessVideoBatchAsync(\n" +
               $"  videoUrls: \"https://drive.google.com/file/d/1ABC.../view\\nhttps://drive.google.com/file/d/2DEF.../view\",\n" +
               $"  objectName: \"Bird\",\n" +
               $"  useConsensus: true\n" +
               $")\n" +
               $"```";
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
            VideoName = ExtractVideoNameFromUrl(videoUrl),
            ProcessedAt = DateTime.UtcNow
        };

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Convert to direct download URL if needed
            var processedUrl = _urlConverter.ConvertToDirectDownloadUrl(videoUrl);

            if (useConsensus)
            {
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
                    result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
                }
            }

            stopwatch.Stop();
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Status = "Failed";
            result.ErrorMessage = ex.Message;
            result.Description = $"Processing error: {ex.Message}";
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            return result;
        }
    }

    /// <summary>
    /// Parses a string containing multiple video URLs
    /// </summary>
    private List<string> ParseVideoUrls(string videoUrls)
    {
        if (string.IsNullOrWhiteSpace(videoUrls))
            return new List<string>();

        var urls = new List<string>();

        // Split by newlines and commas
        var rawUrls = videoUrls.Split(new[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawUrl in rawUrls)
        {
            var trimmedUrl = rawUrl.Trim();
            if (Uri.TryCreate(trimmedUrl, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                urls.Add(trimmedUrl);
            }
        }

        return urls;
    }

    /// <summary>
    /// Extracts a video name from its URL
    /// </summary>
    private string ExtractVideoNameFromUrl(string videoUrl)
    {
        try
        {
            var uri = new Uri(videoUrl);
            var fileName = Path.GetFileName(uri.LocalPath);

            if (!string.IsNullOrEmpty(fileName) && fileName != "/")
            {
                return fileName;
            }

            // For Google Drive URLs, extract ID and use as name
            var driveMatch = System.Text.RegularExpressions.Regex.Match(videoUrl, @"[/]d[/]([^/]+)");
            if (driveMatch.Success)
            {
                return $"DriveVideo_{driveMatch.Groups[1].Value[..8]}";
            }

            return $"Video_{DateTime.Now:HHmmss}";
        }
        catch
        {
            return $"Unknown_Video_{Guid.NewGuid().ToString()[..8]}";
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

        if (lowerDescription.Contains("clearly") || lowerDescription.Contains("definitely"))
            return 0.9;
        if (lowerDescription.Contains("likely") || lowerDescription.Contains("appears"))
            return 0.7;
        if (lowerDescription.Contains("possibly") || lowerDescription.Contains("might"))
            return 0.5;
        if (lowerDescription.Contains("unclear") || lowerDescription.Contains("difficult"))
            return 0.3;

        return 0.6;
    }

    /// <summary>
    /// Saves processing results to CSV file
    /// </summary>
    private async Task SaveResultsToCsvAsync(List<VideoProcessingResult> results, string csvPath)
    {
        try
        {
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
    /// Generates a default CSV file path
    /// </summary>
    private string GenerateDefaultCsvPath(string objectName)
    {
        var fileName = $"{objectName.ToLowerInvariant()}_detection_results_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return Path.Combine(Environment.CurrentDirectory, fileName);
    }

    /// <summary>
    /// Generates summary of batch processing results
    /// </summary>
    private string GenerateBatchProcessingSummary(List<VideoProcessingResult> results, string csvPath, TimeSpan totalTime, string objectName)
    {
        var successful = results.Count(r => r.Status == "Success");
        var failed = results.Count(r => r.Status != "Success");
        var withBird = results.Count(r => r.HasBird && r.Status == "Success");
        var withoutBird = results.Count(r => !r.HasBird && r.Status == "Success");

        var summary = $"🎯 **Batch Video Analysis Complete**\n\n";

        summary += $"**Processing Summary:**\n";
        summary += $"• Total Videos: {results.Count}\n";
        summary += $"• Successfully Analyzed: {successful}\n";
        summary += $"• Failed: {failed}\n";
        summary += $"• Processing Time: {totalTime:hh\\:mm\\:ss}\n";
        summary += $"• CSV File: `{csvPath}`\n\n";

        summary += $"**{objectName} Detection Results:**\n";
        summary += $"• Videos with {objectName}: {withBird}\n";
        summary += $"• Videos without {objectName}: {withoutBird}\n";

        if (withBird > 0)
        {
            var detectionRate = (double)withBird / successful * 100;
            summary += $"• Detection Rate: {detectionRate:F1}%\n";
        }

        summary += "\n";

        // Show sample successful detections
        var birdDetections = results.Where(r => r.HasBird && r.Status == "Success").Take(3);
        if (birdDetections.Any())
        {
            summary += $"**Sample {objectName} Detections:**\n";
            foreach (var detection in birdDetections)
            {
                summary += $"• {detection.VideoName}: {detection.ConfidenceScore:P0} confidence\n";
                var shortDescription = detection.Description.Length > 80
                    ? detection.Description[..80] + "..."
                    : detection.Description;
                summary += $"  \"{shortDescription}\"\n";
            }
            summary += "\n";
        }

        // Show failures if any
        if (failed > 0)
        {
            summary += $"**Issues ({failed} videos):**\n";
            var failedResults = results.Where(r => r.Status != "Success").Take(3);
            foreach (var failure in failedResults)
            {
                summary += $"• {failure.VideoName}: {failure.ErrorMessage}\n";
            }
            if (failed > 3)
            {
                summary += $"• ... and {failed - 3} more failures\n";
            }
            summary += "\n";
        }

        summary += $"📊 **CSV File Details:**\n";
        summary += $"The complete results have been saved to: `{csvPath}`\n";
        summary += $"Columns: Video Name, Has {objectName}, Description, Confidence Score, Processing Time, Video URL, Error Message, Analysis Status, Processed At\n\n";

        summary += $"🔍 **Analysis completed!** You can now open the CSV file to review all {objectName} detection results.";

        return summary;
    }
}