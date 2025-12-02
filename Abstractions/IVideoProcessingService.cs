using VideoAnalysis.MCP.Models;

namespace VideoAnalysis.MCP.Abstractions;

/// <summary>
/// Interface for video processing service
/// </summary>
public interface IVideoProcessingService
{
    /// <summary>
    /// Analyzes a video from a URL using a vision AI model with detailed timing analytics
    /// </summary>
    Task<VideoAnalysisResult> AnalyzeVideoUrlWithAnalyticsAsync(string videoUrl, string prompt, string model, int maxTokens);

    /// <summary>
    /// Performs consensus analysis by running multiple detection attempts
    /// </summary>
    Task<ConsensusAnalysisResult> AnalyzeVideoWithConsensusAsync(string videoUrl, string prompt, string model, int maxTokens, int numberOfRuns = 3);
}