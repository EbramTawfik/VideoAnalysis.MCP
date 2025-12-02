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

    /// <summary>
    /// Formats consensus analysis result with detailed metrics and recommendations
    /// </summary>
    /// <param name="consensusResult">Consensus analysis result</param>
    /// <param name="objectName">Name of the object that was analyzed</param>
    /// <param name="videoUrl">URL of the analyzed video</param>
    /// <param name="model">AI model used for analysis</param>
    /// <returns>Formatted consensus result string with metrics and recommendations</returns>
    public string FormatConsensusAnalysisResult(ConsensusAnalysisResult consensusResult, string objectName, string videoUrl, string model)
    {
        try
        {
            var result = "🎯 Consensus Video Analysis Results\n" +
                        $"📹 Video URL: {GetDisplayUrl(videoUrl)}\n" +
                        $"🔍 Object: {objectName}\n" +
                        $"🤖 Model: {model}\n" +
                        $"🔄 Analysis Runs: {consensusResult.Metrics.TotalRuns}\n\n";

            // Consensus Summary
            result += "📊 **Consensus Summary:**\n";
            if (consensusResult.FinalDetection)
            {
                result += $"✅ **{objectName} DETECTED** (Consensus: {consensusResult.Metrics.PositiveDetections}/{consensusResult.Metrics.TotalRuns} runs)\n";
            }
            else
            {
                result += $"❌ **NO {objectName} DETECTED** (Consensus: {consensusResult.Metrics.NegativeDetections}/{consensusResult.Metrics.TotalRuns} runs)\n";
            }

            result += $"🎯 **Confidence Level:** {consensusResult.ConfidenceLevel:P1}\n";
            result += $"📈 **Detection Rate:** {(double)consensusResult.Metrics.PositiveDetections / consensusResult.Metrics.TotalRuns:P1}\n";
            result += $"⏱️ **Average Processing Time:** {consensusResult.Metrics.AverageProcessingTimeMs:F0}ms\n\n";

            // Quality Flags
            if (consensusResult.Metrics.QualityFlags.Any())
            {
                result += "🏷️ **Quality Indicators:**\n";
                foreach (var flag in consensusResult.Metrics.QualityFlags)
                {
                    var emoji = flag switch
                    {
                        "HIGH_DETECTION_RATE" => "🟢",
                        "MODERATE_DETECTION_RATE" => "🟡",
                        "LOW_DETECTION_RATE" => "🔴",
                        "CONSISTENT_NEGATIVE" => "🔵",
                        "HIGH_CONSISTENCY" => "🟢",
                        "MODERATE_CONSISTENCY" => "🟡",
                        "LOW_CONSISTENCY" => "🔴",
                        "UNRELIABLE_DETECTION" => "⚠️",
                        "ANALYSIS_FAILED" => "❌",
                        _ => "ℹ️"
                    };
                    result += $"• {emoji} {flag.Replace('_', ' ')}\n";
                }
                result += "\n";
            }

            // Individual Run Details
            result += "🔍 **Individual Run Results:**\n";
            for (int i = 0; i < consensusResult.IndividualResults.Count; i++)
            {
                var run = consensusResult.IndividualResults[i];
                var statusEmoji = run.ObjectDetected ? "✅" : "❌";
                var errorInfo = !string.IsNullOrEmpty(run.ErrorMessage) ? " ⚠️" : "";

                result += $"• **Run {run.AttemptNumber}:** {statusEmoji} {(run.ObjectDetected ? "Detected" : "Not Detected")} " +
                         $"({run.Timings.TotalTimeMs}ms){errorInfo}\n";
            }
            result += "\n";

            // Final Description
            result += "📝 **Analysis Description:**\n";
            result += consensusResult.ConsensusDescription + "\n\n";

            // Recommendations
            if (!string.IsNullOrEmpty(consensusResult.RecommendationNote))
            {
                result += "💡 **Recommendations:**\n";
                result += consensusResult.RecommendationNote + "\n\n";
            }

            // Research Notes
            result += "🔬 **Research Notes:**\n";
            result += $"• This consensus analysis used {consensusResult.Metrics.TotalRuns} independent detection runs\n";
            result += $"• Results are suitable for research applications requiring {(consensusResult.ConfidenceLevel >= 0.8 ? "high" : consensusResult.ConfidenceLevel >= 0.6 ? "moderate" : "low")} confidence\n";

            var detectionRate = (double)consensusResult.Metrics.PositiveDetections / consensusResult.Metrics.TotalRuns;
            var variabilityDescription = detectionRate == 1.0 ? "consistent positive" :
                                       detectionRate == 0.0 ? "consistent negative" :
                                       detectionRate >= 0.6 ? "mostly positive" : "variable";
            result += $"• Detection variability: {(1 - detectionRate):P1} suggests {variabilityDescription} conditions\n";

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error formatting consensus analysis result, returning basic format");

            var basicResult = "🎯 Consensus Video Analysis Results\n" +
                             $"📹 Video URL: {GetDisplayUrl(videoUrl)}\n" +
                             $"🔍 Object: {objectName}\n" +
                             $"🔄 Analysis Runs: {consensusResult.Metrics.TotalRuns}\n\n";

            if (consensusResult.FinalDetection)
            {
                basicResult += $"✅ **{objectName} DETECTED** (Consensus)\n";
            }
            else
            {
                basicResult += $"❌ **NO {objectName} DETECTED** (Consensus)\n";
            }

            basicResult += $"🎯 **Confidence:** {consensusResult.ConfidenceLevel:P1}\n\n";
            basicResult += consensusResult.ConsensusDescription;

            return basicResult;
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