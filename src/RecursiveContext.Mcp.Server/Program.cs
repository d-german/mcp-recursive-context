using System.Reflection; 
using RecursiveContext.Mcp.Server.Server; 
 
var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"; 
 
if (args.Length > 0 && (args[0] == "--version" || args[0] == "-v")) 
{ 
    Console.WriteLine(version); 
    return; 
} 
 
if (args.Length > 0 && (args[0] == "--help" || args[0] == "-h" || args[0] == "-?")) 
{ 
    PrintHelp(version); 
    return; 
} 
 
await ServerHost.RunAsync(args); 
 
static void PrintHelp(string version) 
{ 
    Console.WriteLine($"RecursiveContext.Mcp Server v{version}"); 
    Console.WriteLine("A Model Context Protocol server for context-aware file exploration."); 
}
