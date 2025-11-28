namespace VideoAnalysis.MCP.Abstractions;

/// <summary>
/// Interface for video processing service
/// </summary>
public interface IVideoProcessingService
{
    /// <summary>
    /// Analyzes a video from a URL using a vision AI model
    /// </summary>
    Task<string> AnalyzeVideoUrlAsync(string videoUrl, string prompt, string model, int maxTokens);

}