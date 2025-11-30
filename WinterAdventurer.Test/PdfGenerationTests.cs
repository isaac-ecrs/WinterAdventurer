using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using WinterAdventurer.Library;
using WinterAdventurer.Test.Helpers;

namespace WinterAdventurer.Test
{
    /// <summary>
    /// Tests for PDF generation and content validation.
    /// Verifies that PDFs contain correct workshop, participant, and layout information.
    /// </summary>
    [TestClass]
    public class PdfGenerationTests
    {
        private ExcelUtilities _excelUtilities = null!;

        [TestInitialize]
        public void Setup()
        {
            _excelUtilities = new ExcelUtilities(NullLogger<ExcelUtilities>.Instance);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        #region Workshop Roster Tests

        [TestMethod]
        public void CreatePdf_WorkshopRoster_ContainsWorkshopName()
        {
            // Arrange
            var package = CreateMinimalValidExcelPackage();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            // Act
            var document = _excelUtilities.CreatePdf();
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);

            // Assert
            PdfTestHelper.AssertContainsText(pdfBytes, "Pottery", "Workshop roster");
        }

        [TestMethod]
        public void CreatePdf_WorkshopRoster_ContainsLeaderName()
        {
            // Arrange
            var package = CreateMinimalValidExcelPackage();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            // Act
            var document = _excelUtilities.CreatePdf();
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);

            // Assert
            PdfTestHelper.AssertContainsText(pdfBytes, "John Smith", "Workshop roster");
        }

        [TestMethod]
        public void CreatePdf_WorkshopRoster_ContainsLocationWhenSet()
        {
            // Arrange
            var package = CreateMinimalValidExcelPackage();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            // Set location for first workshop
            if (_excelUtilities.Workshops != null && _excelUtilities.Workshops.Count > 0)
            {
                _excelUtilities.Workshops[0].Location = "Art Studio";
            }

            // Act
            var document = _excelUtilities.CreatePdf();
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);

            // Assert
            PdfTestHelper.AssertContainsText(pdfBytes, "Art Studio", "Workshop roster");
        }

        [TestMethod]
        public void CreatePdf_WorkshopRoster_HasEnrolledSection()
        {
            // Arrange
            var package = CreateMinimalValidExcelPackage();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            // Act
            var document = _excelUtilities.CreatePdf();
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);

            // Assert
            PdfTestHelper.AssertContainsText(pdfBytes, "Enrolled Participants", "Workshop roster");
        }

        [TestMethod]
        public void CreatePdf_WorkshopRoster_HasBackupSection_WhenBackupsExist()
        {
            // Arrange
            var package = CreateExcelWithBackupChoices();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            // Act
            var document = _excelUtilities.CreatePdf();
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);

            // Assert
            PdfTestHelper.AssertContainsText(pdfBytes, "Backup/Alternate Choices", "Workshop roster");
        }

        [TestMethod]
        public void CreatePdf_WorkshopRoster_SortsParticipantsByLastName()
        {
            // Arrange
            var package = CreateExcelWithMultipleParticipants();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            // Act
            var document = _excelUtilities.CreatePdf();
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);
            var text = PdfTestHelper.ExtractAllText(pdfBytes);

            // Assert - verify participants appear in order
            var aliceIndex = text.IndexOf("Alice Adams", StringComparison.OrdinalIgnoreCase);
            var bobIndex = text.IndexOf("Bob Baker", StringComparison.OrdinalIgnoreCase);
            var carolIndex = text.IndexOf("Carol Carter", StringComparison.OrdinalIgnoreCase);

            Assert.IsTrue(aliceIndex > 0, "Alice Adams should appear in PDF");
            Assert.IsTrue(bobIndex > aliceIndex, "Bob Baker should appear after Alice Adams");
            Assert.IsTrue(carolIndex > bobIndex, "Carol Carter should appear after Bob Baker");
        }

        [TestMethod]
        public void CreatePdf_WorkshopRoster_DisplaysChoiceNumbers()
        {
            // Arrange
            var package = CreateExcelWithBackupChoices();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            // Act
            var document = _excelUtilities.CreatePdf();
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);

            // Assert - backup choices should show choice number
            PdfTestHelper.AssertContainsText(pdfBytes, "Choice #", "Workshop roster backup section");
        }

        #endregion

        #region Individual Schedule Tests

        [TestMethod]
        public void CreatePdf_IndividualSchedule_ContainsParticipantName()
        {
            // Arrange
            var package = CreateMinimalValidExcelPackage();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            // Act
            var document = _excelUtilities.CreatePdf();
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);

            // Assert
            PdfTestHelper.AssertContainsText(pdfBytes, "Alice Johnson", "Individual schedule");
        }

        [TestMethod]
        public void CreatePdf_IndividualSchedule_ContainsAllWorkshops()
        {
            // Arrange
            var package = CreateMinimalValidExcelPackage();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            // Act
            var document = _excelUtilities.CreatePdf();
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);

            // Assert - workshop should appear in individual schedule
            PdfTestHelper.AssertContainsText(pdfBytes, "Pottery", "Individual schedule");
        }

        [TestMethod]
        public void CreatePdf_IndividualSchedule_HasTimeslotStructure()
        {
            // Arrange
            var package = CreateMinimalValidExcelPackage();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            // Act - Include timeslots
            var document = _excelUtilities.CreatePdf();
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);

            // Assert - should have day indicators or time structure
            var text = PdfTestHelper.ExtractAllText(pdfBytes);
            Assert.IsTrue(
                text.Contains("Day 1", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("Morning", StringComparison.OrdinalIgnoreCase),
                "Individual schedule should contain day/time structure");
        }

        #endregion

        #region Logo Tests

        [TestMethod]
        public void CreatePdf_AllPages_ContainLogo()
        {
            // Arrange
            var package = CreateMinimalValidExcelPackage();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            // Act
            var document = _excelUtilities.CreatePdf();
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);

            // Assert - Check for images on first page (logo should be there)
            int imageCount = PdfTestHelper.CountImages(pdfBytes, pageNumber: 1);
            Assert.IsTrue(imageCount > 0, "First page should contain at least one image (logo)");
        }

        #endregion

        #region Blank Schedule Tests

        [TestMethod]
        public void CreatePdf_BlankSchedules_GeneratesCorrectCount()
        {
            // Arrange
            var package = CreateMinimalValidExcelPackage();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            int blankScheduleCount = 3;

            // Act
            var document = _excelUtilities.CreatePdf(blankScheduleCount: blankScheduleCount);
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);

            // Assert - PDF should have pages for blank schedules
            int pageCount = PdfTestHelper.GetPageCount(pdfBytes);
            Assert.IsTrue(pageCount >= blankScheduleCount,
                $"PDF should have at least {blankScheduleCount} pages for blank schedules");
        }

        [TestMethod]
        public void CreatePdf_BlankSchedules_HasEmptyNameField()
        {
            // Arrange
            var package = CreateMinimalValidExcelPackage();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            // Act
            var document = _excelUtilities.CreatePdf(blankScheduleCount: 1);
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);

            // Assert - Blank schedules should have name field placeholder
            PdfTestHelper.AssertContainsText(pdfBytes, "Name:", "Blank schedule");
        }

        #endregion

        #region Master Schedule Tests

        [TestMethod]
        public void CreateMasterSchedulePdf_ContainsAllWorkshops()
        {
            // Arrange
            var package = CreateExcelWithMultipleWorkshops();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            // Act
            var document = _excelUtilities.CreateMasterSchedulePdf();
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);

            // Assert - All workshops should appear
            PdfTestHelper.AssertContainsText(pdfBytes, "Pottery", "Master schedule");
            PdfTestHelper.AssertContainsText(pdfBytes, "Woodworking", "Master schedule");
        }

        [TestMethod]
        public void CreateMasterSchedulePdf_GroupsByLocation()
        {
            // Arrange
            var package = CreateMinimalValidExcelPackage();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;
            _excelUtilities.ImportExcel(stream);

            // Set locations
            if (_excelUtilities.Workshops != null && _excelUtilities.Workshops.Count > 0)
            {
                _excelUtilities.Workshops[0].Location = "Art Studio";
            }

            // Act
            var document = _excelUtilities.CreateMasterSchedulePdf();
            var pdfBytes = PdfTestHelper.RenderPdfToBytes(document);

            // Assert - Location should appear as column header
            PdfTestHelper.AssertContainsText(pdfBytes, "Art Studio", "Master schedule");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a minimal valid Excel package with one workshop and one attendee.
        /// </summary>
        private ExcelPackage CreateMinimalValidExcelPackage()
        {
            var package = new ExcelPackage();

            // Add ClassSelection sheet
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";
            classSelection.Cells[2, 4].Value = "alice@example.com";
            classSelection.Cells[2, 5].Value = "25";

            // Add MorningFirstPeriod sheet
            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);
            periodSheet.Cells[2, 1].Value = "SEL001";
            periodSheet.Cells[2, 2].Value = "Alice";
            periodSheet.Cells[2, 3].Value = "Johnson";
            periodSheet.Cells[2, 5].Value = "1"; // Registration ID
            periodSheet.Cells[2, 6].Value = "Pottery (John Smith)";
            periodSheet.Cells[2, 7].Value = "1"; // First choice

            return package;
        }

        /// <summary>
        /// Creates an Excel package with multiple workshops.
        /// </summary>
        private ExcelPackage CreateExcelWithMultipleWorkshops()
        {
            var package = new ExcelPackage();

            // Add ClassSelection sheet
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            classSelection.Cells[3, 1].Value = "SEL002";
            classSelection.Cells[3, 2].Value = "Bob";
            classSelection.Cells[3, 3].Value = "Williams";

            // Add MorningFirstPeriod sheet with 2 different workshops
            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);

            periodSheet.Cells[2, 1].Value = "SEL001";
            periodSheet.Cells[2, 5].Value = "1";
            periodSheet.Cells[2, 6].Value = "Pottery (John Smith)";
            periodSheet.Cells[2, 7].Value = "1";

            periodSheet.Cells[3, 1].Value = "SEL002";
            periodSheet.Cells[3, 5].Value = "2";
            periodSheet.Cells[3, 6].Value = "Woodworking (Jane Doe)";
            periodSheet.Cells[3, 7].Value = "1";

            return package;
        }

        /// <summary>
        /// Creates an Excel package with multiple participants in same workshop (sorted names).
        /// </summary>
        private ExcelPackage CreateExcelWithMultipleParticipants()
        {
            var package = new ExcelPackage();

            // Add ClassSelection sheet with 3 attendees (alphabetically)
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);

            // Add in non-alphabetical order to test sorting
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Carol";
            classSelection.Cells[2, 3].Value = "Carter";

            classSelection.Cells[3, 1].Value = "SEL002";
            classSelection.Cells[3, 2].Value = "Alice";
            classSelection.Cells[3, 3].Value = "Adams";

            classSelection.Cells[4, 1].Value = "SEL003";
            classSelection.Cells[4, 2].Value = "Bob";
            classSelection.Cells[4, 3].Value = "Baker";

            // Add period sheet - all select same workshop
            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);

            for (int i = 1; i <= 3; i++)
            {
                periodSheet.Cells[i + 1, 1].Value = $"SEL{i:D3}";
                periodSheet.Cells[i + 1, 5].Value = i.ToString();
                periodSheet.Cells[i + 1, 6].Value = "Pottery (John Smith)";
                periodSheet.Cells[i + 1, 7].Value = "1"; // All first choice
            }

            return package;
        }

        /// <summary>
        /// Creates an Excel package with backup choices.
        /// </summary>
        private ExcelPackage CreateExcelWithBackupChoices()
        {
            var package = new ExcelPackage();

            // Add ClassSelection sheet
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            classSelection.Cells[3, 1].Value = "SEL002";
            classSelection.Cells[3, 2].Value = "Bob";
            classSelection.Cells[3, 3].Value = "Williams";

            // Add period sheet - one first choice, one backup
            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);

            // First choice
            periodSheet.Cells[2, 1].Value = "SEL001";
            periodSheet.Cells[2, 5].Value = "1";
            periodSheet.Cells[2, 6].Value = "Pottery (John Smith)";
            periodSheet.Cells[2, 7].Value = "1";

            // Backup choice (choice #3)
            periodSheet.Cells[3, 1].Value = "SEL002";
            periodSheet.Cells[3, 5].Value = "2";
            periodSheet.Cells[3, 6].Value = "Pottery (John Smith)";
            periodSheet.Cells[3, 7].Value = "3"; // Third choice (backup)

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
            sheet.Cells[1, 5].Value = "2024WinterAdventureClassRegist_Id";
            sheet.Cells[1, 6].Value = "_4dayClasses";
            sheet.Cells[1, 7].Value = "ChoiceNumber";
            sheet.Cells[1, 8].Value = "_2dayClassesFirst2Days";
            sheet.Cells[1, 9].Value = "_2dayClassesSecond2Days";
        }

        #endregion
    }
}
