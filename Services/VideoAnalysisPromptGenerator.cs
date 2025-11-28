using VideoAnalysis.MCP.Abstractions;

namespace VideoAnalysis.MCP.Services;

/// <summary>
/// Service for generating AI prompts for video analysis
/// </summary>
public class VideoAnalysisPromptGenerator : IPromptGenerator
{
  /// <summary>
  /// Creates a detailed analysis prompt for video object detection
  /// </summary>
  /// <param name="objectName">Name of the object to detect</param>
  /// <returns>Formatted prompt for the AI model</returns>
  public string CreateVideoAnalysisPrompt(string objectName)
  {
    var lowerObjectName = objectName.ToLower();

    return $@"You are analyzing a video to look for {lowerObjectName}s and describe what they're doing.

TASK:
1. First, determine if there are any {lowerObjectName}s visible in this video
2. If found, describe what the {lowerObjectName}s are doing throughout the video

DETECTION CRITERIA for {lowerObjectName}s:
- Look for the characteristic shape and features of {lowerObjectName}s
- Consider size, posture, and context across the video timeline
- Be accurate but not overly strict

DESCRIPTION FOCUS (if detected):
- Activities throughout the video (movement patterns, behaviors)
- Interactions with environment and other objects/animals
- Location and positioning in the frame
- Any notable actions or characteristics visible
- Timeline of activities if multiple behaviors observed

OUTPUT FORMAT (JSON ONLY):
If {lowerObjectName}s detected:
{{
  ""detected"": true,
  ""description"": ""Detailed description of what the {lowerObjectName}s are doing in the video, including timeline and behaviors""
}}

If no {lowerObjectName}s detected:
{{
  ""detected"": false,
  ""description"": ""No {lowerObjectName}s detected in this video.""
}}";
  }
}