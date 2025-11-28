using VideoAnalysis.MCP.Models;

namespace VideoAnalysis.MCP.Abstractions;

/// <summary>
/// Interface for formatting AI analysis results
/// </summary>
public interface IAnalysisResultFormatter
{
    /// <summary>
    /// Formats video analysis result with timing analytics
    /// </summary>
    /// <param name="analysisResult">Analysis result with timing information</param>
    /// <param name="objectName">Name of the object that was analyzed</param>
    /// <param name="videoUrl">URL of the analyzed video</param>
    /// <param name="model">AI model used for analysis</param>
    /// <returns>Formatted result string with timing information</returns>
    string FormatVideoAnalysisResultWithAnalytics(VideoAnalysisResult analysisResult, string objectName, string videoUrl, string model);
}