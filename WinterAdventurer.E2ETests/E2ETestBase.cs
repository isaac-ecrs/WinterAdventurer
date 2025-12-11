// <copyright file="E2ETestBase.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using Microsoft.Playwright;
using Microsoft.Playwright.MSTest;
using OfficeOpenXml;

namespace WinterAdventurer.E2ETests;

/// <summary>
/// Base class for all E2E tests. Provides common setup, teardown, and helper methods
/// for Playwright-based end-to-end testing.
/// </summary>
public abstract class E2ETestBase : PageTest
{
    /// <summary>
    /// Gets base URL for the application under test.
    /// Managed by WebServerManager which handles server startup/shutdown.
    /// Port can be overridden via E2E_PORT environment variable (default: 5004).
    /// URL can be overridden via E2E_BASE_URL environment variable (for external servers).
    /// </summary>
    protected static string BaseUrl => Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? WebServerManager.BaseUrl;

    /// <summary>
    /// Test initialization. Navigates to base URL and clears any previous test state.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    [TestInitialize]
    public async Task BaseSetup()
    {
        // Set tour as completed before page loads to prevent tour from starting
        await Page.AddInitScriptAsync("localStorage.setItem('tour_home_completed', 'true')");
        await Page.GotoAsync(BaseUrl);
    }

    /// <summary>
    /// Uploads an Excel file to the application and waits for processing to complete.
    /// </summary>
    /// <param name="package">Excel package to upload.</param>
    /// <param name="waitForWorkshops">If true, waits for workshops to load after upload.</param>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    protected async Task UploadTestExcelFile(ExcelPackage package, bool waitForWorkshops = true)
    {
        // Save package to temporary file
        var tempPath = Path.Combine(Path.GetTempPath(), $"test-{Guid.NewGuid()}.xlsx");

        try
        {
            using (var fileStream = File.Create(tempPath))
            {
                package.SaveAs(fileStream);
            }

            // Find file input and upload
            // Note: MudBlazor file inputs are hidden, so we need to locate them without requiring visibility
            var fileInput = await Page.Locator("input[type='file']").First.ElementHandleAsync();
            if (fileInput == null)
            {
                throw new InvalidOperationException("File upload input not found on page");
            }

            await fileInput.SetInputFilesAsync(tempPath);

            // Wait for upload to process
            if (waitForWorkshops)
            {
                await WaitForWorkshopsLoaded(expectedCount: 1);
            }
            else
            {
                // Give it a moment to process
                await Page.WaitForTimeoutAsync(1000);
            }
        }
        finally
        {
            // Clean up temp file
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    /// <summary>
    /// Clears the tour completion state from localStorage.
    /// Prevents tour from interfering with E2E tests.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    protected async Task ClearTourState()
    {
        await Page.EvaluateAsync("localStorage.removeItem('tour_home_completed')");
    }

    /// <summary>
    /// Waits for workshops to be loaded and displayed on the page.
    /// </summary>
    /// <param name="expectedCount">Minimum number of workshops expected. Defaults to 1.</param>
    /// <param name="timeoutMs">Timeout in milliseconds. Defaults to 5000ms.</param>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous operation.</placeholder></returns>
    protected async Task WaitForWorkshopsLoaded(int expectedCount = 1, int timeoutMs = 5000)
    {
        // Wait for workshop grid to appear
        await Page.WaitForSelectorAsync("#workshop-grid", new () { Timeout = timeoutMs });

        // Wait for at least the expected number of workshop cards
        var selector = ".mud-card";
        await Page.WaitForFunctionAsync(
            $"document.querySelectorAll('{selector}').length >= {expectedCount}",
            new PageWaitForFunctionOptions { Timeout = timeoutMs });
    }

    /// <summary>
    /// Gets the current number of workshops displayed on the page.
    /// </summary>
    /// <returns>Number of workshop cards found.</returns>
    protected async Task<int> GetWorkshopCount()
    {
        var cards = await Page.QuerySelectorAllAsync(".mud-card");
        return cards.Count;
    }

    /// <summary>
    /// Creates a valid Excel package with test data for upload.
    /// Follows the same pattern as existing ExcelParsingTests.
    /// </summary>
    /// <param name="workshopCount">Number of workshops to create. Defaults to 1.</param>
    /// <returns>ExcelPackage ready for upload.</returns>
    protected ExcelPackage CreateValidExcelPackage(int workshopCount = 1)
    {
        ExcelPackage.License.SetNonCommercialOrganization("WinterAdventurer");
        var package = new ExcelPackage();

        // Add ClassSelection sheet
        var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
        AddClassSelectionHeaders(classSelection);

        // Add attendees (one per workshop plus some extras)
        int attendeeCount = workshopCount + 2;
        for (int i = 1; i <= attendeeCount; i++)
        {
            classSelection.Cells[i + 1, 1].Value = $"SEL{i:D3}";
            classSelection.Cells[i + 1, 2].Value = $"FirstName{i}";
            classSelection.Cells[i + 1, 3].Value = $"LastName{i}";
            classSelection.Cells[i + 1, 4].Value = $"test{i}@example.com";
            classSelection.Cells[i + 1, 5].Value = 20 + i;
        }

        // Add MorningFirstPeriod sheet with workshops
        var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
        AddPeriodSheetHeaders(periodSheet);

        var workshopNames = new[] { "Pottery", "Woodworking", "Cooking", "Painting", "Gardening" };
        var leaderNames = new[] { "John Smith", "Jane Doe", "Bob Johnson", "Alice Williams", "Carol Davis" };

        for (int i = 1; i <= workshopCount; i++)
        {
            int row = i + 1;
            periodSheet.Cells[row, 1].Value = $"SEL{i:D3}";
            periodSheet.Cells[row, 2].Value = $"FirstName{i}";
            periodSheet.Cells[row, 3].Value = $"LastName{i}";
            periodSheet.Cells[row, 5].Value = i; // Registration ID
            periodSheet.Cells[row, 6].Value = $"{workshopNames[(i - 1) % workshopNames.Length]} ({leaderNames[(i - 1) % leaderNames.Length]})";
            periodSheet.Cells[row, 7].Value = "1"; // Choice number (first choice)
        }

        return package;
    }

    /// <summary>
    /// Adds standard ClassSelection sheet headers matching the schema.
    /// </summary>
    private void AddClassSelectionHeaders(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "ClassSelection_Id";
        sheet.Cells[1, 2].Value = "Name_First";
        sheet.Cells[1, 3].Value = "Name_Last";
        sheet.Cells[1, 4].Value = "Email";
        sheet.Cells[1, 5].Value = "Age";
    }

    /// <summary>
    /// Adds standard period sheet headers matching the schema.
    /// </summary>
    private void AddPeriodSheetHeaders(ExcelWorksheet sheet)
    {
        sheet.Cells[1, 1].Value = "ClassSelection_Id";
        sheet.Cells[1, 2].Value = "AttendeeName_First";
        sheet.Cells[1, 3].Value = "AttendeeName_Last";
        sheet.Cells[1, 4].Value = "AttendeeName";
        sheet.Cells[1, 5].Value = "2024WinterAdventureClassRegist_Id"; // Registration ID with year prefix
        sheet.Cells[1, 6].Value = "_4dayClasses";
        sheet.Cells[1, 7].Value = "ChoiceNumber";
        sheet.Cells[1, 8].Value = "_2dayClassesFirst2Days";
        sheet.Cells[1, 9].Value = "_2dayClassesSecond2Days";
    }
}
