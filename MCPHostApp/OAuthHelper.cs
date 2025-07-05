using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Net;
using System.Text;

public class OAuthConfig
{
    public string AuthUrl { get; set; } = string.Empty;
    public string TokenUrl { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost:8080/callback";
    public string Scope { get; set; } = string.Empty;
}

public class OAuthHelper
{
    private readonly OAuthConfig _config;
    private HttpListener? _listener;

    public OAuthHelper(OAuthConfig config) => _config = config;

    public async Task<string?> StartOAuthFlowAsync(string state = "random_state")
    {
        // Start local HTTP server to listen for callback
        _listener = new HttpListener();
        _listener.Prefixes.Add(_config.RedirectUri + "/");
        _listener.Start();
        
        Console.WriteLine("Starting OAuth flow...");
        Console.WriteLine($"Listening for callback on {_config.RedirectUri}");

        // Open browser to authorization URL
        var authUrl = $"{_config.AuthUrl}?client_id={_config.ClientId}&redirect_uri={Uri.EscapeDataString(_config.RedirectUri)}&response_type=code&state={state}&scope={Uri.EscapeDataString(_config.Scope)}";
        Console.WriteLine($"Opening browser to: {authUrl}");
        
        try 
        { 
            Process.Start(new ProcessStartInfo { FileName = authUrl, UseShellExecute = true }); 
        } 
        catch (Exception ex)
        {
            Console.WriteLine($"Could not open browser automatically: {ex.Message}");
            Console.WriteLine($"Please manually open this URL in your browser:\n{authUrl}");
        }

        // Wait for callback
        var context = await _listener.GetContextAsync();
        var request = context.Request;
        var response = context.Response;

        // Extract authorization code from callback
        var code = request.QueryString["code"];
        var returnedState = request.QueryString["state"];
        var error = request.QueryString["error"];

        // Send response to browser
        string responseString;
        if (!string.IsNullOrEmpty(error))
        {
            responseString = $"<html><body><h1>OAuth Error</h1><p>Error: {error}</p><p>You can close this window.</p></body></html>";
            response.StatusCode = 400;
        }
        else if (!string.IsNullOrEmpty(code))
        {
            responseString = "<html><body><h1>Authorization Successful!</h1><p>You can close this window and return to the application.</p></body></html>";
            response.StatusCode = 200;
        }
        else
        {
            responseString = "<html><body><h1>OAuth Error</h1><p>No authorization code received.</p><p>You can close this window.</p></body></html>";
            response.StatusCode = 400;
        }

        byte[] buffer = Encoding.UTF8.GetBytes(responseString);
        response.ContentLength64 = buffer.Length;
        response.ContentType = "text/html";
        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        response.OutputStream.Close();
        
        _listener.Stop();
        _listener.Close();

        if (!string.IsNullOrEmpty(error))
        {
            Console.WriteLine($"OAuth error: {error}");
            return null;
        }

        if (string.IsNullOrEmpty(code))
        {
            Console.WriteLine("No authorization code received");
            return null;
        }

        if (returnedState != state)
        {
            Console.WriteLine("State parameter mismatch - possible CSRF attack");
            return null;
        }

        Console.WriteLine("Authorization code received, exchanging for token...");
        return await ExchangeCodeForTokenAsync(code);
    }

    private async Task<string?> ExchangeCodeForTokenAsync(string code)
    {
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("client_id", _config.ClientId),
            new KeyValuePair<string, string>("client_secret", _config.ClientSecret),
            new KeyValuePair<string, string>("redirect_uri", _config.RedirectUri),
            new KeyValuePair<string, string>("code", code)
        });
        
        var response = await client.PostAsync(_config.TokenUrl, content);
        var body = await response.Content.ReadAsStringAsync();
        
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Token exchange failed: {response.StatusCode}");
            Console.WriteLine($"Response: {body}");
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("access_token").GetString();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to parse token response: {ex.Message}");
            Console.WriteLine($"Response: {body}");
            return null;
        }
    }
}
