using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WinterAdventurer.E2ETests;

/// <summary>
/// Assembly-level setup and teardown for E2E tests.
/// Manages the web server lifecycle for all tests.
/// </summary>
[TestClass]
public class AssemblySetup
{
    /// <summary>
    /// Runs once before any tests in the assembly.
    /// Starts the web server and waits for it to be ready.
    /// </summary>
    [AssemblyInitialize]
    public static async Task AssemblyInit(TestContext context)
    {
        Console.WriteLine("=== E2E Test Assembly Initialization ===");
        await WebServerManager.StartServerAsync();
        Console.WriteLine("=== Server Ready - Starting Tests ===");
    }

    /// <summary>
    /// Runs once after all tests in the assembly complete.
    /// Stops the web server and cleans up resources.
    /// </summary>
    [AssemblyCleanup]
    public static void AssemblyCleanup()
    {
        Console.WriteLine("=== E2E Test Assembly Cleanup ===");
        WebServerManager.StopServer();
        Console.WriteLine("=== Cleanup Complete ===");
    }
}
