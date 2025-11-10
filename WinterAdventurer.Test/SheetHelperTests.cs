using OfficeOpenXml;
using WinterAdventurer.Library;

namespace WinterAdventurer.Test
{
    [TestClass]
    public class SheetHelperTests
    {
        [TestInitialize]
        public void TestInitialize()
        {
            // Required for EPPlus
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        private ExcelWorksheet CreateTestSheet(Action<ExcelWorksheet>? setupAction = null)
        {
            var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("TestSheet");
            setupAction?.Invoke(sheet);
            return sheet;
        }

        #region Constructor and Initialization Tests

        [TestMethod]
        public void SheetHelper_WithValidSheet_InitializesCorrectly()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Name";
                s.Cells[1, 2].Value = "Age";
                s.Cells[1, 3].Value = "Location";
            });

            // Act
            var helper = new SheetHelper(sheet);

            // Assert
            Assert.IsNotNull(helper);
        }

        [TestMethod]
        public void SheetHelper_WithNullSheet_HandlesGracefully()
        {
            // Arrange & Act
            var helper = new SheetHelper(null!);

            // Assert
            Assert.IsNotNull(helper);
        }

        [TestMethod]
        public void SheetHelper_WithEmptySheet_InitializesWithoutError()
        {
            // Arrange
            var package = new ExcelPackage();
            var sheet = package.Workbook.Worksheets.Add("EmptySheet");

            // Act
            var helper = new SheetHelper(sheet);

            // Assert
            Assert.IsNotNull(helper);
        }

        #endregion

        #region GetColumnIndex Tests

        [TestMethod]
        public void GetColumnIndex_WithExactMatch_ReturnsCorrectIndex()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Name";
                s.Cells[1, 2].Value = "Age";
                s.Cells[1, 3].Value = "Location";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var index = helper.GetColumnIndex("Age");

            // Assert
            Assert.AreEqual(2, index);
        }

        [TestMethod]
        public void GetColumnIndex_WithNonExistentHeader_ReturnsNull()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Name";
                s.Cells[1, 2].Value = "Age";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var index = helper.GetColumnIndex("InvalidHeader");

            // Assert
            Assert.IsNull(index);
        }

        [TestMethod]
        public void GetColumnIndex_FirstColumn_ReturnsOne()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "FirstColumn";
                s.Cells[1, 2].Value = "SecondColumn";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var index = helper.GetColumnIndex("FirstColumn");

            // Assert
            Assert.AreEqual(1, index);
        }

        [TestMethod]
        public void GetColumnIndex_LastColumn_ReturnsCorrectIndex()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Col1";
                s.Cells[1, 2].Value = "Col2";
                s.Cells[1, 3].Value = "Col3";
                s.Cells[1, 4].Value = "LastColumn";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var index = helper.GetColumnIndex("LastColumn");

            // Assert
            Assert.AreEqual(4, index);
        }

        [TestMethod]
        public void GetColumnIndex_CaseSensitive_RequiresExactMatch()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Name";
                s.Cells[1, 2].Value = "Age";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var indexLower = helper.GetColumnIndex("name");
            var indexUpper = helper.GetColumnIndex("NAME");

            // Assert - Should not find lowercase/uppercase variants
            Assert.IsNull(indexLower);
            Assert.IsNull(indexUpper);
        }

        #endregion

        #region GetColumnIndexByPattern Tests

        [TestMethod]
        public void GetColumnIndexByPattern_WithPartialMatch_FindsColumn()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "2024WinterAdventureClassRegist_Id";
                s.Cells[1, 2].Value = "FirstName";
                s.Cells[1, 3].Value = "LastName";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var index = helper.GetColumnIndexByPattern("ClassRegist_Id");

            // Assert
            Assert.AreEqual(1, index);
        }

        [TestMethod]
        public void GetColumnIndexByPattern_WithMultipleMatches_ReturnsFirst()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Name_First";
                s.Cells[1, 2].Value = "Name_Last";
                s.Cells[1, 3].Value = "Name_Middle";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var index = helper.GetColumnIndexByPattern("Name");

            // Assert - Should return the first match
            Assert.AreEqual(1, index);
        }

        [TestMethod]
        public void GetColumnIndexByPattern_NoMatch_ReturnsNull()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "FirstName";
                s.Cells[1, 2].Value = "LastName";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var index = helper.GetColumnIndexByPattern("Address");

            // Assert
            Assert.IsNull(index);
        }

        [TestMethod]
        public void GetColumnIndexByPattern_ExactMatch_FindsColumn()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Id";
                s.Cells[1, 2].Value = "Name";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var index = helper.GetColumnIndexByPattern("Id");

            // Assert
            Assert.AreEqual(1, index);
        }

        #endregion

        #region GetCellValue Tests

        [TestMethod]
        public void GetCellValue_WithValidHeaderAndRow_ReturnsValue()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Name";
                s.Cells[1, 2].Value = "Age";
                s.Cells[2, 1].Value = "John Smith";
                s.Cells[2, 2].Value = 25;
            });
            var helper = new SheetHelper(sheet);

            // Act
            var value = helper.GetCellValue(2, "Name");

            // Assert
            Assert.AreEqual("John Smith", value);
        }

        [TestMethod]
        public void GetCellValue_WithNumericValue_ReturnsAsString()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Age";
                s.Cells[2, 1].Value = 25;
            });
            var helper = new SheetHelper(sheet);

            // Act
            var value = helper.GetCellValue(2, "Age");

            // Assert
            Assert.AreEqual("25", value);
        }

        [TestMethod]
        public void GetCellValue_WithInvalidHeader_ReturnsNull()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Name";
                s.Cells[2, 1].Value = "John Smith";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var value = helper.GetCellValue(2, "InvalidHeader");

            // Assert
            Assert.IsNull(value);
        }

        [TestMethod]
        public void GetCellValue_WithEmptyCell_ReturnsNull()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Name";
                // Row 2, col 1 is empty
            });
            var helper = new SheetHelper(sheet);

            // Act
            var value = helper.GetCellValue(2, "Name");

            // Assert
            Assert.IsNull(value);
        }

        #endregion

        #region GetCellValueByPattern Tests

        [TestMethod]
        public void GetCellValueByPattern_WithMatch_ReturnsValue()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "2024WinterAdventureClassRegist_Id";
                s.Cells[1, 2].Value = "FirstName";
                s.Cells[2, 1].Value = "12345";
                s.Cells[2, 2].Value = "John";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var value = helper.GetCellValueByPattern(2, "ClassRegist_Id");

            // Assert
            Assert.AreEqual("12345", value);
        }

        [TestMethod]
        public void GetCellValueByPattern_NoMatch_ReturnsNull()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "FirstName";
                s.Cells[2, 1].Value = "John";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var value = helper.GetCellValueByPattern(2, "LastName");

            // Assert
            Assert.IsNull(value);
        }

        #endregion

        #region GetCellValueByIndex Tests

        [TestMethod]
        public void GetCellValueByIndex_WithValidCoordinates_ReturnsValue()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[2, 3].Value = "Test Value";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var value = helper.GetCellValueByIndex(2, 3);

            // Assert
            Assert.AreEqual("Test Value", value);
        }

        [TestMethod]
        public void GetCellValueByIndex_WithEmptyCell_ReturnsNull()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                // Cell is empty
            });
            var helper = new SheetHelper(sheet);

            // Act
            var value = helper.GetCellValueByIndex(5, 5);

            // Assert
            Assert.IsNull(value);
        }

        #endregion

        #region Integration and Edge Cases

        [TestMethod]
        public void SheetHelper_RealWorldScenario_WorksCorrectly()
        {
            // Arrange - Simulate a real workshop registration sheet
            var sheet = CreateTestSheet(s =>
            {
                // Headers
                s.Cells[1, 1].Value = "2024WinterAdventureClassRegist_Id";
                s.Cells[1, 2].Value = "FirstName";
                s.Cells[1, 3].Value = "LastName";
                s.Cells[1, 4].Value = "MorningFirstPeriod_4Day";

                // Data row
                s.Cells[2, 1].Value = "12345";
                s.Cells[2, 2].Value = "John";
                s.Cells[2, 3].Value = "Smith";
                s.Cells[2, 4].Value = "Pottery (Jane Doe)";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var registId = helper.GetCellValueByPattern(2, "ClassRegist_Id");
            var firstName = helper.GetCellValue(2, "FirstName");
            var lastName = helper.GetCellValue(2, "LastName");
            var workshop = helper.GetCellValueByPattern(2, "MorningFirstPeriod");

            // Assert
            Assert.AreEqual("12345", registId);
            Assert.AreEqual("John", firstName);
            Assert.AreEqual("Smith", lastName);
            Assert.AreEqual("Pottery (Jane Doe)", workshop);
        }

        [TestMethod]
        public void SheetHelper_WithWhitespaceHeaders_IgnoresEmpty()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Name";
                s.Cells[1, 2].Value = "   "; // Whitespace only
                s.Cells[1, 3].Value = "Age";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var nameIndex = helper.GetColumnIndex("Name");
            var ageIndex = helper.GetColumnIndex("Age");
            var whitespaceIndex = helper.GetColumnIndex("   ");

            // Assert
            Assert.AreEqual(1, nameIndex);
            Assert.AreEqual(3, ageIndex);
            Assert.IsNull(whitespaceIndex); // Whitespace headers should be ignored
        }

        [TestMethod]
        public void SheetHelper_WithSpecialCharacters_HandlesCorrectly()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Name & Title";
                s.Cells[1, 2].Value = "Age (years)";
                s.Cells[1, 3].Value = "Email@Address";
                s.Cells[2, 1].Value = "Dr. Smith";
                s.Cells[2, 2].Value = 45;
                s.Cells[2, 3].Value = "test@example.com";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var name = helper.GetCellValue(2, "Name & Title");
            var age = helper.GetCellValue(2, "Age (years)");
            var email = helper.GetCellValue(2, "Email@Address");

            // Assert
            Assert.AreEqual("Dr. Smith", name);
            Assert.AreEqual("45", age);
            Assert.AreEqual("test@example.com", email);
        }

        [TestMethod]
        public void SheetHelper_MultipleRowsAccess_WorksCorrectly()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Name";
                s.Cells[2, 1].Value = "Alice";
                s.Cells[3, 1].Value = "Bob";
                s.Cells[4, 1].Value = "Charlie";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var row2 = helper.GetCellValue(2, "Name");
            var row3 = helper.GetCellValue(3, "Name");
            var row4 = helper.GetCellValue(4, "Name");

            // Assert
            Assert.AreEqual("Alice", row2);
            Assert.AreEqual("Bob", row3);
            Assert.AreEqual("Charlie", row4);
        }

        [TestMethod]
        public void SheetHelper_PatternMatching_PreferenceForFirstOccurrence()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Workshop_Morning_4Day";
                s.Cells[1, 2].Value = "Workshop_Afternoon_2Day";
                s.Cells[1, 3].Value = "Workshop_Evening_4Day";
                s.Cells[2, 1].Value = "Pottery";
                s.Cells[2, 2].Value = "Painting";
                s.Cells[2, 3].Value = "Sculpture";
            });
            var helper = new SheetHelper(sheet);

            // Act
            var value = helper.GetCellValueByPattern(2, "Workshop_");

            // Assert - Should return first match
            Assert.AreEqual("Pottery", value);
        }

        [TestMethod]
        public void SheetHelper_WithNullCellValues_ReturnsNull()
        {
            // Arrange
            var sheet = CreateTestSheet(s =>
            {
                s.Cells[1, 1].Value = "Name";
                s.Cells[1, 2].Value = "Age";
                s.Cells[2, 1].Value = null;
                s.Cells[2, 2].Value = null;
            });
            var helper = new SheetHelper(sheet);

            // Act
            var name = helper.GetCellValue(2, "Name");
            var age = helper.GetCellValue(2, "Age");

            // Assert
            Assert.IsNull(name);
            Assert.IsNull(age);
        }

        #endregion
    }
}
