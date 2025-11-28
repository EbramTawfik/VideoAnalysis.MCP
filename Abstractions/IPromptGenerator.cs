namespace VideoAnalysis.MCP.Abstractions;

/// <summary>
/// Interface for creating AI prompts for video analysis
/// </summary>
public interface IPromptGenerator
{
    /// <summary>
    /// Creates a detailed analysis prompt for video object detection
    /// </summary>
    /// <param name="objectName">Name of the object to detect</param>
    /// <returns>Formatted prompt for the AI model</returns>
    string CreateVideoAnalysisPrompt(string objectName);
}