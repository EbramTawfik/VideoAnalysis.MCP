using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VideoAnalysis.MCP.Abstractions;
using VideoAnalysis.MCP.Services;
using VideoAnalysis.MCP.Tools;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddSingleton<IVideoProcessingService, VideoProcessingService>()
    .AddSingleton<IVideoUrlConverter, VideoUrlConverter>()
    .AddSingleton<IPromptGenerator, VideoAnalysisPromptGenerator>()
    .AddSingleton<IAnalysisResultFormatter, VideoAnalysisResultFormatter>()
    .AddSingleton<IGoogleDriveService, GoogleDriveService>()
    .AddSingleton<IFolderProcessingService, FolderProcessingService>()
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<VideoAnalysisTool>()
    .WithTools<FolderProcessingTool>()
    .WithTools<VideoBatchProcessingTool>();

await builder.Build().RunAsync();
