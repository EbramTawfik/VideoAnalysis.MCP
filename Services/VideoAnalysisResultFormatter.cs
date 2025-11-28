using System.Text.Json;
using Microsoft.Extensions.Logging;
using VideoAnalysis.MCP.Abstractions;
using VideoAnalysis.MCP.Models;

namespace VideoAnalysis.MCP.Services;

/// <summary>
/// Service for formatting AI analysis results
/// </summary>
public class VideoAnalysisResultFormatter : IAnalysisResultFormatter
{
    private readonly ILogger<VideoAnalysisResultFormatter> _logger;

    public VideoAnalysisResultFormatter(ILogger<VideoAnalysisResultFormatter> logger)
    {
        _logger = logger;
    }

    private string FormatHeader(string videoUrl, string objectName, string model)
    {
        return $"🎬 Video Analysis Results\n" +
               $"📹 Video URL: {GetDisplayUrl(videoUrl)}\n" +
               $"🔍 Object: {objectName}\n" +
               $"🤖 Model: {model}\n\n";
    }

    /// <summary>
    /// Formats video analysis result with timing analytics
    /// </summary>
    /// <param name="analysisResult">Analysis result with timing information</param>
    /// <param name="objectName">Name of the object that was analyzed</param>
    /// <param name="videoUrl">URL of the analyzed video</param>
    /// <param name="model">AI model used for analysis</param>
    /// <returns>Formatted result string with timing information</returns>
    public string FormatVideoAnalysisResultWithAnalytics(VideoAnalysisResult analysisResult, string objectName, string videoUrl, string model)
    {
        try
        {
            var result = FormatHeader(videoUrl, objectName, model);

            // Add timing information
            result += "⏱️ **Processing Analytics:**\n";
            result += $"• URL Validation: {analysisResult.Timings.ValidationTimeMs}ms\n";
            result += $"• API Call: {analysisResult.Timings.ApiCallTimeMs}ms\n";
            result += $"• Response Parsing: {analysisResult.Timings.ParsingTimeMs}ms\n";
            result += $"• **Total Processing Time: {analysisResult.Timings.TotalTimeMs}ms**\n\n";

            if (!analysisResult.IsSuccess)
            {
                result += $"❌ **Analysis Failed**\n";
                result += $"📝 **Error:** {analysisResult.ErrorMessage}\n";
                return result;
            }

            // Try to extract JSON from the response
            var jsonStart = analysisResult.Content.IndexOf('{');
            var jsonEnd = analysisResult.Content.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                var jsonContent = analysisResult.Content.Substring(jsonStart, jsonEnd - jsonStart + 1);
                var jsonResult = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                var detected = jsonResult.GetProperty("detected").GetBoolean();
                var description = jsonResult.GetProperty("description").GetString() ?? "No description provided";

                if (detected)
                {
                    result += $"✅ **{objectName} Detected!**\n";
                    result += $"📝 **Activity Description:**\n{description}\n";
                }
                else
                {
                    result += $"❌ **No {objectName} Detected**\n";
                    result += $"📝 **Analysis Notes:** {description}\n";
                }
            }
            else
            {
                result += $"📝 **Raw Response:**\n{analysisResult.Content}";
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error formatting analysis result with analytics, returning basic format");
            return FormatHeader(videoUrl, objectName, model) +
                   $"⏱️ **Total Processing Time: {analysisResult.Timings.TotalTimeMs}ms**\n\n" +
                   $"📝 **Response:**\n{analysisResult.Content}";
        }
    }

    private string GetDisplayUrl(string url)
    {
        try
        {
            return url.Length > 80 ? url.Substring(0, 77) + "..." : url;
        }
        catch
        {
            return url;
        }
    }
}