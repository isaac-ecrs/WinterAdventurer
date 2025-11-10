using WinterAdventurer.Library.Extensions;

namespace WinterAdventurer.Test
{
    [TestClass]
    public class StringExtensionsTests
    {
        #region ToProper Tests

        [TestMethod]
        public void ToProper_WithLowercaseWords_CapitalizesEachWord()
        {
            // Arrange
            string input = "hello world";

            // Act
            string result = input.ToProper();

            // Assert
            Assert.AreEqual("Hello World", result);
        }

        [TestMethod]
        public void ToProper_WithUppercaseWords_ConvertsToProperCase()
        {
            // Arrange
            string input = "HELLO WORLD";

            // Act
            string result = input.ToProper();

            // Assert
            Assert.AreEqual("Hello World", result);
        }

        [TestMethod]
        public void ToProper_WithMixedCase_ConvertsToProperCase()
        {
            // Arrange
            string input = "hElLo WoRlD";

            // Act
            string result = input.ToProper();

            // Assert
            Assert.AreEqual("Hello World", result);
        }

        [TestMethod]
        public void ToProper_WithExtraSpaces_TrimsAndCapitalizes()
        {
            // Arrange
            string input = "  hello   world  ";

            // Act
            string result = input.ToProper();

            // Assert
            Assert.AreEqual("Hello World", result);
        }

        [TestMethod]
        public void ToProper_WithSingleWord_CapitalizesWord()
        {
            // Arrange
            string input = "pottery";

            // Act
            string result = input.ToProper();

            // Assert
            Assert.AreEqual("Pottery", result);
        }

        [TestMethod]
        public void ToProper_WithMultipleConsecutiveSpaces_HandlesCorrectly()
        {
            // Arrange
            string input = "hello     world";

            // Act
            string result = input.ToProper();

            // Assert
            Assert.AreEqual("Hello World", result);
        }

        #endregion

        #region ExtractFromParentheses Tests

        [TestMethod]
        public void ExtractFromParentheses_PullFromInside_ExtractsContent()
        {
            // Arrange
            string input = "Pottery (John Smith)";

            // Act
            string result = input.ExtractFromParentheses(pullFromInside: true);

            // Assert
            Assert.AreEqual("John Smith", result);
        }

        [TestMethod]
        public void ExtractFromParentheses_PullFromOutside_ExtractsBeforeParenthesis()
        {
            // Arrange
            string input = "Pottery (John Smith)";

            // Act
            string result = input.ExtractFromParentheses(pullFromInside: false);

            // Assert
            Assert.AreEqual("Pottery", result);
        }

        [TestMethod]
        public void ExtractFromParentheses_NoParentheses_ReturnsEmpty()
        {
            // Arrange
            string input = "Pottery Workshop";

            // Act
            string result = input.ExtractFromParentheses(pullFromInside: true);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ExtractFromParentheses_OnlyOpeningParenthesis_ReturnsEmpty()
        {
            // Arrange
            string input = "Pottery (John Smith";

            // Act
            string result = input.ExtractFromParentheses(pullFromInside: true);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ExtractFromParentheses_OnlyClosingParenthesis_ReturnsEmpty()
        {
            // Arrange
            string input = "Pottery John Smith)";

            // Act
            string result = input.ExtractFromParentheses(pullFromInside: true);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ExtractFromParentheses_BackwardsParentheses_ReturnsEmpty()
        {
            // Arrange
            string input = "Pottery )John Smith(";

            // Act
            string result = input.ExtractFromParentheses(pullFromInside: true);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ExtractFromParentheses_EmptyParentheses_ReturnsEmpty()
        {
            // Arrange
            string input = "Pottery ()";

            // Act
            string result = input.ExtractFromParentheses(pullFromInside: true);

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void ExtractFromParentheses_WithSpacesInsideParentheses_TrimsContent()
        {
            // Arrange
            string input = "Pottery (  John Smith  )";

            // Act
            string result = input.ExtractFromParentheses(pullFromInside: true);

            // Assert
            Assert.AreEqual("John Smith", result);
        }

        [TestMethod]
        public void ExtractFromParentheses_WithSpacesBeforeParentheses_TrimsContent()
        {
            // Arrange
            string input = "Pottery Workshop  (John Smith)";

            // Act
            string result = input.ExtractFromParentheses(pullFromInside: false);

            // Assert
            Assert.AreEqual("Pottery Workshop", result);
        }

        [TestMethod]
        public void ExtractFromParentheses_MultipleParentheses_UsesFirst()
        {
            // Arrange
            string input = "Pottery (John Smith) (Jane Doe)";

            // Act
            string resultInside = input.ExtractFromParentheses(pullFromInside: true);
            string resultOutside = input.ExtractFromParentheses(pullFromInside: false);

            // Assert
            // Should extract content between first opening and first closing parenthesis
            Assert.AreEqual("John Smith", resultInside);
            Assert.AreEqual("Pottery", resultOutside);
        }

        #endregion

        #region GetLeaderName Tests

        [TestMethod]
        public void GetLeaderName_WithValidFormat_ExtractsLeader()
        {
            // Arrange
            string input = "Pottery (John Smith)";

            // Act
            string result = input.GetLeaderName();

            // Assert
            Assert.AreEqual("John Smith", result);
        }

        [TestMethod]
        public void GetLeaderName_NoParentheses_ReturnsEmpty()
        {
            // Arrange
            string input = "Pottery Workshop";

            // Act
            string result = input.GetLeaderName();

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void GetLeaderName_WithMultipleNames_ExtractsAll()
        {
            // Arrange
            string input = "Pottery (John Smith & Jane Doe)";

            // Act
            string result = input.GetLeaderName();

            // Assert
            Assert.AreEqual("John Smith & Jane Doe", result);
        }

        #endregion

        #region GetWorkshopName Tests

        [TestMethod]
        public void GetWorkshopName_WithValidFormat_ExtractsWorkshopName()
        {
            // Arrange
            string input = "Pottery (John Smith)";

            // Act
            string result = input.GetWorkshopName();

            // Assert
            Assert.AreEqual("Pottery", result);
        }

        [TestMethod]
        public void GetWorkshopName_NoParentheses_ReturnsEmpty()
        {
            // Arrange
            string input = "Pottery Workshop";

            // Act
            string result = input.GetWorkshopName();

            // Assert
            Assert.AreEqual(string.Empty, result);
        }

        [TestMethod]
        public void GetWorkshopName_WithLongName_ExtractsFullName()
        {
            // Arrange
            string input = "Advanced Pottery and Ceramics Workshop (John Smith)";

            // Act
            string result = input.GetWorkshopName();

            // Assert
            Assert.AreEqual("Advanced Pottery and Ceramics Workshop", result);
        }

        [TestMethod]
        public void GetWorkshopName_WithSpecialCharacters_ExtractsCorrectly()
        {
            // Arrange
            string input = "Art & Craft: Pottery (John Smith)";

            // Act
            string result = input.GetWorkshopName();

            // Assert
            Assert.AreEqual("Art & Craft: Pottery", result);
        }

        #endregion

        #region Edge Cases and Integration

        [TestMethod]
        public void ExtractFromParentheses_ComplexRealWorldExample_ParsesCorrectly()
        {
            // Arrange
            string input = "Woodworking & Carpentry Basics (Bob Johnson, Master Craftsman)";

            // Act
            string workshopName = input.GetWorkshopName();
            string leaderName = input.GetLeaderName();

            // Assert
            Assert.AreEqual("Woodworking & Carpentry Basics", workshopName);
            Assert.AreEqual("Bob Johnson, Master Craftsman", leaderName);
        }

        [TestMethod]
        public void ToProper_WithApostrophes_HandlesCorrectly()
        {
            // Arrange
            string input = "john's pottery class";

            // Act
            string result = input.ToProper();

            // Assert
            Assert.AreEqual("John's Pottery Class", result);
        }

        #endregion
    }
}
