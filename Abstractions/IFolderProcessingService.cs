using VideoAnalysis.MCP.Models;

namespace VideoAnalysis.MCP.Abstractions;

/// <summary>
/// Interface for processing folders of videos
/// </summary>
public interface IFolderProcessingService
{
    /// <summary>
    /// Processes all videos in a Google Drive folder and saves results to CSV
    /// </summary>
    Task<FolderProcessingResult> ProcessGoogleDriveFolderAsync(
        string folderUrl,
        string objectName = "Bird",
        string model = "OpenGVLab/InternVL3_5-14B-Instruct",
        string? outputCsvPath = null,
        bool useConsensus = false,
        int consensusRuns = 3);
}