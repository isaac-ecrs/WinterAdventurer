using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace WinterAdventurer.E2ETests;

/// <summary>
/// Manages the lifecycle of the WinterAdventurer web server for E2E testing.
/// Starts the server before tests run and ensures clean shutdown afterward.
/// </summary>
public static class WebServerManager
{
    private static Process? _serverProcess;
    private static readonly string _projectPath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "..",
        "..",
        "..",
        "..",
        "WinterAdventurer"
    );

    /// <summary>
    /// Port to run the test server on. Can be overridden via E2E_PORT environment variable.
    /// </summary>
    public static int Port => int.TryParse(Environment.GetEnvironmentVariable("E2E_PORT"), out var port)
        ? port
        : 5004;

    /// <summary>
    /// Base URL for the running test server.
    /// </summary>
    public static string BaseUrl => $"http://localhost:{Port}";

    /// <summary>
    /// Starts the web server and waits for it to become responsive.
    /// </summary>
    public static async Task StartServerAsync()
    {
        // Check if something is already running on the port
        if (IsPortInUse(Port))
        {
            Console.WriteLine($"[WebServerManager] Port {Port} is already in use. Assuming server is already running.");

            // Verify it's responding
            if (await WaitForServerReadyAsync(timeoutSeconds: 5))
            {
                Console.WriteLine("[WebServerManager] Existing server is responsive. Using it for tests.");
                return;
            }

            throw new InvalidOperationException(
                $"Port {Port} is in use but server is not responding. " +
                "Please stop the process using this port and try again.");
        }

        Console.WriteLine($"[WebServerManager] Starting web server at {BaseUrl}...");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --no-build",
            WorkingDirectory = _projectPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            Environment =
            {
                ["ASPNETCORE_URLS"] = BaseUrl,
                ["ASPNETCORE_ENVIRONMENT"] = "Development"
            }
        };

        _serverProcess = Process.Start(startInfo);

        if (_serverProcess == null)
        {
            throw new InvalidOperationException("Failed to start web server process");
        }

        // Capture output for debugging
        _serverProcess.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Console.WriteLine($"[Server] {e.Data}");
            }
        };

        _serverProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                Console.WriteLine($"[Server Error] {e.Data}");
            }
        };

        _serverProcess.BeginOutputReadLine();
        _serverProcess.BeginErrorReadLine();

        // Wait for server to be ready
        if (!await WaitForServerReadyAsync(timeoutSeconds: 60))
        {
            StopServer();
            throw new TimeoutException(
                $"Web server did not become ready within 60 seconds at {BaseUrl}");
        }

        Console.WriteLine($"[WebServerManager] Server is ready at {BaseUrl}");
    }

    /// <summary>
    /// Stops the web server if it was started by this manager.
    /// </summary>
    public static void StopServer()
    {
        if (_serverProcess == null)
        {
            return;
        }

        Console.WriteLine("[WebServerManager] Stopping web server...");

        try
        {
            if (!_serverProcess.HasExited)
            {
                _serverProcess.Kill(entireProcessTree: true);
                _serverProcess.WaitForExit(5000);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[WebServerManager] Error stopping server: {ex.Message}");
        }
        finally
        {
            _serverProcess?.Dispose();
            _serverProcess = null;
        }

        Console.WriteLine("[WebServerManager] Server stopped");
    }

    /// <summary>
    /// Checks if a TCP port is currently in use.
    /// </summary>
    private static bool IsPortInUse(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            listener.Stop();
            return false;
        }
        catch (SocketException)
        {
            return true;
        }
    }

    /// <summary>
    /// Waits for the server to respond to HTTP requests.
    /// </summary>
    private static async Task<bool> WaitForServerReadyAsync(int timeoutSeconds)
    {
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var response = await httpClient.GetAsync(BaseUrl);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
            }
            catch
            {
                // Server not ready yet, continue waiting
            }

            await Task.Delay(500);
        }

        return false;
    }
}
