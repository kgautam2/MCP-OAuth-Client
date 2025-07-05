# MCP OAuth Client for GitHub

A robust C# implementation of OAuth 2.0 authentication and Server-Sent Events (SSE) client for connecting to GitHub's Model Context Protocol (MCP) server.

## Features

- ✅ **OAuth 2.0 Flow**: Complete authorization code flow with GitHub
- ✅ **SSE Connection**: Real-time bidirectional communication with GitHub's MCP server
- ✅ **Configuration Management**: Secure credential storage in `appsettings.json`
- ✅ **Auto Browser Launch**: Automatic OAuth authorization page opening
- ✅ **JSON-RPC 2.0**: MCP protocol message handling
- ✅ **Error Handling**: Robust error handling and resource cleanup

## Prerequisites

- .NET 9.0 SDK
- GitHub OAuth App (Client ID and Secret)

## Setup

1. **Create GitHub OAuth App**:
   - Go to https://github.com/settings/applications/new
   - Set **Authorization callback URL** to: `http://localhost:8080/callback`
   - Copy your Client ID and Client Secret

2. **Configure Application**:
   - Update `MCPHostApp/appsettings.json` with your credentials:
   ```json
   {
     "GitHub": {
       "ClientId": "YOUR_CLIENT_ID",
       "ClientSecret": "YOUR_CLIENT_SECRET",
       "McpServerUrl": "https://api.githubcopilot.com/mcp/"
     },
     "OAuth": {
       "CallbackUrl": "http://localhost:8080/callback",
       "Scope": "read:user user:email"
     }
   }
   ```

## Usage

1. **Build the project**:
   ```bash
   dotnet build
   ```

2. **Run the application**:
   ```bash
   dotnet run --project MCPHostApp
   ```

3. **OAuth Flow**:
   - Browser opens to GitHub authorization page
   - Click "Authorize" to grant permissions
   - Application automatically captures the authorization code
   - Access token is exchanged and displayed

4. **MCP Connection**:
   - Application connects to GitHub's MCP SSE server
   - Receives real-time JSON-RPC 2.0 messages
   - Displays incoming ping messages from the server

## Example Output

```
=== MCP OAuth Setup ===
Using Client ID: Ov23liKEq1iuwRuRcLwt
Starting OAuth flow...
Listening for callback on http://localhost:8080/callback
Opening browser to: https://github.com/login/oauth/authorize?...

🎉 OAuth Flow Successful!
===============================
✅ Access Token: gho_xxxxxxxxxxxxxxxxxxxxxxxxxxxx
📅 Token obtained at: 2025-07-05 12:38:19
🔑 Token length: 40 characters
🏷️  Token type: GitHub Personal Access Token
===============================

Connecting to GitHub MCP server...
✅ Successfully connected to GitHub MCP SSE server!
📡 Listening for SSE events...
🎯 Event type: message
📨 Received MCP message: {"jsonrpc":"2.0","id":1,"method":"ping"}
```

## Project Structure

```
MCP/
├── MCP.sln
├── MCPHostApp/
│   ├── MCPHostApp.csproj
│   ├── Program.cs              # Main application entry point
│   ├── OAuthHelper.cs          # OAuth 2.0 flow implementation
│   └── appsettings.json        # Configuration file
└── README.md
```

## Key Components

- **OAuthHelper.cs**: Handles OAuth 2.0 authorization code flow with local HTTP listener
- **Program.cs**: Main application logic, SSE connection, and MCP message handling
- **appsettings.json**: Secure configuration storage for OAuth credentials

## Testing with curl

You can also test the GitHub API directly with curl:

```bash
# Get user info
curl -H "Authorization: Bearer YOUR_TOKEN" https://api.github.com/user

# Connect to MCP SSE server
curl -H "Authorization: Bearer YOUR_TOKEN" \
     -H "Accept: text/event-stream" \
     https://api.githubcopilot.com/mcp/sse
```

## License

This project is open source and available under the MIT License.
