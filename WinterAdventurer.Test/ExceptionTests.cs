// <copyright file="ExceptionTests.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using WinterAdventurer.Library.Exceptions;

namespace WinterAdventurer.Test
{
    /// <summary>
    /// Tests for custom exception classes in the WinterAdventurer.Library.Exceptions namespace.
    /// Verifies constructors, properties, and exception hierarchy.
    /// </summary>
    [TestClass]
    public class ExceptionTests
    {
        [TestMethod]
        public void ExcelParsingException_DefaultConstructor_CreatesException()
        {
            // Act
            var exception = new ExcelParsingException();

            // Assert
            Assert.IsNotNull(exception);
            Assert.IsNull(exception.SheetName);
            Assert.IsNull(exception.RowNumber);
            Assert.IsNull(exception.ColumnName);
        }

        [TestMethod]
        public void ExcelParsingException_MessageConstructor_SetsMessage()
        {
            // Arrange
            string message = "Test parsing error";

            // Act
            var exception = new ExcelParsingException(message);

            // Assert
            Assert.AreEqual(message, exception.Message);
        }

        [TestMethod]
        public void ExcelParsingException_InnerExceptionConstructor_SetsInnerException()
        {
            // Arrange
            string message = "Test parsing error";
            var innerException = new InvalidOperationException("Inner error");

            // Act
            var exception = new ExcelParsingException(message, innerException);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual(innerException, exception.InnerException);
        }

        [TestMethod]
        public void ExcelParsingException_Properties_CanBeSet()
        {
            // Arrange
            var exception = new ExcelParsingException("Test error")
            {
                SheetName = "ClassSelection",
                RowNumber = 5,
                ColumnName = "Name_First",
            };

            // Assert
            Assert.AreEqual("ClassSelection", exception.SheetName);
            Assert.AreEqual(5, exception.RowNumber);
            Assert.AreEqual("Name_First", exception.ColumnName);
        }

        [TestMethod]
        public void MissingSheetException_DefaultConstructor_CreatesException()
        {
            // Act
            var exception = new MissingSheetException();

            // Assert
            Assert.IsNotNull(exception);
            Assert.IsNotNull(exception.AvailableSheets);
            Assert.AreEqual(0, exception.AvailableSheets.Count);
        }

        [TestMethod]
        public void MissingSheetException_MessageConstructor_SetsMessage()
        {
            // Arrange
            string message = "Sheet 'ClassSelection' not found";

            // Act
            var exception = new MissingSheetException(message);

            // Assert
            Assert.AreEqual(message, exception.Message);
        }

        [TestMethod]
        public void MissingSheetException_InnerExceptionConstructor_SetsInnerException()
        {
            // Arrange
            string message = "Sheet not found";
            var innerException = new KeyNotFoundException("Key error");

            // Act
            var exception = new MissingSheetException(message, innerException);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual(innerException, exception.InnerException);
        }

        [TestMethod]
        public void MissingSheetException_AvailableSheets_CanBeSet()
        {
            // Arrange
            var exception = new MissingSheetException("Sheet not found")
            {
                AvailableSheets = new List<string> { "Sheet1", "Sheet2", "Sheet3" },
            };

            // Assert
            Assert.AreEqual(3, exception.AvailableSheets.Count);
            Assert.IsTrue(exception.AvailableSheets.Contains("Sheet1"));
            Assert.IsTrue(exception.AvailableSheets.Contains("Sheet2"));
            Assert.IsTrue(exception.AvailableSheets.Contains("Sheet3"));
        }

        [TestMethod]
        public void MissingSheetException_InheritsFromExcelParsingException()
        {
            // Arrange
            var exception = new MissingSheetException();

            // Assert
            Assert.IsInstanceOfType<ExcelParsingException>(exception);
        }

        [TestMethod]
        public void MissingColumnException_DefaultConstructor_CreatesException()
        {
            // Act
            var exception = new MissingColumnException();

            // Assert
            Assert.IsNotNull(exception);
            Assert.IsNotNull(exception.AvailableColumns);
            Assert.AreEqual(0, exception.AvailableColumns.Count);
        }

        [TestMethod]
        public void MissingColumnException_MessageConstructor_SetsMessage()
        {
            // Arrange
            string message = "Column 'Name_First' not found";

            // Act
            var exception = new MissingColumnException(message);

            // Assert
            Assert.AreEqual(message, exception.Message);
        }

        [TestMethod]
        public void MissingColumnException_Properties_CanBeSet()
        {
            // Arrange
            var exception = new MissingColumnException("Column not found")
            {
                ExpectedPattern = "Name_First",
                AvailableColumns = new List<string> { "Column1", "Column2", "Column3" },
            };

            // Assert
            Assert.AreEqual("Name_First", exception.ExpectedPattern);
            Assert.AreEqual(3, exception.AvailableColumns.Count);
            Assert.IsTrue(exception.AvailableColumns.Contains("Column1"));
        }

        [TestMethod]
        public void MissingColumnException_InheritsFromExcelParsingException()
        {
            // Arrange
            var exception = new MissingColumnException();

            // Assert
            Assert.IsInstanceOfType<ExcelParsingException>(exception);
        }

        [TestMethod]
        public void InvalidWorkshopFormatException_DefaultConstructor_CreatesException()
        {
            // Act
            var exception = new InvalidWorkshopFormatException();

            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(string.Empty, exception.CellValue);
            Assert.AreEqual("WorkshopName (LeaderName)", exception.ExpectedFormat);
        }

        [TestMethod]
        public void InvalidWorkshopFormatException_MessageConstructor_SetsMessage()
        {
            // Arrange
            string message = "Invalid workshop format";

            // Act
            var exception = new InvalidWorkshopFormatException(message);

            // Assert
            Assert.AreEqual(message, exception.Message);
        }

        [TestMethod]
        public void InvalidWorkshopFormatException_Properties_CanBeSet()
        {
            // Arrange
            var exception = new InvalidWorkshopFormatException("Invalid format")
            {
                CellValue = "Invalid Workshop Name",
                ExpectedFormat = "WorkshopName (Leader)",
            };

            // Assert
            Assert.AreEqual("Invalid Workshop Name", exception.CellValue);
            Assert.AreEqual("WorkshopName (Leader)", exception.ExpectedFormat);
        }

        [TestMethod]
        public void InvalidWorkshopFormatException_InheritsFromExcelParsingException()
        {
            // Arrange
            var exception = new InvalidWorkshopFormatException();

            // Assert
            Assert.IsInstanceOfType<ExcelParsingException>(exception);
        }

        [TestMethod]
        public void SchemaValidationException_DefaultConstructor_CreatesException()
        {
            // Act
            var exception = new SchemaValidationException();

            // Assert
            Assert.IsNotNull(exception);
            Assert.IsNull(exception.SchemaName);
            Assert.IsNull(exception.MissingSheet);
            Assert.IsNotNull(exception.AvailableSheets);
            Assert.AreEqual(0, exception.AvailableSheets.Count);
        }

        [TestMethod]
        public void SchemaValidationException_MessageConstructor_SetsMessage()
        {
            // Arrange
            string message = "Schema validation failed";

            // Act
            var exception = new SchemaValidationException(message);

            // Assert
            Assert.AreEqual(message, exception.Message);
        }

        [TestMethod]
        public void SchemaValidationException_Properties_CanBeSet()
        {
            // Arrange
            var exception = new SchemaValidationException("Validation failed")
            {
                SchemaName = "WinterAdventureSchema",
                MissingSheet = "ClassSelection",
                AvailableSheets = new List<string> { "Sheet1", "Sheet2" },
            };

            // Assert
            Assert.AreEqual("WinterAdventureSchema", exception.SchemaName);
            Assert.AreEqual("ClassSelection", exception.MissingSheet);
            Assert.AreEqual(2, exception.AvailableSheets.Count);
            Assert.IsTrue(exception.AvailableSheets.Contains("Sheet1"));
            Assert.IsTrue(exception.AvailableSheets.Contains("Sheet2"));
        }

        [TestMethod]
        public void SchemaValidationException_InheritsFromExcelParsingException()
        {
            // Arrange
            var exception = new SchemaValidationException();

            // Assert
            Assert.IsInstanceOfType<ExcelParsingException>(exception);
        }

        [TestMethod]
        public void PdfGenerationException_DefaultConstructor_CreatesException()
        {
            // Act
            var exception = new PdfGenerationException();

            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(string.Empty, exception.Section);
        }

        [TestMethod]
        public void PdfGenerationException_MessageConstructor_SetsMessage()
        {
            // Arrange
            string message = "PDF generation failed";

            // Act
            var exception = new PdfGenerationException(message);

            // Assert
            Assert.AreEqual(message, exception.Message);
        }

        [TestMethod]
        public void PdfGenerationException_InnerExceptionConstructor_SetsInnerException()
        {
            // Arrange
            string message = "PDF generation failed";
            var innerException = new IOException("File error");

            // Act
            var exception = new PdfGenerationException(message, innerException);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual(innerException, exception.InnerException);
        }

        [TestMethod]
        public void PdfGenerationException_Section_CanBeSet()
        {
            // Arrange
            var exception = new PdfGenerationException("Generation failed")
            {
                Section = "Roster",
            };

            // Assert
            Assert.AreEqual("Roster", exception.Section);
        }

        [TestMethod]
        public void PdfGenerationException_InheritsFromException()
        {
            // Arrange
            var exception = new PdfGenerationException();

            // Assert
            Assert.IsInstanceOfType<Exception>(exception);
        }

        [TestMethod]
        public void MissingResourceException_DefaultConstructor_CreatesException()
        {
            // Act
            var exception = new MissingResourceException();

            // Assert
            Assert.IsNotNull(exception);
            Assert.AreEqual(string.Empty, exception.ResourceName);
        }

        [TestMethod]
        public void MissingResourceException_MessageConstructor_SetsMessage()
        {
            // Arrange
            string message = "Resource not found";

            // Act
            var exception = new MissingResourceException(message);

            // Assert
            Assert.AreEqual(message, exception.Message);
        }

        [TestMethod]
        public void MissingResourceException_InnerExceptionConstructor_SetsInnerException()
        {
            // Arrange
            string message = "Resource not found";
            var innerException = new FileNotFoundException("Font file not found");

            // Act
            var exception = new MissingResourceException(message, innerException);

            // Assert
            Assert.AreEqual(message, exception.Message);
            Assert.AreEqual(innerException, exception.InnerException);
        }

        [TestMethod]
        public void MissingResourceException_ResourceName_CanBeSet()
        {
            // Arrange
            var exception = new MissingResourceException("Resource not found")
            {
                ResourceName = "NotoSans-Regular.ttf",
            };

            // Assert
            Assert.AreEqual("NotoSans-Regular.ttf", exception.ResourceName);
        }

        [TestMethod]
        public void MissingResourceException_InheritsFromPdfGenerationException()
        {
            // Arrange
            var exception = new MissingResourceException();

            // Assert
            Assert.IsInstanceOfType<PdfGenerationException>(exception);
        }
    }
}
