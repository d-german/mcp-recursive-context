using System.Reflection; 
 
namespace RecursiveContext.Mcp.Server.Server; 
 
internal sealed record ServerMetadata(string Name, string Version) 
{ 
    public static ServerMetadata Default { get; } = new( 
        "recursive-context", 
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0" 
    ); 
}
