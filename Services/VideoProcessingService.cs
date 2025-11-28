using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VideoAnalysis.MCP.Abstractions;
using VideoAnalysis.MCP.Models;

namespace VideoAnalysis.MCP.Services;

/// <summary>
/// Service for processing video frames and sending them to vision AI models
/// </summary>
public class VideoProcessingService : IVideoProcessingService, IDisposable
{
    private const string ENV_FILE_NAME = ".env";
    private const string API_KEY_ENV_VAR = "API_KEY";
    private const string API_URL_ENV_VAR = "API_URL";

    private readonly HttpClient _httpClient;
    private readonly ILogger<VideoProcessingService> _logger;
    private readonly string? _apiKey;
    private readonly string? _apiUrl;

    public VideoProcessingService(ILogger<VideoProcessingService> logger)
    {
        _httpClient = new HttpClient();
        _logger = logger;
        LoadConfiguration();
        _apiKey = GetConfigValue(API_KEY_ENV_VAR);
        _apiUrl = GetConfigValue(API_URL_ENV_VAR);
    }

    private readonly Dictionary<string, string> _envConfig = new();

    /// <summary>
    /// Loads configuration from .env file
    /// </summary>
    private void LoadConfiguration()
    {
        try
        {
            // First, try to read from .env file in the current directory
            if (File.Exists(ENV_FILE_NAME))
            {
                var envLines = File.ReadAllLines(ENV_FILE_NAME);
                foreach (var line in envLines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
                        continue;

                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim().Trim('"', '\'');
                        _envConfig[key] = value;
                    }
                }
                _logger.LogInformation("Configuration loaded from {FileName}", ENV_FILE_NAME);
            }
            else
            {
                _logger.LogInformation("No {FileName} file found, will use environment variables", ENV_FILE_NAME);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading configuration from {FileName}", ENV_FILE_NAME);
        }
    }

    /// <summary>
    /// Gets configuration value from .env file or environment variable
    /// </summary>
    /// <param name="envVarName">Environment variable name (also used as .env key)</param>
    /// <returns>Configuration value or null if not found</returns>
    private string? GetConfigValue(string envVarName)
    {
        try
        {
            // First try from .env file using the same key name
            if (_envConfig.TryGetValue(envVarName, out var envValue) && !string.IsNullOrWhiteSpace(envValue))
            {
                _logger.LogInformation("{EnvVarName} loaded from {FileName}", envVarName, ENV_FILE_NAME);
                return envValue;
            }

            // Fallback to environment variables
            var systemEnvValue = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrWhiteSpace(systemEnvValue))
            {
                _logger.LogInformation("{EnvVarName} loaded from environment variable", envVarName);
                return systemEnvValue;
            }

            _logger.LogWarning("No {EnvVarName} found in {FileName} file or environment variable", envVarName, ENV_FILE_NAME);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting configuration value for {EnvVarName}", envVarName);
            return null;
        }
    }

    /// <summary>
    /// Analyzes a video from a URL using a vision AI model with detailed timing analytics
    /// </summary>
    /// <param name="videoUrl">URL to the video file</param>
    /// <param name="prompt">Prompt for the AI model</param>
    /// <param name="model">The vision model to use</param>
    /// <param name="maxTokens">Maximum tokens in response</param>
    /// <returns>AI response with detailed timing information</returns>
    public async Task<VideoAnalysisResult> AnalyzeVideoUrlWithAnalyticsAsync(string videoUrl, string prompt, string model, int maxTokens)
    {
        var stopwatch = Stopwatch.StartNew();
        var validationStopwatch = Stopwatch.StartNew();
        var result = new VideoAnalysisResult();

        try
        {
            _logger.LogInformation("Starting video analysis for URL: {VideoUrl} using model: {Model}", videoUrl, model);

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                var errorMessage = $"No API key configured. Please configure it in {ENV_FILE_NAME} file or {API_KEY_ENV_VAR} environment variable.";
                _logger.LogError(errorMessage);
                result.IsSuccess = false;
                result.ErrorMessage = errorMessage;
                result.Content = errorMessage;
                return result;
            }

            if (string.IsNullOrWhiteSpace(_apiUrl))
            {
                var errorMessage = $"No API URL configured. Please configure it in {ENV_FILE_NAME} file or {API_URL_ENV_VAR} environment variable.";
                _logger.LogError(errorMessage);
                result.IsSuccess = false;
                result.ErrorMessage = errorMessage;
                result.Content = errorMessage;
                return result;
            }

            if (string.IsNullOrWhiteSpace(videoUrl))
            {
                var errorMessage = "Video URL is required";
                _logger.LogError(errorMessage);
                result.IsSuccess = false;
                result.ErrorMessage = errorMessage;
                result.Content = errorMessage;
                return result;
            }

            // Validate video URL accessibility
            _logger.LogInformation("Validating video URL accessibility...");
            if (!await ValidateVideoUrlAsync(videoUrl))
            {
                var errorMessage = "Video URL is not accessible or invalid";
                _logger.LogError(errorMessage);
                result.IsSuccess = false;
                result.ErrorMessage = errorMessage;
                result.Content = errorMessage;
                return result;
            }
            validationStopwatch.Stop();
            _logger.LogInformation("Video URL validation completed in {ValidationTime}ms", validationStopwatch.ElapsedMilliseconds);

            var requestUrl = $"{_apiUrl.TrimEnd('/')}/v1/chat/completions";

            var requestBody = new
            {
                model = model,
                max_tokens = maxTokens,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = prompt },
                            new { type = "video_url", video_url = new { url = videoUrl } }
                        }
                    }
                }
            };

            var jsonContent = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            _logger.LogInformation("Sending video analysis request to {Url} for video {VideoUrl}", requestUrl, videoUrl);

            var apiCallStopwatch = Stopwatch.StartNew();
            var response = await _httpClient.PostAsync(requestUrl, content);
            apiCallStopwatch.Stop();

            _logger.LogInformation("API call completed in {ApiCallTime}ms", apiCallStopwatch.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorMessage = $"Server error: {response.StatusCode} - {errorContent}";
                _logger.LogError("Video API request failed: {Error}", errorMessage);
                result.IsSuccess = false;
                result.ErrorMessage = errorMessage;
                result.Content = errorMessage;
                result.Timings.ValidationTimeMs = validationStopwatch.ElapsedMilliseconds;
                result.Timings.ApiCallTimeMs = apiCallStopwatch.ElapsedMilliseconds;
                return result;
            }

            var responseParsingStopwatch = Stopwatch.StartNew();
            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Raw video API response: {ResponseContent}", responseContent);

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var responseData = JsonSerializer.Deserialize<ChatCompletionResponse>(responseContent, options);

                var analysisResult = responseData?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response received";
                responseParsingStopwatch.Stop();

                stopwatch.Stop();

                _logger.LogInformation("Response parsing completed in {ParsingTime}ms", responseParsingStopwatch.ElapsedMilliseconds);
                _logger.LogInformation("Total video analysis completed in {TotalTime}ms (Validation: {ValidationTime}ms, API Call: {ApiCallTime}ms, Parsing: {ParsingTime}ms)",
                    stopwatch.ElapsedMilliseconds, validationStopwatch.ElapsedMilliseconds, apiCallStopwatch.ElapsedMilliseconds, responseParsingStopwatch.ElapsedMilliseconds);
                _logger.LogInformation("Parsed video response content: {Response}", analysisResult);

                result.IsSuccess = true;
                result.Content = analysisResult;
                result.Timings.ValidationTimeMs = validationStopwatch.ElapsedMilliseconds;
                result.Timings.ApiCallTimeMs = apiCallStopwatch.ElapsedMilliseconds;
                result.Timings.ParsingTimeMs = responseParsingStopwatch.ElapsedMilliseconds;
                result.Timings.TotalTimeMs = stopwatch.ElapsedMilliseconds;

                return result;
            }
            catch (JsonException ex)
            {
                responseParsingStopwatch.Stop();
                stopwatch.Stop();

                _logger.LogError(ex, "Failed to parse JSON response after {TotalTime}ms: {ResponseContent}", stopwatch.ElapsedMilliseconds, responseContent);
                result.IsSuccess = false;
                result.ErrorMessage = $"JSON parsing error: {ex.Message}";
                result.Content = result.ErrorMessage;
                result.Timings.ValidationTimeMs = validationStopwatch.ElapsedMilliseconds;
                result.Timings.ApiCallTimeMs = apiCallStopwatch.ElapsedMilliseconds;
                result.Timings.ParsingTimeMs = responseParsingStopwatch.ElapsedMilliseconds;
                result.Timings.TotalTimeMs = stopwatch.ElapsedMilliseconds;
                return result;
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var errorMessage = $"Error in AnalyzeVideoUrlWithAnalyticsAsync: {ex.Message}";
            _logger.LogError(ex, "Video analysis failed after {TotalTime}ms: {ErrorMessage}", stopwatch.ElapsedMilliseconds, ex.Message);
            result.IsSuccess = false;
            result.ErrorMessage = errorMessage;
            result.Content = errorMessage;
            result.Timings.TotalTimeMs = stopwatch.ElapsedMilliseconds;
            return result;
        }
    }

    /// <summary>
    /// Validates if a URL is accessible for video processing
    /// </summary>
    /// <param name="videoUrl">URL to validate</param>
    /// <returns>True if accessible, false otherwise</returns>
    public async Task<bool> ValidateVideoUrlAsync(string videoUrl)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("Starting URL validation for: {VideoUrl}", videoUrl);

            if (string.IsNullOrWhiteSpace(videoUrl))
            {
                _logger.LogWarning("Video URL validation failed: URL is null or empty");
                return false;
            }

            // Basic URL validation
            if (!Uri.TryCreate(videoUrl, UriKind.Absolute, out var uri))
            {
                _logger.LogWarning("Video URL validation failed: Invalid URL format - {VideoUrl}", videoUrl);
                return false;
            }

            // Check if it's HTTP/HTTPS
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                _logger.LogWarning("Video URL validation failed: Unsupported scheme '{Scheme}' - {VideoUrl}", uri.Scheme, videoUrl);
                return false;
            }

            // Try a HEAD request to check accessibility without downloading
            using var request = new HttpRequestMessage(HttpMethod.Head, videoUrl);
            using var response = await _httpClient.SendAsync(request);

            stopwatch.Stop();

            // Accept various success codes and partial content
            var isAccessible = response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.PartialContent;

            _logger.LogInformation("URL validation completed in {ValidationTime}ms. Status: {StatusCode}, Accessible: {IsAccessible}",
                stopwatch.ElapsedMilliseconds, response.StatusCode, isAccessible);

            return isAccessible;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Video URL validation failed for {VideoUrl} after {ValidationTime}ms: {ErrorMessage}",
                videoUrl, stopwatch.ElapsedMilliseconds, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Dispose of resources
    /// </summary>
    public void Dispose()
    {
        _httpClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}