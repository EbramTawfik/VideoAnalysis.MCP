using Microsoft.Extensions.Logging;
using VideoAnalysis.MCP.Abstractions;

namespace VideoAnalysis.MCP.Services;

/// <summary>
/// Service for converting video sharing URLs to direct download URLs
/// </summary>
public class VideoUrlConverter : IVideoUrlConverter
{
    private readonly ILogger<VideoUrlConverter> _logger;

    public VideoUrlConverter(ILogger<VideoUrlConverter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Converts sharing URLs from various services to direct download URLs
    /// </summary>
    /// <param name="url">Original sharing URL</param>
    /// <returns>Direct download URL</returns>
    public string ConvertToDirectDownloadUrl(string url)
    {
        try
        {
            // Google Drive conversion
            if (url.Contains("drive.google.com") && url.Contains("/file/d/"))
            {
                return ConvertGoogleDriveUrl(url);
            }

            // Dropbox conversion
            if (url.Contains("dropbox.com") && url.Contains("?dl=0"))
            {
                return ConvertDropboxUrl(url);
            }

            // If it's already a direct URL or unknown format, return as-is
            _logger.LogInformation("Using URL as-is: {Url}", url);
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error converting URL, using original: {Url}", url);
            return url;
        }
    }

    private string ConvertGoogleDriveUrl(string url)
    {
        var fileIdStart = url.IndexOf("/file/d/") + "/file/d/".Length;
        var fileIdEnd = url.IndexOf("/", fileIdStart);
        if (fileIdEnd == -1) fileIdEnd = url.IndexOf("?", fileIdStart);
        if (fileIdEnd == -1) fileIdEnd = url.Length;

        var fileId = url.Substring(fileIdStart, fileIdEnd - fileIdStart);
        var directUrl = $"https://drive.google.com/uc?export=download&id={fileId}";

        _logger.LogInformation("Converted Google Drive URL: {Original} -> {Direct}", url, directUrl);
        return directUrl;
    }

    private string ConvertDropboxUrl(string url)
    {
        var directUrl = url.Replace("?dl=0", "?dl=1");
        _logger.LogInformation("Converted Dropbox URL: {Original} -> {Direct}", url, directUrl);
        return directUrl;
    }
}