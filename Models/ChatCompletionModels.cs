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