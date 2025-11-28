using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using VideoAnalysis.MCP.Abstractions;

/// <summary>
/// MCP tool for analyzing videos directly from URLs using vision AI models
/// </summary>
[McpServerToolType]
public class VideoAnalysisTool
{
    private readonly IVideoProcessingService _videoProcessingService;
    private readonly IVideoUrlConverter _urlConverter;
    private readonly IPromptGenerator _promptGenerator;
    private readonly IAnalysisResultFormatter _resultFormatter;
    private readonly ILogger<VideoAnalysisTool> _logger;

    public VideoAnalysisTool(
        IVideoProcessingService videoProcessingService,
        IVideoUrlConverter urlConverter,
        IPromptGenerator promptGenerator,
        IAnalysisResultFormatter resultFormatter,
        ILogger<VideoAnalysisTool> logger)
    {
        _videoProcessingService = videoProcessingService;
        _urlConverter = urlConverter;
        _promptGenerator = promptGenerator;
        _resultFormatter = resultFormatter;
        _logger = logger;
    }

    /// <summary>
    /// Analyzes a video from a URL to detect objects and describe activities
    /// Supports various hosting services: Google Drive, Dropbox, direct video URLs
    /// Usage: "Analyze the video at https://drive.google.com/... for bird activity"
    /// </summary>
    /// <param name="videoUrl">URL to the video file (supports Google Drive, Dropbox, direct URLs)</param>
    /// <param name="objectName">Name of the object to detect and analyze (e.g., "Bird", "Car", "Person")</param>
    /// <param name="model">Vision AI model to use (default: OpenGVLab/InternVL3_5-14B-Instruct)</param>
    /// <returns>Analysis result with object detection and activity description</returns>
    [McpServerTool, Description("Analyzes a video from URL to detect objects and describe their activities. Supports Google Drive, Dropbox, and direct video URLs. Usage: Analyze video at [URL] for [object] activity")]
    public async Task<string> AnalyzeVideoFromUrlAsync(
        [Description("URL to the video file (Google Drive, Dropbox, or direct URL)")] string videoUrl,
        [Description("Name of the object to detect and analyze (e.g., Bird, Car, Person)")] string objectName,
        [Description("Vision AI model to use")] string model = "OpenGVLab/InternVL3_5-14B-Instruct")
    {
        try
        {
            _logger.LogInformation("Processing video URL analysis request - Object: {ObjectName}, URL: {VideoUrl}, Model: {Model}",
                objectName, videoUrl, model);

            // Convert sharing URLs to direct download URLs if needed
            var processedUrl = _urlConverter.ConvertToDirectDownloadUrl(videoUrl);

            // Create analysis prompt
            var prompt = _promptGenerator.CreateVideoAnalysisPrompt(objectName);

            // Analyze video using the vision AI model with analytics
            var analysisResult = await _videoProcessingService.AnalyzeVideoUrlWithAnalyticsAsync(processedUrl, prompt, model, 400);

            // Parse and format the response with timing information
            var formattedResult = _resultFormatter.FormatVideoAnalysisResultWithAnalytics(analysisResult, objectName, processedUrl, model);

            _logger.LogInformation("Video URL analysis completed successfully for {ObjectName} in {VideoUrl}", objectName, videoUrl);

            return formattedResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error processing video URL analysis request for {ObjectName} in {VideoUrl}", objectName, videoUrl);
            return $"❌ Error analyzing video: {ex.Message}";
        }
    }
}