using VideoAnalysis.MCP.Models;

namespace VideoAnalysis.MCP.Abstractions;

/// <summary>
/// Interface for Google Drive operations
/// </summary>
public interface IGoogleDriveService
{
    /// <summary>
    /// Extracts folder ID from Google Drive folder URL
    /// </summary>
    string ExtractFolderIdFromUrl(string folderUrl);

    /// <summary>
    /// Gets all video files from a Google Drive folder
    /// </summary>
    Task<List<GoogleDriveVideoFile>> GetVideoFilesFromFolderAsync(string folderId);
}