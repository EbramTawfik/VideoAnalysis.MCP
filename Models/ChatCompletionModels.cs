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