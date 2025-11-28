using System.Text.Json;
using Microsoft.Extensions.Logging;
using VideoAnalysis.MCP.Abstractions;

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

    /// <summary>
    /// Formats raw AI response into user-friendly presentation
    /// </summary>
    /// <param name="rawResponse">Raw response from AI model</param>
    /// <param name="objectName">Name of the object that was analyzed</param>
    /// <param name="videoUrl">URL of the analyzed video</param>
    /// <param name="model">AI model used for analysis</param>
    /// <returns>Formatted result string</returns>
    public string FormatVideoAnalysisResult(string rawResponse, string objectName, string videoUrl, string model)
    {
        try
        {
            // Try to extract JSON from the response
            var jsonStart = rawResponse.IndexOf('{');
            var jsonEnd = rawResponse.LastIndexOf('}');

            if (jsonStart >= 0 && jsonEnd > jsonStart)
            {
                return FormatJsonResponse(rawResponse, objectName, videoUrl, model, jsonStart, jsonEnd);
            }
            else
            {
                return FormatFallbackResponse(rawResponse, objectName, videoUrl, model);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error formatting analysis result, returning raw response");
            return FormatErrorResponse(rawResponse, objectName, videoUrl);
        }
    }

    private string FormatJsonResponse(string rawResponse, string objectName, string videoUrl, string model, int jsonStart, int jsonEnd)
    {
        var jsonContent = rawResponse.Substring(jsonStart, jsonEnd - jsonStart + 1);
        var analysisResult = JsonSerializer.Deserialize<JsonElement>(jsonContent);

        var detected = analysisResult.GetProperty("detected").GetBoolean();
        var description = analysisResult.GetProperty("description").GetString() ?? "No description provided";

        var result = FormatHeader(videoUrl, objectName, model);

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

        return result;
    }

    private string FormatFallbackResponse(string rawResponse, string objectName, string videoUrl, string model)
    {
        return FormatHeader(videoUrl, objectName, model) +
               $"📝 **Raw Response:**\n{rawResponse}";
    }

    private string FormatErrorResponse(string rawResponse, string objectName, string videoUrl)
    {
        return $"🎬 Video Analysis Results\n" +
               $"📹 Video URL: {GetDisplayUrl(videoUrl)}\n" +
               $"🔍 Object: {objectName}\n\n" +
               $"📝 **Response:**\n{rawResponse}";
    }

    private string FormatHeader(string videoUrl, string objectName, string model)
    {
        return $"🎬 Video Analysis Results\n" +
               $"📹 Video URL: {GetDisplayUrl(videoUrl)}\n" +
               $"🔍 Object: {objectName}\n" +
               $"🤖 Model: {model}\n\n";
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