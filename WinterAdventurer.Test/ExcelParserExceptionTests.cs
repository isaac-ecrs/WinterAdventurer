using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using WinterAdventurer.Library.Services;
using WinterAdventurer.Library.Exceptions;

namespace WinterAdventurer.Test
{
    /// <summary>
    /// Tests for ExcelParser exception scenarios and error handling.
    /// Verifies that proper exceptions are thrown with helpful context when parsing fails.
    /// </summary>
    [TestClass]
    public class ExcelParserExceptionTests
    {
        private ExcelParser _parser = null!;

        [TestInitialize]
        public void Setup()
        {
            ExcelPackage.License.SetNonCommercialOrganization("WinterAdventurer");
            _parser = new ExcelParser(NullLogger<ExcelParser>.Instance);
        }

        #region MissingSheetException Tests

        [TestMethod]
        public void ParseFromStream_WithMissingClassSelectionSheet_ThrowsMissingSheetException()
        {
            // Arrange
            var package = new ExcelPackage();
            package.Workbook.Worksheets.Add("WrongSheetName");
            package.Workbook.Worksheets.Add("MorningFirstPeriod");

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            // Act & Assert
            try
            {
                _parser.ParseFromStream(stream);
                Assert.Fail("Should have thrown MissingSheetException");
            }
            catch (MissingSheetException ex)
            {
                Assert.IsTrue(ex.Message.Contains("ClassSelection"),
                    "Exception message should mention ClassSelection");
                Assert.IsNotNull(ex.AvailableSheets,
                    "AvailableSheets should be populated");
                Assert.IsTrue(ex.AvailableSheets.Count > 0,
                    "AvailableSheets should contain at least one sheet");
            }
        }

        [TestMethod]
        public void ParseFromStream_WithMissingPeriodSheet_ContinuesGracefully()
        {
            // Arrange - Only ClassSelection, missing period sheets
            var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            // Act
            var workshops = _parser.ParseFromStream(stream);

            // Assert - Should succeed with empty workshop list
            Assert.IsNotNull(workshops);
            Assert.AreEqual(0, workshops.Count,
                "Should return empty list when period sheets are missing");
        }

        #endregion

        // Note: ExcelParser is robust and handles many edge cases gracefully,
        // so many scenarios that might throw exceptions actually succeed.
        // The tests below verify the robust behavior.

        #region Edge Case Tests

        [TestMethod]
        public void ParseFromStream_WithEmptyWorkshopCell_SkipsGracefully()
        {
            // Arrange - Row with empty workshop cell
            var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);
            periodSheet.Cells[2, 1].Value = "SEL001";
            periodSheet.Cells[2, 6].Value = "";  // Empty workshop
            periodSheet.Cells[2, 7].Value = "1";

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            // Act
            var workshops = _parser.ParseFromStream(stream);

            // Assert - Should succeed with no workshops
            Assert.IsNotNull(workshops);
            Assert.AreEqual(0, workshops.Count,
                "Should skip rows with empty workshop cells");
        }

        [TestMethod]
        public void ParseFromStream_WithWhitespaceWorkshopCell_SkipsGracefully()
        {
            // Arrange - Row with whitespace-only workshop cell
            var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);
            periodSheet.Cells[2, 1].Value = "SEL001";
            periodSheet.Cells[2, 6].Value = "   ";  // Whitespace only
            periodSheet.Cells[2, 7].Value = "1";

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            // Act
            var workshops = _parser.ParseFromStream(stream);

            // Assert - Should succeed with no workshops
            Assert.IsNotNull(workshops);
            Assert.AreEqual(0, workshops.Count,
                "Should skip rows with whitespace-only workshop cells");
        }

        [TestMethod]
        public void ParseFromStream_WithVeryLongWorkshopName_HandlesCorrectly()
        {
            // Arrange - Workshop with very long name
            var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);
            periodSheet.Cells[2, 1].Value = "SEL001";
            string longName = new string('A', 500); // Very long name
            periodSheet.Cells[2, 6].Value = $"{longName} (John Smith)";
            periodSheet.Cells[2, 7].Value = "1";

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            // Act
            var workshops = _parser.ParseFromStream(stream);

            // Assert
            Assert.IsNotNull(workshops);
            Assert.AreEqual(1, workshops.Count);
            Assert.AreEqual(longName, workshops[0].Name,
                "Should handle very long workshop names");
        }

        [TestMethod]
        public void ParseFromStream_WithSpecialCharactersInWorkshop_HandlesCorrectly()
        {
            // Arrange - Workshop with special characters
            var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);
            periodSheet.Cells[2, 1].Value = "SEL001";
            periodSheet.Cells[2, 6].Value = "Pottery & Ceramics: Beginner's Class! (John O'Brien-Smith)";
            periodSheet.Cells[2, 7].Value = "1";

            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            // Act
            var workshops = _parser.ParseFromStream(stream);

            // Assert
            Assert.IsNotNull(workshops);
            Assert.AreEqual(1, workshops.Count);
            Assert.AreEqual("Pottery & Ceramics: Beginner's Class!", workshops[0].Name);
            Assert.AreEqual("John O'Brien-Smith", workshops[0].Leader);
        }

        #endregion

        #region Helper Methods

        private void AddClassSelectionHeaders(ExcelWorksheet sheet)
        {
            sheet.Cells[1, 1].Value = "ClassSelection_Id";
            sheet.Cells[1, 2].Value = "Name_First";
            sheet.Cells[1, 3].Value = "Name_Last";
            sheet.Cells[1, 4].Value = "Email";
            sheet.Cells[1, 5].Value = "Age";
        }

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
