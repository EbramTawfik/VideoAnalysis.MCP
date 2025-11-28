namespace VideoAnalysis.MCP.Abstractions;

/// <summary>
/// Interface for converting video sharing URLs to direct download URLs
/// </summary>
public interface IVideoUrlConverter
{
    /// <summary>
    /// Converts sharing URLs from various services to direct download URLs
    /// </summary>
    /// <param name="url">Original sharing URL</param>
    /// <returns>Direct download URL</returns>
    string ConvertToDirectDownloadUrl(string url);
}