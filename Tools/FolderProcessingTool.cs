using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using VideoAnalysis.MCP.Abstractions;

namespace VideoAnalysis.MCP.Tools;

/// <summary>
/// MCP tool for processing folders of videos and saving results to CSV
/// </summary>
[McpServerToolType]
public class FolderProcessingTool
{
    private readonly IFolderProcessingService _folderProcessingService;
    private readonly ILogger<FolderProcessingTool> _logger;

    public FolderProcessingTool(
        IFolderProcessingService folderProcessingService,
        ILogger<FolderProcessingTool> logger)
    {
        _folderProcessingService = folderProcessingService;
        _logger = logger;
    }

    /// <summary>
    /// Processes all videos in a Google Drive folder to detect birds and saves results to CSV
    /// Note: This is a demonstration tool. For production use, implement proper Google Drive API authentication.
    /// Currently works best when individual video URLs are provided or folder is publicly accessible.
    /// </summary>
    /// <param name="folderUrl">URL to the Google Drive folder containing videos</param>
    /// <param name="objectName">Name of the object to detect (e.g., "Bird", "Car", "Person")</param>
    /// <param name="model">Vision AI model to use</param>
    /// <param name="outputCsvPath">Optional path for the output CSV file. If not provided, generates a default name.</param>
    /// <param name="useConsensus">Whether to use consensus analysis for more reliable results</param>
    /// <param name="consensusRuns">Number of analysis runs for consensus (if useConsensus is true)</param>
    /// <returns>Summary of folder processing results and CSV file location</returns>
    [McpServerTool, Description("Processes all videos in a Google Drive folder to detect objects (especially birds) and saves results to CSV. Creates a report with video names, detection status, and descriptions.")]
    public async Task<string> ProcessGoogleDriveFolderAsync(
        [Description("URL to the Google Drive folder containing videos (e.g., https://drive.google.com/drive/folders/1o62Ys4-ecWBg-stSHgAOqPIobwK6SWN7)")] string folderUrl,
        [Description("Name of the object to detect and analyze (e.g., Bird, Car, Person)")] string objectName = "Bird",
        [Description("Vision AI model to use")] string model = "OpenGVLab/InternVL3_5-14B-Instruct",
        [Description("Optional path for output CSV file. If not provided, generates default name in current directory.")] string? outputCsvPath = null,
        [Description("Whether to use consensus analysis with multiple runs for more reliable results")] bool useConsensus = false,
        [Description("Number of analysis runs for consensus (3-5 recommended, only used if useConsensus is true)")] int consensusRuns = 3)
    {
        try
        {
            _logger.LogInformation("Processing Google Drive folder - URL: {FolderUrl}, Object: {ObjectName}, Consensus: {UseConsensus}",
                folderUrl, objectName, useConsensus);

            var result = await _folderProcessingService.ProcessGoogleDriveFolderAsync(
                folderUrl, objectName, model, outputCsvPath, useConsensus, consensusRuns);

            // Format the response
            var response = FormatFolderProcessingResult(result);

            _logger.LogInformation("Folder processing completed - Processed: {ProcessedVideos}, Successful: {SuccessfulAnalyses}, CSV: {CsvPath}",
                result.ProcessedVideos, result.SuccessfulAnalyses, result.CsvFilePath);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing Google Drive folder: {FolderUrl}", folderUrl);
            return $"❌ Error processing folder: {ex.Message}\n\n💡 **Troubleshooting Tips:**\n1. Ensure the folder URL is correct and publicly accessible\n2. For private folders, you may need to provide individual video URLs\n3. Check that the folder contains video files\n4. Verify your internet connection and API configuration";
        }
    }

    /// <summary>
    /// Formats the folder processing result into a readable response
    /// </summary>
    private string FormatFolderProcessingResult(VideoAnalysis.MCP.Models.FolderProcessingResult result)
    {
        var response = $"📁 **Google Drive Folder Processing Complete**\n\n";

        response += $"**Folder Information:**\n";
        response += $"• Folder URL: {result.FolderUrl}\n";
        response += $"• Folder ID: {result.FolderId}\n";
        response += $"• Processing Time: {result.TotalProcessingTime:hh\\:mm\\:ss}\n\n";

        response += $"**Processing Summary:**\n";
        response += $"• Total Videos: {result.TotalVideos}\n";
        response += $"• Successfully Processed: {result.SuccessfulAnalyses}\n";
        response += $"• Failed: {result.FailedAnalyses}\n";
        response += $"• CSV File: `{result.CsvFilePath}`\n\n";

        if (result.Results.Any())
        {
            var birdDetections = result.Results.Count(r => r.HasBird && r.Status == "Success");
            var noBirdDetections = result.Results.Count(r => !r.HasBird && r.Status == "Success");

            response += $"**Bird Detection Results:**\n";
            response += $"• Videos with Birds: {birdDetections}\n";
            response += $"• Videos without Birds: {noBirdDetections}\n\n";

            // Show sample results
            response += $"**Sample Results:**\n";
            var successfulResults = result.Results.Where(r => r.Status == "Success").Take(5);
            foreach (var videoResult in successfulResults)
            {
                var status = videoResult.HasBird ? "🐦 Bird Detected" : "❌ No Bird";
                response += $"•{status}\n";
                if (videoResult.HasBird)
                {
                    var shortDescription = videoResult.Description.Length > 100
                        ? videoResult.Description[..100] + "..."
                        : videoResult.Description;
                    response += $"  Description: {shortDescription}\n";
                }
            }

            if (result.Results.Count > 5)
            {
                response += $"• ... and {result.Results.Count - 5} more videos\n";
            }
        }

        if (result.FailedAnalyses > 0)
        {
            response += $"\n⚠️ **Issues Detected:**\n";
            var failedResults = result.Results.Where(r => r.Status != "Success");
            foreach (var failure in failedResults.Take(3))
            {
                response += $"• {failure.ErrorMessage}\n";
            }
        }

        response += $"\n📊 **CSV File Details:**\n";
        response += $"The results have been saved to: `{result.CsvFilePath}`\n";
        response += $"Columns include: Video Name, Has Bird, Description, Confidence Score, Processing Time, Video URL, Error Message, Analysis Status, Processed At\n\n";

        if (result.TotalVideos == 0)
        {
            response += $"⚠️ **Important Note:**\n";
            response += $"No videos were found in the specified folder. This may be due to:\n";
            response += $"1. The folder is private and requires authentication\n";
            response += $"2. The folder URL is incorrect\n";
            response += $"3. The folder doesn't contain video files\n";
            response += $"4. Google Drive API authentication is required\n\n";
            response += $"💡 **Workaround:** Use the individual video analysis tool for single videos.";
        }

        return response;
    }
}