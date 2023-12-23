using OfficeOpenXml;
using WinterAdventurer.Library;

namespace WinterAdventurer.Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void CollectWorkshops_WhenCalled_ReturnsListOfWorkshops()
        {
            // Arrange
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("TestSheet");
            sheet.Cells[1, 5].Value = "4 Day";
            sheet.Cells[1, 6].Value = "2 Day";
            sheet.Cells[2, 5].Value = "Workshop 1 (Leader 1)";
            sheet.Cells[2, 6].Value = "Workshop 2 (Leader 2)";
            sheet.Cells[2, 1].Value = 1;
            sheet.Cells[2, 2].Value = "Selection 1";
            sheet.Cells[2, 4].Value = "Full Name 1";
            var excelUtilities = new ExcelUtilities();

            // Act
            var result = excelUtilities.CollectWorkshops(sheet);

            // Assert
            Assert.IsInstanceOfType(result, typeof(List<Library.Models.Workshop>));
            Assert.AreEqual(2, result.Count);
        }
    }
}