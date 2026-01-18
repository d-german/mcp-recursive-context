using Microsoft.Extensions.Hosting; 
 
namespace RecursiveContext.Mcp.Server.Server; 
 
internal static class ServerHost 
{ 
    public static async Task RunAsync(string[] args, CancellationToken cancellationToken = default) 
    { 
        var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings 
        { 
            Args = args 
        }); 
 
        LoggingConfiguration.Configure(builder.Logging); 
        ServerServices.Configure(builder.Services); 
 
        using var host = builder.Build(); 
        await host.RunAsync(cancellationToken).ConfigureAwait(false); 
    } 
}
