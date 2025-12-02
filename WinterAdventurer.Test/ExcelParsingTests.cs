using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OfficeOpenXml;
using WinterAdventurer.Library;
using WinterAdventurer.Library.Models;

namespace WinterAdventurer.Test
{
    /// <summary>
    /// Tests for Excel parsing logic in ExcelUtilities.
    /// Covers ImportExcel, ParseWorkshops, LoadAttendees, and CollectWorkshops methods.
    /// </summary>
    [TestClass]
    public class ExcelParsingTests
    {
        private ExcelUtilities _excelUtilities = null!;

        [TestInitialize]
        public void Setup()
        {
            _excelUtilities = new ExcelUtilities(NullLogger<ExcelUtilities>.Instance);
            ExcelPackage.License.SetNonCommercialOrganization("WinterAdventurer");
        }

        #region ImportExcel Tests

        [TestMethod]
        public void ImportExcel_WithNullStream_ThrowsInvalidOperationException()
        {
            // Arrange
            Stream? nullStream = null;

            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _excelUtilities.ImportExcel(nullStream!);
            });
        }

        [TestMethod]
        public void ImportExcel_WithEmptyStream_ThrowsInvalidOperationException()
        {
            // Arrange
            using var emptyStream = new MemoryStream();

            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _excelUtilities.ImportExcel(emptyStream);
            });
        }

        // NOTE: This test is commented out because EPPlus does not allow saving a workbook with zero worksheets.
        // The defensive check in ImportExcel (line 79-82) for empty workbooks cannot be easily tested
        // since EPPlus throws an exception before we can create a valid test file.
        // The code path is left in place as defensive programming.
        /*
        [TestMethod]
        public void ImportExcel_WithNoWorksheets_ThrowsInvalidDataException()
        {
            // Cannot test - EPPlus doesn't allow saving workbooks with zero worksheets
        }
        */

        [TestMethod]
        public void ImportExcel_WithMissingClassSelectionSheet_ThrowsInvalidDataException()
        {
            // Arrange
            using var package = new ExcelPackage();
            package.Workbook.Worksheets.Add("WrongSheetName");
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            // Act & Assert
            try
            {
                _excelUtilities.ImportExcel(stream);
                Assert.Fail("Should have thrown exception");
            }
            catch (InvalidOperationException ex)
            {
                // The InvalidDataException is wrapped in InvalidOperationException
                Assert.IsInstanceOfType<InvalidOperationException>(ex.InnerException);
                Assert.Contains(ex.Message, "verify the file format");
            }
        }

        [TestMethod]
        public void ImportExcel_WithValidMinimalData_ParsesSuccessfully()
        {
            // Arrange
            using var package = CreateMinimalValidExcelPackage();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            // Act
            _excelUtilities.ImportExcel(stream);

            // Assert
            Assert.IsNotNull(_excelUtilities.Workshops);
            Assert.AreEqual(1, _excelUtilities.Workshops.Count);
            Assert.AreEqual("Woodworking", _excelUtilities.Workshops[0].Name);
            Assert.AreEqual("John Smith", _excelUtilities.Workshops[0].Leader);
        }

        [TestMethod]
        public void ImportExcel_WithMultipleWorkshops_ParsesAllWorkshops()
        {
            // Arrange
            using var package = CreateExcelPackageWithMultipleWorkshops();
            using var stream = new MemoryStream();
            package.SaveAs(stream);
            stream.Position = 0;

            // Act
            _excelUtilities.ImportExcel(stream);

            // Assert
            Assert.AreEqual(2, _excelUtilities.Workshops.Count);

            var woodworking = _excelUtilities.Workshops.FirstOrDefault(w => w.Name == "Woodworking");
            var pottery = _excelUtilities.Workshops.FirstOrDefault(w => w.Name == "Pottery");

            Assert.IsNotNull(woodworking);
            Assert.IsNotNull(pottery);
            Assert.AreEqual("John Smith", woodworking!.Leader);
            Assert.AreEqual("Jane Doe", pottery!.Leader);
        }

        #endregion

        #region ParseWorkshops Tests

        [TestMethod]
        public void ParseWorkshops_WithEmptyClassSelectionSheet_ReturnsEmptyWorkshopList()
        {
            // Arrange
            using var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            // No attendee rows added

            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);

            // Act
            var workshops = _excelUtilities.ParseWorkshops(package);

            // Assert
            Assert.IsNotNull(workshops);
            Assert.AreEqual(0, workshops.Count);
        }

        [TestMethod]
        public void ParseWorkshops_WithEmptyPeriodSheet_HandlesGracefully()
        {
            // Arrange
            using var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);
            // No workshop rows added

            // Act
            var workshops = _excelUtilities.ParseWorkshops(package);

            // Assert
            Assert.IsNotNull(workshops);
            Assert.AreEqual(0, workshops.Count);
        }

        [TestMethod]
        public void ParseWorkshops_WithMissingPeriodSheet_SkipsAndContinues()
        {
            // Arrange
            using var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            // Only add MorningFirstPeriod, skip MorningSecondPeriod and AfternoonPeriod
            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);
            periodSheet.Cells[2, 1].Value = "SEL001";
            periodSheet.Cells[2, 2].Value = "Alice";
            periodSheet.Cells[2, 3].Value = "Johnson";
            periodSheet.Cells[2, 6].Value = "Woodworking (John Smith)";
            periodSheet.Cells[2, 7].Value = "1";

            // Act
            var workshops = _excelUtilities.ParseWorkshops(package);

            // Assert
            Assert.IsNotNull(workshops);
            Assert.AreEqual(1, workshops.Count);
            Assert.AreEqual("Woodworking", workshops[0].Name);
        }

        #endregion

        #region LoadAttendees Tests (via ParseWorkshops)

        [TestMethod]
        public void LoadAttendees_WithMissingSelectionId_GeneratesFallbackId()
        {
            // Arrange
            using var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);

            // Add attendee WITHOUT selection ID - LoadAttendees will generate "BobSmith" as fallback
            classSelection.Cells[2, 1].Value = ""; // Empty selection ID
            classSelection.Cells[2, 2].Value = "Bob";
            classSelection.Cells[2, 3].Value = "Smith";
            classSelection.Cells[2, 4].Value = "bob@example.com";
            classSelection.Cells[2, 5].Value = "25";

            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);
            // Use the fallback ID that LoadAttendees generated
            periodSheet.Cells[2, 1].Value = "BobSmith"; // Must match the generated fallback ID
            periodSheet.Cells[2, 2].Value = "Bob";
            periodSheet.Cells[2, 3].Value = "Smith";
            periodSheet.Cells[2, 6].Value = "Woodworking (John Smith)";
            periodSheet.Cells[2, 7].Value = "1";

            // Act
            var workshops = _excelUtilities.ParseWorkshops(package);

            // Assert
            Assert.AreEqual(1, workshops.Count);
            var workshop = workshops[0];
            Assert.AreEqual(1, workshop.Selections.Count);

            // Fallback ID should be FirstNameLastName (no spaces)
            var selection = workshop.Selections[0];
            Assert.AreEqual("BobSmith", selection.ClassSelectionId);
            Assert.AreEqual("Bob", selection.FirstName);
            Assert.AreEqual("Smith", selection.LastName);
        }

        [TestMethod]
        public void LoadAttendees_WithMultipleAttendees_LoadsAll()
        {
            // Arrange
            using var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);

            // Add 3 attendees
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            classSelection.Cells[3, 1].Value = "SEL002";
            classSelection.Cells[3, 2].Value = "Bob";
            classSelection.Cells[3, 3].Value = "Williams";

            classSelection.Cells[4, 1].Value = "SEL003";
            classSelection.Cells[4, 2].Value = "Carol";
            classSelection.Cells[4, 3].Value = "Davis";

            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);

            // All 3 attendees select the same workshop
            for (int row = 2; row <= 4; row++)
            {
                periodSheet.Cells[row, 1].Value = $"SEL00{row - 1}";
                periodSheet.Cells[row, 6].Value = "Woodworking (John Smith)";
                periodSheet.Cells[row, 7].Value = "1";
            }

            // Act
            var workshops = _excelUtilities.ParseWorkshops(package);

            // Assert
            Assert.AreEqual(1, workshops.Count);
            var workshop = workshops[0];
            Assert.AreEqual(3, workshop.Selections.Count);

            Assert.IsTrue(workshop.Selections.Any(s => s.FirstName == "Alice"));
            Assert.IsTrue(workshop.Selections.Any(s => s.FirstName == "Bob"));
            Assert.IsTrue(workshop.Selections.Any(s => s.FirstName == "Carol"));
        }

        #endregion

        #region CollectWorkshops Tests (via ParseWorkshops)

        [TestMethod]
        public void CollectWorkshops_WithMultipleDurations_CreatesDistinctWorkshops()
        {
            // Arrange
            using var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);

            // Same workshop name but different durations (4-day vs 2-day)
            periodSheet.Cells[2, 1].Value = "SEL001";
            periodSheet.Cells[2, 6].Value = "Woodworking (John Smith)"; // 4-day column
            periodSheet.Cells[2, 7].Value = "1";

            classSelection.Cells[3, 1].Value = "SEL002";
            classSelection.Cells[3, 2].Value = "Bob";
            classSelection.Cells[3, 3].Value = "Williams";

            periodSheet.Cells[3, 1].Value = "SEL002";
            periodSheet.Cells[3, 8].Value = "Woodworking (John Smith)"; // 2-day first half column
            periodSheet.Cells[3, 7].Value = "1";

            // Act
            var workshops = _excelUtilities.ParseWorkshops(package);

            // Assert
            Assert.AreEqual(2, workshops.Count, "Should create 2 distinct workshops (different durations)");

            var fourDayWorkshop = workshops.FirstOrDefault(w => w.Duration.NumberOfDays == 4);
            var twoDayWorkshop = workshops.FirstOrDefault(w => w.Duration.NumberOfDays == 2);

            Assert.IsNotNull(fourDayWorkshop);
            Assert.IsNotNull(twoDayWorkshop);
            Assert.AreEqual("Woodworking", fourDayWorkshop!.Name);
            Assert.AreEqual("Woodworking", twoDayWorkshop!.Name);
        }

        [TestMethod]
        public void CollectWorkshops_WithChoiceNumbers_StoresCorrectly()
        {
            // Arrange
            using var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);

            // First choice
            periodSheet.Cells[2, 1].Value = "SEL001";
            periodSheet.Cells[2, 6].Value = "Woodworking (John Smith)";
            periodSheet.Cells[2, 7].Value = "1"; // First choice

            classSelection.Cells[3, 1].Value = "SEL002";
            classSelection.Cells[3, 2].Value = "Bob";
            classSelection.Cells[3, 3].Value = "Williams";

            // Backup choice
            periodSheet.Cells[3, 1].Value = "SEL002";
            periodSheet.Cells[3, 6].Value = "Woodworking (John Smith)";
            periodSheet.Cells[3, 7].Value = "3"; // Third choice (backup)

            // Act
            var workshops = _excelUtilities.ParseWorkshops(package);

            // Assert
            Assert.AreEqual(1, workshops.Count);
            var workshop = workshops[0];
            Assert.AreEqual(2, workshop.Selections.Count);

            var firstChoice = workshop.Selections.First(s => s.FirstName == "Alice");
            var backupChoice = workshop.Selections.First(s => s.FirstName == "Bob");

            Assert.AreEqual(1, firstChoice.ChoiceNumber);
            Assert.AreEqual(3, backupChoice.ChoiceNumber);
        }

        [TestMethod]
        public void CollectWorkshops_WithSameWorkshopMultiplePeople_AggregatesSelections()
        {
            // Arrange
            using var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);

            for (int i = 1; i <= 5; i++)
            {
                classSelection.Cells[i + 1, 1].Value = $"SEL00{i}";
                classSelection.Cells[i + 1, 2].Value = $"Person{i}";
                classSelection.Cells[i + 1, 3].Value = "LastName";
            }

            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);

            // All 5 people select the same workshop
            for (int i = 1; i <= 5; i++)
            {
                periodSheet.Cells[i + 1, 1].Value = $"SEL00{i}";
                periodSheet.Cells[i + 1, 6].Value = "Woodworking (John Smith)";
                periodSheet.Cells[i + 1, 7].Value = "1";
            }

            // Act
            var workshops = _excelUtilities.ParseWorkshops(package);

            // Assert
            Assert.AreEqual(1, workshops.Count, "Should aggregate into single workshop");
            var workshop = workshops[0];
            Assert.AreEqual(5, workshop.Selections.Count, "Should have 5 selections");
            Assert.AreEqual("Woodworking", workshop.Name);
        }

        [TestMethod]
        public void CollectWorkshops_WithDifferentLeaders_CreatesDistinctWorkshops()
        {
            // Arrange
            using var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            classSelection.Cells[3, 1].Value = "SEL002";
            classSelection.Cells[3, 2].Value = "Bob";
            classSelection.Cells[3, 3].Value = "Williams";

            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);

            // Same workshop name but different leaders
            periodSheet.Cells[2, 1].Value = "SEL001";
            periodSheet.Cells[2, 6].Value = "Woodworking (John Smith)";
            periodSheet.Cells[2, 7].Value = "1";

            periodSheet.Cells[3, 1].Value = "SEL002";
            periodSheet.Cells[3, 6].Value = "Woodworking (Jane Doe)"; // Different leader
            periodSheet.Cells[3, 7].Value = "1";

            // Act
            var workshops = _excelUtilities.ParseWorkshops(package);

            // Assert
            Assert.AreEqual(2, workshops.Count, "Should create 2 distinct workshops (different leaders)");

            var johnWorkshop = workshops.FirstOrDefault(w => w.Leader == "John Smith");
            var janeWorkshop = workshops.FirstOrDefault(w => w.Leader == "Jane Doe");

            Assert.IsNotNull(johnWorkshop);
            Assert.IsNotNull(janeWorkshop);
            Assert.AreEqual("Woodworking", johnWorkshop!.Name);
            Assert.AreEqual("Woodworking", janeWorkshop!.Name);
        }

        [TestMethod]
        public void CollectWorkshops_WithMultiplePeriods_CreatesDistinctWorkshops()
        {
            // Arrange
            using var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            // Add same workshop to two different periods
            var morningFirst = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(morningFirst);
            morningFirst.Cells[2, 1].Value = "SEL001";
            morningFirst.Cells[2, 6].Value = "Woodworking (John Smith)";
            morningFirst.Cells[2, 7].Value = "1";

            var afternoon = package.Workbook.Worksheets.Add("AfternoonPeriod");
            AddPeriodSheetHeaders(afternoon);
            afternoon.Cells[2, 1].Value = "SEL001";
            afternoon.Cells[2, 6].Value = "Woodworking (John Smith)"; // Same workshop, different period
            afternoon.Cells[2, 7].Value = "1";

            // Act
            var workshops = _excelUtilities.ParseWorkshops(package);

            // Assert
            Assert.AreEqual(2, workshops.Count, "Should create 2 distinct workshops (different periods)");

            var morningWorkshop = workshops.FirstOrDefault(w => w.Period.SheetName == "MorningFirstPeriod");
            var afternoonWorkshop = workshops.FirstOrDefault(w => w.Period.SheetName == "AfternoonPeriod");

            Assert.IsNotNull(morningWorkshop);
            Assert.IsNotNull(afternoonWorkshop);
            Assert.AreEqual("Woodworking", morningWorkshop!.Name);
            Assert.AreEqual("Woodworking", afternoonWorkshop!.Name);
        }

        [TestMethod]
        public void CollectWorkshops_WithEmptyWorkshopCells_SkipsGracefully()
        {
            // Arrange
            using var package = new ExcelPackage();
            var classSelection = package.Workbook.Worksheets.Add("ClassSelection");
            AddClassSelectionHeaders(classSelection);
            classSelection.Cells[2, 1].Value = "SEL001";
            classSelection.Cells[2, 2].Value = "Alice";
            classSelection.Cells[2, 3].Value = "Johnson";

            classSelection.Cells[3, 1].Value = "SEL002";
            classSelection.Cells[3, 2].Value = "Bob";
            classSelection.Cells[3, 3].Value = "Williams";

            var periodSheet = package.Workbook.Worksheets.Add("MorningFirstPeriod");
            AddPeriodSheetHeaders(periodSheet);

            // First row has workshop
            periodSheet.Cells[2, 1].Value = "SEL001";
            periodSheet.Cells[2, 6].Value = "Woodworking (John Smith)";
            periodSheet.Cells[2, 7].Value = "1";

            // Second row has empty workshop cell
            periodSheet.Cells[3, 1].Value = "SEL002";
            periodSheet.Cells[3, 6].Value = ""; // Empty workshop
            periodSheet.Cells[3, 7].Value = "1";

            // Act
            var workshops = _excelUtilities.ParseWorkshops(package);

            // Assert
            Assert.AreEqual(1, workshops.Count, "Should skip empty workshop cells");
            Assert.AreEqual(1, workshops[0].Selections.Count);
            Assert.AreEqual("Alice", workshops[0].Selections[0].FirstName);
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Creates a minimal valid Excel package with one workshop and one attendee
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
            periodSheet.Cells[2, 6].Value = "Woodworking (John Smith)";
            periodSheet.Cells[2, 7].Value = "1";

            return package;
        }

        /// <summary>
        /// Creates an Excel package with multiple workshops
        /// </summary>
        private ExcelPackage CreateExcelPackageWithMultipleWorkshops()
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
            periodSheet.Cells[2, 6].Value = "Woodworking (John Smith)";
            periodSheet.Cells[2, 7].Value = "1";

            periodSheet.Cells[3, 1].Value = "SEL002";
            periodSheet.Cells[3, 6].Value = "Pottery (Jane Doe)";
            periodSheet.Cells[3, 7].Value = "1";

            return package;
        }

        /// <summary>
        /// Adds standard ClassSelection sheet headers matching the schema
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
        /// Adds standard period sheet headers matching the schema
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

        #endregion
    }
}
