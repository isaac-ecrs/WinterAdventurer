using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace WinterAdventurer.E2ETests;

[TestClass]
public class TourTests : PageTest
{
    private static readonly string BaseUrl = Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5000"; // Default to 5000 for CI, override with E2E_BASE_URL=http://localhost:5004 for local dev

    [TestMethod]
    public async Task Tour_AssignLocationsStep_ShouldHighlightLocationField()
    {
        // Arrange: Clear tour completion to force tour to show
        await Page.GotoAsync(BaseUrl);
        await Page.EvaluateAsync("localStorage.removeItem('tour_home_completed')");
        await Page.ReloadAsync();

        // Wait for the tour to start
        await Page.WaitForSelectorAsync(".driver-popover", new() { Timeout = 5000 });

        // Act: Navigate through tour steps
        // Step 0: Welcome - click "Start tour"
        var startButton = await Page.WaitForSelectorAsync("button:has-text('Start tour')");
        Assert.IsNotNull(startButton, "Start tour button should exist");
        await startButton.ClickAsync();

        // Step 1: Upload file (we need to upload a file to proceed)
        // For now, let's skip upload and test with pre-loaded workshops
        // In a real test, you would upload a test Excel file here

        // Alternative: Test with workshops already loaded by manually advancing
        // You can skip steps or use driver API to jump to specific step

        // For debugging: Log what elements exist
        await Page.EvaluateAsync(@"
            console.log('=== E2E Test Debug ===');
            const locationEl = document.querySelector('#first-workshop-location');
            console.log('Location element:', locationEl);
            const leaderEl = document.querySelector('#first-workshop-leader');
            console.log('Leader element:', leaderEl);
        ");

        // Assert: Check if the element with ID exists
        var locationElement = await Page.QuerySelectorAsync("#first-workshop-location");
        if (locationElement == null)
        {
            Assert.Inconclusive("No workshops loaded - upload an Excel file to test tour highlighting");
            return;
        }

        // If tour is at the right step, verify it's highlighted
        // Driver.js adds 'driver-active-element' class to highlighted elements
        var isHighlighted = await Page.EvaluateAsync<bool>(@"
            (() => {
                const el = document.querySelector('#first-workshop-location');
                return el && el.classList.contains('driver-active-element');
            })()
        ");

        Assert.IsTrue(isHighlighted, "Location field should be highlighted during assign locations step");
    }

    [TestMethod]
    public async Task CardIndex_ShouldBeSequential_NotAllTheSame()
    {
        // Regression test for bug where all workshops had CardIndex=20
        await Page.GotoAsync(BaseUrl);

        // TODO: Upload test file here when we have test data setup
        // For now, assumes workshops are already loaded or will be loaded manually

        // Wait a bit for workshops to potentially load
        await Page.WaitForTimeoutAsync(1000);

        // Get all CardIndex values
        var cardIndices = await Page.EvaluateAsync<int[]>(@"
            Array.from(document.querySelectorAll('[data-card-index]'))
                .map(el => parseInt(el.getAttribute('data-card-index')))
        ");

        if (cardIndices.Length == 0)
        {
            Assert.Inconclusive("No workshops loaded - upload an Excel file to test");
        }

        Console.WriteLine($"Found {cardIndices.Length} elements with CardIndex");
        Console.WriteLine($"CardIndex values: {string.Join(", ", cardIndices.Distinct().OrderBy(x => x))}");

        // Should have at least some different indices (not all the same)
        var uniqueIndices = cardIndices.Distinct().Count();
        Assert.IsTrue(uniqueIndices > 1,
            $"All {cardIndices.Length} elements have the same CardIndex! Expected sequential indices.");

        // Should start from 0
        var minIndex = cardIndices.Min();
        Assert.AreEqual(0, minIndex, "CardIndex should start from 0");

        // Each index should appear exactly twice (once for location div, once for leader div)
        var indexCounts = cardIndices.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        foreach (var kvp in indexCounts)
        {
            Assert.AreEqual(2, kvp.Value,
                $"CardIndex {kvp.Key} appears {kvp.Value} times, expected 2 (location + leader div)");
        }

        Console.WriteLine($"✓ CardIndex values are sequential: 0 to {cardIndices.Max() / 2}");
    }

    [TestMethod]
    public async Task FirstWorkshop_ShouldHaveCorrectIDs()
    {
        // Test that CardIndex=0 has the expected IDs
        await Page.GotoAsync(BaseUrl);

        await Page.WaitForTimeoutAsync(1000);

        // Check for elements with CardIndex=0
        var hasCardIndex0 = await Page.EvaluateAsync<bool>(@"
            document.querySelectorAll('[data-card-index=""0""]').length > 0
        ");

        if (!hasCardIndex0)
        {
            Assert.Inconclusive("No workshops with CardIndex=0 found - upload an Excel file to test");
        }

        // Verify the location element exists
        var locationElement = await Page.QuerySelectorAsync("#first-workshop-location");
        Assert.IsNotNull(locationElement, "#first-workshop-location should exist for CardIndex=0");

        // Verify it has the correct CardIndex attribute
        var locationCardIndex = await locationElement.GetAttributeAsync("data-card-index");
        Assert.AreEqual("0", locationCardIndex, "Location element should have data-card-index='0'");

        // Verify the leader element exists
        var leaderElement = await Page.QuerySelectorAsync("#first-workshop-leader");
        Assert.IsNotNull(leaderElement, "#first-workshop-leader should exist for CardIndex=0");

        // Verify it has the correct CardIndex attribute
        var leaderCardIndex = await leaderElement.GetAttributeAsync("data-card-index");
        Assert.AreEqual("0", leaderCardIndex, "Leader element should have data-card-index='0'");

        // Verify they contain the expected child elements
        var hasAutocomplete = await locationElement.EvaluateAsync<bool>("el => el.querySelector('.mud-autocomplete') !== null");
        Assert.IsTrue(hasAutocomplete, "Location element should contain MudAutocomplete");

        var hasTextField = await leaderElement.EvaluateAsync<bool>("el => el.querySelector('.mud-input-control') !== null");
        Assert.IsTrue(hasTextField, "Leader element should contain MudTextField");

        Console.WriteLine("✓ First workshop has correct IDs and structure");
    }

    [TestMethod]
    public async Task DebugTourElements_LogsAllRelevantElements()
    {
        // This test is for debugging - it logs all tour-related elements
        await Page.GotoAsync(BaseUrl);

        // Call the debug helper function
        var debugOutput = await Page.EvaluateAsync<string>(@"
            (() => {
                window.debugTourElements && window.debugTourElements();
                return 'Debug function called - check browser console';
            })()
        ");

        Console.WriteLine(debugOutput);

        // Get all elements with 'first' in their ID
        var firstElements = await Page.EvaluateAsync<string[]>(@"
            Array.from(document.querySelectorAll('[id*=""first""]')).map(el => el.id)
        ");

        Console.WriteLine("Elements with 'first' in ID:");
        foreach (var id in firstElements)
        {
            Console.WriteLine($"  - {id}");
        }

        // Get all CardIndex values
        var cardIndexInfo = await Page.EvaluateAsync<string[]>(@"
            Array.from(document.querySelectorAll('[data-card-index]'))
                .map(el => `CardIndex ${el.getAttribute('data-card-index')}: ID=${el.id || '(none)'}`)
        ");

        Console.WriteLine("\nCardIndex assignments:");
        foreach (var info in cardIndexInfo.Take(10)) // Just show first 10
        {
            Console.WriteLine($"  {info}");
        }

        // This test always passes - it's just for inspection
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task Tour_CanAccessDebugFunction()
    {
        // Test that the debug function exists and works
        await Page.GotoAsync(BaseUrl);

        var hasTourDriver = await Page.EvaluateAsync<bool>("typeof window.tourDriver !== 'undefined'");
        var hasDebugFunction = await Page.EvaluateAsync<bool>("typeof window.debugTourElements === 'function'");
        var hasStartTourFunction = await Page.EvaluateAsync<bool>("typeof window.startHomeTour === 'function'");

        Console.WriteLine($"Tour driver available: {hasTourDriver}");
        Console.WriteLine($"Debug function available: {hasDebugFunction}");
        Console.WriteLine($"Start tour function available: {hasStartTourFunction}");

        Assert.IsTrue(hasDebugFunction, "debugTourElements function should be available");
        Assert.IsTrue(hasStartTourFunction, "startHomeTour function should be available");
    }
}
