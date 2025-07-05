using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using ModelContextProtocol.Client;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;


// --- MCP OAuth Setup for GitHub ---
// Load configuration from appsettings.json
var configuration = new ConfigurationBuilder()
    .SetBasePath(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)!)
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .Build();

var clientId = configuration["GitHub:ClientId"];
var clientSecret = configuration["GitHub:ClientSecret"];
var mcpServerUrl = configuration["GitHub:McpServerUrl"];
var callbackUrl = configuration["OAuth:CallbackUrl"];
var scope = configuration["OAuth:Scope"];

Console.WriteLine("=== MCP OAuth Setup ===");
Console.WriteLine($"Using Client ID: {clientId}");

if (string.IsNullOrEmpty(clientSecret))
{
    Console.WriteLine("GitHub Client Secret is not configured in appsettings.json");
    Console.Write("Enter your GitHub Client Secret: ");
    clientSecret = Console.ReadLine();
}

if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
{
    Console.WriteLine("Client ID and Secret are required. Please configure them in appsettings.json");
    Console.WriteLine("Visit: https://github.com/settings/applications/new");
    Console.WriteLine($"Use callback URL: {callbackUrl}");
    return;
}

var githubOAuth = new OAuthConfig
{
    AuthUrl = "https://github.com/login/oauth/authorize",
    TokenUrl = "https://github.com/login/oauth/access_token",
    ClientId = clientId,
    ClientSecret = clientSecret,
    RedirectUri = callbackUrl ?? "http://localhost:8080/callback",
    Scope = scope ?? "read:user user:email"
};
var oauth = new OAuthHelper(githubOAuth);
var accessToken = await oauth.StartOAuthFlowAsync();
if (accessToken == null)
{
    Console.WriteLine("OAuth failed. Exiting.");
    return;
}

Console.WriteLine("\n🎉 OAuth Flow Successful!");
Console.WriteLine("===============================");
Console.WriteLine($"✅ Access Token: {accessToken}");
Console.WriteLine($"📅 Token obtained at: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"🔑 Token length: {accessToken.Length} characters");
Console.WriteLine($"🏷️  Token type: GitHub Personal Access Token");
Console.WriteLine("===============================\n");

// Connect to GitHub's MCP server
Console.WriteLine("Connecting to GitHub MCP server...");
await ConnectToGitHubMcpServer(mcpServerUrl!, accessToken);

// Demonstrate GitHub API usage with the token
await DemonstrateGitHubApiUsage(accessToken);

async Task ConnectToGitHubMcpServer(string serverUrl, string accessToken)
{
    try
    {
        Console.WriteLine($"Connecting to GitHub MCP SSE server at: {serverUrl}");
        
        // Create HTTP client with authorization header for SSE connection
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        httpClient.DefaultRequestHeaders.Add("Accept", "text/event-stream");
        httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        
        // Connect to SSE endpoint
        var sseUrl = serverUrl.TrimEnd('/') + "/sse";
        Console.WriteLine($"Connecting to SSE endpoint: {sseUrl}");
        
        var request = new HttpRequestMessage(HttpMethod.Get, sseUrl);
        var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("✅ Successfully connected to GitHub MCP SSE server!");
            
            // Read SSE stream
            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            
            Console.WriteLine("📡 Listening for SSE events...");
            Console.WriteLine("Press Ctrl+C to disconnect");
            
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6); // Remove "data: " prefix
                    Console.WriteLine($"📨 Received MCP message: {data}");
                    
                    // Here you would parse the MCP message and respond accordingly
                    // For now, we'll just display the raw message
                }
                else if (line.StartsWith("event: "))
                {
                    var eventType = line.Substring(7); // Remove "event: " prefix
                    Console.WriteLine($"🎯 Event type: {eventType}");
                }
                else if (string.IsNullOrEmpty(line))
                {
                    // Empty line indicates end of event
                    Console.WriteLine("---");
                }
            }
        }
        else
        {
            Console.WriteLine($"❌ Failed to connect to MCP SSE server. Status: {response.StatusCode}");
            Console.WriteLine($"Response: {await response.Content.ReadAsStringAsync()}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error connecting to MCP SSE server: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
    }
}

async Task DemonstrateGitHubApiUsage(string accessToken)
{
    Console.WriteLine("\n=== GitHub API Usage Examples ===");
    Console.WriteLine($"🔑 Your Access Token: {accessToken}");
    Console.WriteLine($"📅 Token expires: Check GitHub settings for expiration");
    Console.WriteLine($"🔒 Token scopes: {scope}");
    
    Console.WriteLine("\n📋 Curl Examples:");
    Console.WriteLine("1. Get your user info:");
    Console.WriteLine($"   curl -H \"Authorization: Bearer {accessToken}\" https://api.github.com/user");
    
    Console.WriteLine("\n2. List your repositories:");
    Console.WriteLine($"   curl -H \"Authorization: Bearer {accessToken}\" https://api.github.com/user/repos");
    
    Console.WriteLine("\n3. Get user emails:");
    Console.WriteLine($"   curl -H \"Authorization: Bearer {accessToken}\" https://api.github.com/user/emails");
    
    Console.WriteLine("\n4. GitHub MCP SSE endpoint:");
    Console.WriteLine($"   curl -H \"Authorization: Bearer {accessToken}\" \\");
    Console.WriteLine($"        -H \"Accept: text/event-stream\" \\");
    Console.WriteLine($"        -H \"Cache-Control: no-cache\" \\");
    Console.WriteLine($"        \"{mcpServerUrl}sse\"");
    
    // Actually test some GitHub API calls
    Console.WriteLine("\n🔬 Testing GitHub API calls...");
    try
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        httpClient.DefaultRequestHeaders.Add("User-Agent", "MCP-Host-App");
        
        // Get user info
        var userResponse = await httpClient.GetAsync("https://api.github.com/user");
        if (userResponse.IsSuccessStatusCode)
        {
            var userContent = await userResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"✅ User API call successful:");
            Console.WriteLine($"   {userContent}");
        }
        else
        {
            Console.WriteLine($"❌ User API call failed: {userResponse.StatusCode}");
        }
        
        // Get rate limit info
        var rateLimitResponse = await httpClient.GetAsync("https://api.github.com/rate_limit");
        if (rateLimitResponse.IsSuccessStatusCode)
        {
            var rateLimitContent = await rateLimitResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"\n✅ Rate limit info:");
            Console.WriteLine($"   {rateLimitContent}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error testing GitHub API: {ex.Message}");
    }
}

Console.WriteLine("\n🎉 MCP SSE connection completed!");
Console.WriteLine("The application listened for MCP messages over the SSE connection.");
Console.WriteLine();

// For demonstration, we'll skip the actual MCP connection since we don't have a server
// In a real scenario, you would connect to an SSE endpoint like:
// var sseClient = new SseClient("https://api.github.com/mcp/sse", accessToken);

Console.WriteLine("OAuth setup is complete. To test with a real MCP SSE server:");
Console.WriteLine("1. Set up your OAuth app in the provider (GitHub, Asana, etc.)");
Console.WriteLine("2. Replace YOUR_GITHUB_CLIENT_ID and YOUR_GITHUB_CLIENT_SECRET with real values");
Console.WriteLine("3. Implement the SSE client connection using the access token");
Console.WriteLine();

// Skip the rest of the MCP demo since we don't have a server
return;