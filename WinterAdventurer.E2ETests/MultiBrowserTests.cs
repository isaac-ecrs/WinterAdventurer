// <copyright file="MultiBrowserTests.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

namespace WinterAdventurer.E2ETests;

/// <summary>
/// Multi-browser E2E tests. These tests verify that critical workflows
/// work correctly across different browsers (Chromium and Firefox).
///
/// Note: Playwright MSTest runs tests with the browser configured in runsettings.
/// To test multiple browsers, run the test suite multiple times with different configurations
/// or use the Playwright CLI to specify browsers.
///
/// Example:
///   BROWSER=chromium dotnet test
///   BROWSER=firefox dotnet test.
/// </summary>
[TestClass]
public class MultiBrowserTests : E2ETestBase
{
    /// <summary>
    /// Tests that file upload works correctly across browsers.
    /// This is a critical workflow that may behave differently in different browsers.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [TestMethod]
    public async Task FileUpload_UploadsSuccessfully()
    {
        // Arrange
        var package = CreateValidExcelPackage(workshopCount: 2);

        // Act
        await UploadTestExcelFile(package, waitForWorkshops: true);

        // Assert - Verify workshops loaded
        var workshopGrid = await Page.QuerySelectorAsync("#workshop-grid");
        Assert.IsNotNull(workshopGrid, "Workshop grid should be visible after upload");

        int workshopCount = await GetWorkshopCount();
        Assert.IsTrue(workshopCount >= 2, $"Should have at least 2 workshops, found {workshopCount}");
    }

    /// <summary>
    /// Tests that PDF generation and download works across browsers.
    /// Download behavior can vary significantly between browsers.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [TestMethod]
    public async Task PdfGeneration_Success()
    {
        // Arrange
        var package = CreateValidExcelPackage(workshopCount: 1);
        await UploadTestExcelFile(package);
        await WaitForWorkshopsLoaded();

        // Act - Generate PDF
        var downloadTask = Page.WaitForDownloadAsync();
        var pdfButton = await Page.QuerySelectorAsync("button:has-text('Create PDF')");
        Assert.IsNotNull(pdfButton, "Create PDF button should exist");

        await pdfButton.ClickAsync();

        // Assert - Verify download started
        var download = await downloadTask;
        Assert.IsNotNull(download, "PDF download should be triggered");
        Assert.IsTrue(
            download.SuggestedFilename.EndsWith(".pdf"),
            $"Download should be a PDF file, got: {download.SuggestedFilename}");
    }

    /// <summary>
    /// Tests that interactive UI elements (autocomplete, inputs) work across browsers.
    /// MudBlazor components may render differently in different browsers.
    /// </summary>
    /// <returns><placeholder>A <see cref="Task"/> representing the asynchronous unit test.</placeholder></returns>
    [TestMethod]
    public async Task WorkshopInteraction_FunctionsCorrectly()
    {
        // Arrange
        var package = CreateValidExcelPackage(workshopCount: 1);
        await UploadTestExcelFile(package);
        await WaitForWorkshopsLoaded();

        // Act - Interact with location autocomplete
        var locationInput = await Page.QuerySelectorAsync("#first-workshop-location input");
        if (locationInput != null)
        {
            // Type into autocomplete
            await locationInput.FillAsync("Test Location");

            // Wait a moment for the value to be set
            await Page.WaitForTimeoutAsync(100);

            // Verify text was entered using InputValueAsync (proper method for input elements)
            var value = await locationInput.InputValueAsync();
            Assert.AreEqual("Test Location", value, "Location input should update with typed value");
        }
        else
        {
            Assert.Fail("Location input not found - workshop card may not have rendered correctly");
        }

        // Act - Interact with leader name input
        var leaderInput = await Page.QuerySelectorAsync("#first-workshop-leader input");
        if (leaderInput != null)
        {
            var initialValue = await leaderInput.InputValueAsync();
            Assert.IsFalse(string.IsNullOrEmpty(initialValue), "Leader name should be populated from Excel");
        }

        Assert.IsTrue(true, "Workshop interaction completed successfully");
    }
}
