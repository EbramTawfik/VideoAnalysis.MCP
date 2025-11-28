namespace VideoAnalysis.MCP.Abstractions;

/// <summary>
/// Interface for formatting AI analysis results
/// </summary>
public interface IAnalysisResultFormatter
{
    /// <summary>
    /// Formats raw AI response into user-friendly presentation
    /// </summary>
    /// <param name="rawResponse">Raw response from AI model</param>
    /// <param name="objectName">Name of the object that was analyzed</param>
    /// <param name="videoUrl">URL of the analyzed video</param>
    /// <param name="model">AI model used for analysis</param>
    /// <returns>Formatted result string</returns>
    string FormatVideoAnalysisResult(string rawResponse, string objectName, string videoUrl, string model);
}