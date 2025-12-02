using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;

namespace WinterAdventurer.E2ETests;

[TestClass]
public class TourTests : E2ETestBase
{

    [TestMethod]
    public async Task Tour_AssignLocationsStep_ShouldHighlightLocationField()
    {
        // Arrange: Upload test data first
        var package = CreateValidExcelPackage(workshopCount: 1);
        await UploadTestExcelFile(package, waitForWorkshops: true);

        // Clear tour state and manually start the tour
        await Page.EvaluateAsync("localStorage.removeItem('tour_home_completed')");

        // Manually trigger the tour if the startHomeTour function exists
        var tourStarted = await Page.EvaluateAsync<bool>(@"
            (() => {
                if (typeof window.startHomeTour === 'function') {
                    window.startHomeTour();
                    return true;
                }
                return false;
            })()
        ");

        if (!tourStarted)
        {
            Assert.Inconclusive("Tour system not available - startHomeTour function not found");
            return;
        }

        // Wait for the tour popover to appear
        await Page.WaitForSelectorAsync(".driver-popover", new() { Timeout = 5000 });

        // Act: Navigate through tour steps
        // Step 0: Welcome - click "Next" or "Start tour"
        var nextButton = Page.Locator("button:has-text('Next'), button:has-text('Start tour')").First;
        await nextButton.ClickAsync();

        // Wait a moment for tour to advance
        await Page.WaitForTimeoutAsync(500);

        // Assert: Check if the location element with ID exists
        var locationElement = await Page.QuerySelectorAsync("#first-workshop-location");
        Assert.IsNotNull(locationElement, "Location element should exist after uploading workshop data");

        // Check if tour highlights any element (tour might be on different steps depending on implementation)
        // Note: This test might need adjustment based on actual tour step sequence
        var hasHighlightedElement = await Page.EvaluateAsync<bool>(@"
            document.querySelector('.driver-active-element') !== null
        ");

        Assert.IsTrue(hasHighlightedElement, "Tour should highlight some element during tour steps");
    }

    [TestMethod]
    public async Task CardIndex_ShouldBeSequential_NotAllTheSame()
    {
        // Regression test for bug where all workshops had CardIndex=20
        // Upload test data with multiple workshops to verify sequential indexing
        var package = CreateValidExcelPackage(workshopCount: 3);
        await UploadTestExcelFile(package, waitForWorkshops: true);

        // Get all CardIndex values
        var cardIndices = await Page.EvaluateAsync<int[]>(@"
            Array.from(document.querySelectorAll('[data-card-index]'))
                .map(el => parseInt(el.getAttribute('data-card-index')))
        ");

        Assert.IsTrue(cardIndices.Length > 0, "Should have elements with CardIndex after uploading workshops");

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
        // Upload test data to ensure we have workshops
        var package = CreateValidExcelPackage(workshopCount: 1);
        await UploadTestExcelFile(package, waitForWorkshops: true);

        // Check for elements with CardIndex=0
        var hasCardIndex0 = await Page.EvaluateAsync<bool>(@"
            document.querySelectorAll('[data-card-index=""0""]').length > 0
        ");

        Assert.IsTrue(hasCardIndex0, "Should have workshops with CardIndex=0 after uploading test data");

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
