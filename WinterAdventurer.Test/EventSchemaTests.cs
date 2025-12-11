using Newtonsoft.Json.Linq;
using WinterAdventurer.Library.EventSchemas;

namespace WinterAdventurer.Test
{
    [TestClass]
    public class EventSchemaTests
    {
        #region ClassSelectionSheetConfig.GetColumnName Tests

        [TestMethod]
        public void GetColumnName_WithSimpleString_ReturnsString()
        {
            // Arrange
            var config = new ClassSelectionSheetConfig();
            config.Columns["firstName"] = "First Name";

            // Act
            var result = config.GetColumnName("firstName");

            // Assert
            Assert.AreEqual("First Name", result);
        }

        [TestMethod]
        public void GetColumnName_WithPatternObject_ReturnsPattern()
        {
            // Arrange
            var config = new ClassSelectionSheetConfig();
            var patternObj = new JObject
            {
                ["pattern"] = "WinterAdventureClassRegist_Id"
            };
            config.Columns["registrationId"] = patternObj;

            // Act
            var result = config.GetColumnName("registrationId");

            // Assert
            Assert.AreEqual("WinterAdventureClassRegist_Id", result);
        }

        [TestMethod]
        public void GetColumnName_WithMissingKey_ReturnsEmptyString()
        {
            // Arrange
            var config = new ClassSelectionSheetConfig();

            // Act
            var result = config.GetColumnName("nonExistentKey");

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void GetColumnName_WithNullPattern_ReturnsEmptyString()
        {
            // Arrange
            var config = new ClassSelectionSheetConfig();
            var patternObj = new JObject
            {
                ["pattern"] = null
            };
            config.Columns["testKey"] = patternObj;

            // Act
            var result = config.GetColumnName("testKey");

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void GetColumnName_WithObjectWithoutPattern_ReturnsEmptyString()
        {
            // Arrange
            var config = new ClassSelectionSheetConfig();
            var obj = new JObject
            {
                ["someOtherProperty"] = "value"
            };
            config.Columns["testKey"] = obj;

            // Act
            var result = config.GetColumnName("testKey");

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void GetColumnName_WithEmptyString_ReturnsEmptyString()
        {
            // Arrange
            var config = new ClassSelectionSheetConfig();
            config.Columns["emptyKey"] = "";

            // Act
            var result = config.GetColumnName("emptyKey");

            // Assert
            Assert.AreEqual("", result);
        }

        [TestMethod]
        public void GetColumnName_MultipleKeys_ReturnsCorrectValues()
        {
            // Arrange
            var config = new ClassSelectionSheetConfig();
            config.Columns["firstName"] = "First Name";
            config.Columns["lastName"] = "Last Name";
            var patternObj = new JObject { ["pattern"] = "Email_Pattern" };
            config.Columns["email"] = patternObj;

            // Act
            var result1 = config.GetColumnName("firstName");
            var result2 = config.GetColumnName("lastName");
            var result3 = config.GetColumnName("email");

            // Assert
            Assert.AreEqual("First Name", result1);
            Assert.AreEqual("Last Name", result2);
            Assert.AreEqual("Email_Pattern", result3);
        }

        #endregion

        #region PeriodSheetConfig.GetColumnName Tests

        [TestMethod]
        public void PeriodSheet_GetColumnName_WithSimpleString_ReturnsString()
        {
            // Arrange
            var config = new PeriodSheetConfig();
            config.Columns["selectionId"] = "Selection ID";

            // Act
            var result = config.GetColumnName("selectionId");

            // Assert
            Assert.AreEqual("Selection ID", result);
        }

        [TestMethod]
        public void PeriodSheet_GetColumnName_WithPatternObject_ReturnsPattern()
        {
            // Arrange
            var config = new PeriodSheetConfig();
            var patternObj = new JObject
            {
                ["pattern"] = "MorningPeriod_Choice"
            };
            config.Columns["choiceNumber"] = patternObj;

            // Act
            var result = config.GetColumnName("choiceNumber");

            // Assert
            Assert.AreEqual("MorningPeriod_Choice", result);
        }

        [TestMethod]
        public void PeriodSheet_GetColumnName_WithMissingKey_ReturnsEmptyString()
        {
            // Arrange
            var config = new PeriodSheetConfig();

            // Act
            var result = config.GetColumnName("missingKey");

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void PeriodSheet_GetColumnName_CaseInsensitiveKeys_DoesNotMatch()
        {
            // Arrange
            var config = new PeriodSheetConfig();
            config.Columns["firstName"] = "First Name";

            // Act
            var result = config.GetColumnName("FIRSTNAME"); // Different case

            // Assert
            Assert.AreEqual(string.Empty, result); // Dictionary keys are case-sensitive
        }

        #endregion

        #region EventSchema Property Tests

        [TestMethod]
        public void EventSchema_DefaultConstructor_InitializesProperties()
        {
            // Act
            var schema = new EventSchema();

            // Assert
            Assert.AreEqual(string.Empty, schema.EventName);
            Assert.AreEqual(0, schema.TotalDays);
            Assert.IsNotNull(schema.ClassSelectionSheet);
            Assert.IsNotNull(schema.PeriodSheets);
            Assert.IsNotNull(schema.WorkshopFormat);
            Assert.AreEqual(0, schema.PeriodSheets.Count);
        }

        [TestMethod]
        public void EventSchema_Properties_CanBeSet()
        {
            // Arrange
            var schema = new EventSchema();
            var classConfig = new ClassSelectionSheetConfig { SheetName = "ClassSelection" };
            var periodConfigs = new List<PeriodSheetConfig>
            {
                new PeriodSheetConfig { SheetName = "Morning", DisplayName = "Morning Period" }
            };
            var workshopFormat = new WorkshopFormatConfig { Pattern = @"(.+)\s*\((.+)\)" };

            // Act
            schema.EventName = "Winter Adventure 2024";
            schema.TotalDays = 4;
            schema.ClassSelectionSheet = classConfig;
            schema.PeriodSheets = periodConfigs;
            schema.WorkshopFormat = workshopFormat;

            // Assert
            Assert.AreEqual("Winter Adventure 2024", schema.EventName);
            Assert.AreEqual(4, schema.TotalDays);
            Assert.AreEqual("ClassSelection", schema.ClassSelectionSheet.SheetName);
            Assert.AreEqual(1, schema.PeriodSheets.Count);
            Assert.AreEqual("Morning", schema.PeriodSheets[0].SheetName);
            Assert.AreEqual(@"(.+)\s*\((.+)\)", schema.WorkshopFormat.Pattern);
        }

        #endregion

        #region ClassSelectionSheetConfig Property Tests

        [TestMethod]
        public void ClassSelectionSheetConfig_DefaultConstructor_InitializesProperties()
        {
            // Act
            var config = new ClassSelectionSheetConfig();

            // Assert
            Assert.AreEqual(string.Empty, config.SheetName);
            Assert.IsNotNull(config.Columns);
            Assert.AreEqual(0, config.Columns.Count);
        }

        [TestMethod]
        public void ClassSelectionSheetConfig_Properties_CanBeSet()
        {
            // Arrange & Act
            var config = new ClassSelectionSheetConfig
            {
                SheetName = "ClassSelection",
                Columns = new Dictionary<string, object>
                {
                    ["firstName"] = "First Name",
                    ["lastName"] = "Last Name"
                }
            };

            // Assert
            Assert.AreEqual("ClassSelection", config.SheetName);
            Assert.AreEqual(2, config.Columns.Count);
            Assert.AreEqual("First Name", config.Columns["firstName"]);
        }

        #endregion

        #region PeriodSheetConfig Property Tests

        [TestMethod]
        public void PeriodSheetConfig_DefaultConstructor_InitializesProperties()
        {
            // Act
            var config = new PeriodSheetConfig();

            // Assert
            Assert.AreEqual(string.Empty, config.SheetName);
            Assert.AreEqual(string.Empty, config.DisplayName);
            Assert.IsNotNull(config.Columns);
            Assert.IsNotNull(config.WorkshopColumns);
            Assert.AreEqual(0, config.Columns.Count);
            Assert.AreEqual(0, config.WorkshopColumns.Count);
        }

        [TestMethod]
        public void PeriodSheetConfig_Properties_CanBeSet()
        {
            // Arrange & Act
            var workshopColumns = new List<WorkshopColumnConfig>
            {
                new WorkshopColumnConfig { ColumnName = "4-Day Workshop", StartDay = 1, EndDay = 4 }
            };

            var config = new PeriodSheetConfig
            {
                SheetName = "MorningFirstPeriod",
                DisplayName = "Morning - First Period",
                Columns = new Dictionary<string, object> { ["selectionId"] = "ID" },
                WorkshopColumns = workshopColumns
            };

            // Assert
            Assert.AreEqual("MorningFirstPeriod", config.SheetName);
            Assert.AreEqual("Morning - First Period", config.DisplayName);
            Assert.AreEqual(1, config.Columns.Count);
            Assert.AreEqual(1, config.WorkshopColumns.Count);
            Assert.AreEqual("4-Day Workshop", config.WorkshopColumns[0].ColumnName);
        }

        #endregion

        #region WorkshopColumnConfig Property Tests

        [TestMethod]
        public void WorkshopColumnConfig_DefaultConstructor_InitializesProperties()
        {
            // Act
            var config = new WorkshopColumnConfig();

            // Assert
            Assert.AreEqual(string.Empty, config.ColumnName);
            Assert.AreEqual(0, config.StartDay);
            Assert.AreEqual(0, config.EndDay);
        }

        [TestMethod]
        public void WorkshopColumnConfig_Properties_CanBeSet()
        {
            // Arrange & Act
            var config = new WorkshopColumnConfig
            {
                ColumnName = "Days 1-2 Workshop",
                StartDay = 1,
                EndDay = 2
            };

            // Assert
            Assert.AreEqual("Days 1-2 Workshop", config.ColumnName);
            Assert.AreEqual(1, config.StartDay);
            Assert.AreEqual(2, config.EndDay);
        }

        #endregion

        #region WorkshopFormatConfig Property Tests

        [TestMethod]
        public void WorkshopFormatConfig_DefaultConstructor_InitializesProperties()
        {
            // Act
            var config = new WorkshopFormatConfig();

            // Assert
            Assert.AreEqual(string.Empty, config.Pattern);
            Assert.AreEqual(string.Empty, config.Description);
        }

        [TestMethod]
        public void WorkshopFormatConfig_Properties_CanBeSet()
        {
            // Arrange & Act
            var config = new WorkshopFormatConfig
            {
                Pattern = @"(.+)\s*\((.+)\)",
                Description = "Workshop Name (Leader Name)"
            };

            // Assert
            Assert.AreEqual(@"(.+)\s*\((.+)\)", config.Pattern);
            Assert.AreEqual("Workshop Name (Leader Name)", config.Description);
        }

        #endregion
    }
}
