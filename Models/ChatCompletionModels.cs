namespace VideoAnalysis.MCP.Models;

/// <summary>
/// Response model for chat completion API
/// </summary>
public class ChatCompletionResponse
{
    public Choice[]? Choices { get; set; }
}

/// <summary>
/// Choice model for chat completion response
/// </summary>
public class Choice
{
    public Message? Message { get; set; }
}

/// <summary>
/// Message model for chat completion response
/// </summary>
public class Message
{
    public string? Content { get; set; }
}

/// <summary>
/// Video analysis result with timing analytics
/// </summary>
public class VideoAnalysisResult
{
    public string Content { get; set; } = string.Empty;
    public VideoAnalysisTimings Timings { get; set; } = new();
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Timing information for video analysis
/// </summary>
public class VideoAnalysisTimings
{
    public long ValidationTimeMs { get; set; }
    public long ApiCallTimeMs { get; set; }
    public long ParsingTimeMs { get; set; }
    public long TotalTimeMs { get; set; }
}

/// <summary>
/// Result of a single detection attempt
/// </summary>
public class DetectionResult
{
    public bool ObjectDetected { get; set; }
    public string Description { get; set; } = string.Empty;
    public double ConfidenceScore { get; set; }
    public VideoAnalysisTimings Timings { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public int AttemptNumber { get; set; }
}

/// <summary>
/// Result of consensus analysis with multiple runs
/// </summary>
public class ConsensusAnalysisResult
{
    public List<DetectionResult> IndividualResults { get; set; } = new();
    public bool FinalDetection { get; set; }
    public double ConfidenceLevel { get; set; }
    public string ConsensusDescription { get; set; } = string.Empty;
    public ConsensusMetrics Metrics { get; set; } = new();
    public string RecommendationNote { get; set; } = string.Empty;
}

/// <summary>
/// Metrics for consensus analysis
/// </summary>
public class ConsensusMetrics
{
    public int TotalRuns { get; set; }
    public int PositiveDetections { get; set; }
    public int NegativeDetections { get; set; }
    public double AverageProcessingTimeMs { get; set; }
    public double DetectionConsistency { get; set; }
    public List<string> QualityFlags { get; set; } = new();
}