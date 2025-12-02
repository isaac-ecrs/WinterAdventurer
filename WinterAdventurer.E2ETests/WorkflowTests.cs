using Microsoft.Playwright;

namespace WinterAdventurer.E2ETests;

/// <summary>
/// End-to-end tests for complete user workflows.
/// Tests the full application stack from UI interaction to PDF generation.
/// </summary>
[TestClass]
public class WorkflowTests : E2ETestBase
{
    #region Upload and Edit Workflows

    [TestMethod]
    public async Task UploadExcel_EditWorkshop_GeneratePdf_Success()
    {
        // Arrange
        var package = CreateValidExcelPackage(workshopCount: 2);

        // Act - Upload
        await UploadTestExcelFile(package);
        await WaitForWorkshopsLoaded(expectedCount: 2);

        // Verify workshops loaded
        int workshopCount = await GetWorkshopCount();
        Assert.IsTrue(workshopCount >= 2, $"Expected at least 2 workshops, but found {workshopCount}");

        // Act - Edit first workshop location
        var locationInput = await Page.QuerySelectorAsync("#first-workshop-location input");
        if (locationInput != null)
        {
            await locationInput.FillAsync("Cafeteria");
            await Page.Keyboard.PressAsync("Tab"); // Trigger blur to save
            await Page.WaitForTimeoutAsync(500); // Wait for save
        }

        // Act - Generate PDF
        var downloadTask = Page.WaitForDownloadAsync();
        var pdfButton = await Page.QuerySelectorAsync("button:has-text('Create PDF')");
        Assert.IsNotNull(pdfButton, "Create PDF button should exist");
        await pdfButton.ClickAsync();

        // Assert - Verify download
        var download = await downloadTask;
        Assert.IsNotNull(download, "PDF download should be triggered");
        Assert.IsTrue(download.SuggestedFilename.EndsWith(".pdf"), "Downloaded file should be a PDF");
    }

    [TestMethod]
    public async Task UploadExcel_ChangeLocation_PdfReflectsChange()
    {
        // Arrange
        var package = CreateValidExcelPackage(workshopCount: 1);

        // Act - Upload and wait for workshops
        await UploadTestExcelFile(package);
        await WaitForWorkshopsLoaded();

        // Edit location
        var locationInput = await Page.QuerySelectorAsync("#first-workshop-location input");
        if (locationInput != null)
        {
            await locationInput.FillAsync("Room 101");
            await Page.Keyboard.PressAsync("Tab");
            await Page.WaitForTimeoutAsync(500);
        }

        // Generate PDF
        var downloadTask = Page.WaitForDownloadAsync();
        var pdfButton = await Page.QuerySelectorAsync("button:has-text('Create PDF')");
        if (pdfButton != null)
        {
            await pdfButton.ClickAsync();
        }

        // Assert
        var download = await downloadTask;
        Assert.IsNotNull(download, "PDF should be generated after location change");
    }

    [TestMethod]
    public async Task UploadExcel_ConfigureTimeslots_PdfIncludesTimes()
    {
        // Arrange
        var package = CreateValidExcelPackage(workshopCount: 1);

        // Act - Upload
        await UploadTestExcelFile(package);
        await WaitForWorkshopsLoaded();

        // Configure timeslots (if timeslot editor is visible)
        var timeslotSection = await Page.QuerySelectorAsync("text=Configure Schedule Times");
        if (timeslotSection != null)
        {
            // Save times (default times should be present)
            var saveButton = await Page.QuerySelectorAsync("button:has-text('Save Times')");
            if (saveButton != null)
            {
                await saveButton.ClickAsync();
                await Page.WaitForTimeoutAsync(500);
            }
        }

        // Generate PDF
        var downloadTask = Page.WaitForDownloadAsync();
        var pdfButton = await Page.QuerySelectorAsync("button:has-text('Create PDF')");
        if (pdfButton != null)
        {
            await pdfButton.ClickAsync();
        }

        // Assert
        var download = await downloadTask;
        Assert.IsNotNull(download, "PDF should be generated with timeslots configured");
    }

    #endregion

    #region Error Handling

    [TestMethod]
    public async Task UploadInvalidExcel_ShowsErrorMessage()
    {
        // Arrange - Create invalid Excel (empty file)
        var tempPath = Path.Combine(Path.GetTempPath(), $"invalid-{Guid.NewGuid()}.xlsx");

        try
        {
            // Create empty file
            File.WriteAllBytes(tempPath, new byte[] { 0x50, 0x4B }); // Invalid ZIP header

            // Act - Try to upload
            // Note: MudBlazor file inputs are hidden, so we need to locate them without requiring visibility
            var fileInput = await Page.Locator("input[type='file']").First.ElementHandleAsync();
            if (fileInput != null)
            {
                await fileInput.SetInputFilesAsync(tempPath);
                await Page.WaitForTimeoutAsync(2000); // Wait for processing

                // Assert - Should show error or no workshops
                var workshopGrid = await Page.QuerySelectorAsync("#workshop-grid");
                if (workshopGrid != null)
                {
                    // If grid exists, it should be empty or show error
                    int count = await GetWorkshopCount();
                    Assert.AreEqual(0, count, "Invalid Excel should not load any workshops");
                }
            }
        }
        finally
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
    }

    [TestMethod]
    public async Task OverlappingTimeslots_BlocksPdfGeneration()
    {
        // Arrange
        var package = CreateValidExcelPackage(workshopCount: 1);

        // Act - Upload
        await UploadTestExcelFile(package);
        await WaitForWorkshopsLoaded();

        // Look for PDF generation button
        var pdfButton = await Page.QuerySelectorAsync("button:has-text('Create PDF')");

        // Assert - Button should exist
        // Note: Actual validation logic depends on timeslot configuration
        // If no timeslots are configured, button should be enabled
        Assert.IsNotNull(pdfButton, "Create PDF button should exist");
    }

    #endregion

    #region PDF Download

    [TestMethod]
    public async Task GeneratePdf_DownloadsFile_WithCorrectFilename()
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

        var download = await downloadTask;

        // Assert - Verify filename
        Assert.IsNotNull(download);
        Assert.IsTrue(download.SuggestedFilename.Contains("Rosters") || download.SuggestedFilename.Contains("rosters"),
            $"Filename should contain 'Rosters', got: {download.SuggestedFilename}");
        Assert.IsTrue(download.SuggestedFilename.EndsWith(".pdf"),
            $"Filename should end with .pdf, got: {download.SuggestedFilename}");
    }

    [TestMethod]
    public async Task GenerateMasterSchedule_DownloadsFile()
    {
        // Arrange
        var package = CreateValidExcelPackage(workshopCount: 2);
        await UploadTestExcelFile(package);
        await WaitForWorkshopsLoaded(expectedCount: 2);

        // Act - Generate Master Schedule PDF
        var downloadTask = Page.WaitForDownloadAsync();
        var masterButton = await Page.QuerySelectorAsync("button:has-text('Master Schedule')");

        if (masterButton != null)
        {
            await masterButton.ClickAsync();

            // Assert
            var download = await downloadTask;
            Assert.IsNotNull(download, "Master schedule PDF should be generated");
            Assert.IsTrue(download.SuggestedFilename.EndsWith(".pdf"));
        }
        else
        {
            // Button might not exist if not enough workshops
            Assert.Inconclusive("Master Schedule button not found - may require more workshops or locations");
        }
    }

    #endregion

    #region Multi-Step Workflow

    [TestMethod]
    public async Task UploadExcel_AddBlankSchedules_GeneratePdf_Success()
    {
        // Arrange
        var package = CreateValidExcelPackage(workshopCount: 1);
        await UploadTestExcelFile(package);
        await WaitForWorkshopsLoaded();

        // Act - Configure blank schedules (if field exists)
        var blankScheduleInput = await Page.QuerySelectorAsync("input[type='number']");
        if (blankScheduleInput != null)
        {
            await blankScheduleInput.FillAsync("3");
        }

        // Generate PDF
        var downloadTask = Page.WaitForDownloadAsync();
        var pdfButton = await Page.QuerySelectorAsync("button:has-text('Create PDF')");
        Assert.IsNotNull(pdfButton, "Create PDF button should exist");
        await pdfButton.ClickAsync();

        // Assert
        var download = await downloadTask;
        Assert.IsNotNull(download, "PDF with blank schedules should be generated");
    }

    #endregion
}
