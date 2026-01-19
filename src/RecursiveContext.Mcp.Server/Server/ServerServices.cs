using Microsoft.Extensions.DependencyInjection; 
using ModelContextProtocol.Protocol; 
using ModelContextProtocol.Server; 
using RecursiveContext.Mcp.Server.Config; 
using RecursiveContext.Mcp.Server.Services; 
 
namespace RecursiveContext.Mcp.Server.Server; 
 
internal static class ServerServices 
{ 
    public static void Configure(IServiceCollection services) 
    { 
        var metadata = ServerMetadata.Default; 
        var settings = ConfigReader.ReadSettings(); 
 
        // Register configuration 
        services.AddSingleton(settings); 
        services.AddSingleton(new PathResolver(settings)); 
 
        // Register services 
        services.AddSingleton<IGuardrailService, GuardrailService>(); 
        services.AddSingleton<IFileSystemService, FileSystemService>(); 
        services.AddSingleton<IContextMetadataService, ContextMetadataService>(); 
        services.AddSingleton<IPatternMatchingService, PatternMatchingService>();

        // Analysis services
        services.AddSingleton<IAggregationService, AggregationService>();
        services.AddSingleton<IChunkingService, ChunkingService>();
        services.AddSingleton<IContentAnalysisService, ContentAnalysisService>();

        services.AddSingleton(metadata); 
 
        services.AddMcpServer(options => 
        { 
            options.ServerInfo = new Implementation 
            { 
                Name = metadata.Name, 
                Version = metadata.Version 
            }; 
        }) 
        .WithStdioServerTransport() 
        .WithToolsFromAssembly(); 
    } 
}
