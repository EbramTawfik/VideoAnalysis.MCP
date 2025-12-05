using CsvHelper.Configuration.Attributes;

namespace VideoAnalysis.MCP.Models;

/// <summary>
/// Model for Google Drive video file information
/// </summary>
public class GoogleDriveVideoFile
{
    public string Name { get; set; } = string.Empty;
    public string Id { get; set; } = string.Empty;
    public string DirectDownloadUrl => $"https://drive.google.com/uc?id={Id}&export=download";
    public string WebViewUrl { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime CreatedTime { get; set; }
}

/// <summary>
/// Result of processing a folder of videos
/// </summary>
public class FolderProcessingResult
{
    public string FolderUrl { get; set; } = string.Empty;
    public string FolderId { get; set; } = string.Empty;
    public List<VideoProcessingResult> Results { get; set; } = new();
    public int TotalVideos { get; set; }
    public int ProcessedVideos { get; set; }
    public int SuccessfulAnalyses { get; set; }
    public int FailedAnalyses { get; set; }
    public DateTime ProcessingStartTime { get; set; }
    public DateTime ProcessingEndTime { get; set; }
    public TimeSpan TotalProcessingTime { get; set; }
    public string CsvFilePath { get; set; } = string.Empty;
}

/// <summary>
/// Result of processing a single video
/// </summary>
public class VideoProcessingResult
{
    [Name("Has Bird")]
    public bool HasBird { get; set; }

    [Name("Description")]
    public string Description { get; set; } = string.Empty;

    [Name("Confidence Score")]
    public double ConfidenceScore { get; set; }

    [Name("Processing Time (ms)")]
    public long ProcessingTimeMs { get; set; }

    [Name("Video URL")]
    public string VideoUrl { get; set; } = string.Empty;

    [Name("Error Message")]
    public string? ErrorMessage { get; set; }

    [Name("Analysis Status")]
    public string Status { get; set; } = "Success";

    [Name("Processed At")]
    public DateTime ProcessedAt { get; set; }
}