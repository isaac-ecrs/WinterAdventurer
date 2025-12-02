using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinterAdventurer.Library;
using PdfSharp.Fonts;

namespace WinterAdventurer.Test
{
    [TestClass]
    public class CustomFontResolverTests
    {
        private CustomFontResolver _resolver = null!;

        [TestInitialize]
        public void Setup()
        {
            _resolver = new CustomFontResolver();
        }

        #region ResolveTypeface Tests - NotoSans

        [TestMethod]
        public void ResolveTypeface_NotoSansRegular_ReturnsCorrectFontInfo()
        {
            // Act
            var result = _resolver.ResolveTypeface("NotoSans", isBold: false, isItalic: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("NotoSans-Regular", result.FaceName);
        }

        [TestMethod]
        public void ResolveTypeface_NotoSansBold_ReturnsCorrectFontInfo()
        {
            // Act
            var result = _resolver.ResolveTypeface("NotoSans", isBold: true, isItalic: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("NotoSans-Bold", result.FaceName);
        }

        [TestMethod]
        public void ResolveTypeface_NotoAlias_ReturnsNotoSansRegular()
        {
            // Act
            var result = _resolver.ResolveTypeface("Noto", isBold: false, isItalic: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("NotoSans-Regular", result.FaceName);
        }

        [TestMethod]
        public void ResolveTypeface_ArialAlias_FallsBackToNotoSans()
        {
            // Act
            var result = _resolver.ResolveTypeface("Arial", isBold: false, isItalic: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("NotoSans-Regular", result.FaceName);
        }

        [TestMethod]
        public void ResolveTypeface_ArialBold_FallsBackToNotoSansBold()
        {
            // Act
            var result = _resolver.ResolveTypeface("Arial", isBold: true, isItalic: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("NotoSans-Bold", result.FaceName);
        }

        #endregion

        #region ResolveTypeface Tests - Oswald

        [TestMethod]
        public void ResolveTypeface_OswaldRegular_ReturnsCorrectFontInfo()
        {
            // Act
            var result = _resolver.ResolveTypeface("Oswald", isBold: false, isItalic: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Oswald-Regular", result.FaceName);
        }

        [TestMethod]
        public void ResolveTypeface_OswaldBold_ReturnsCorrectFontInfo()
        {
            // Act
            var result = _resolver.ResolveTypeface("Oswald", isBold: true, isItalic: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Oswald-Bold", result.FaceName);
        }

        #endregion

        #region ResolveTypeface Tests - Roboto

        [TestMethod]
        public void ResolveTypeface_RobotoRegular_ReturnsCorrectFontInfo()
        {
            // Act
            var result = _resolver.ResolveTypeface("Roboto", isBold: false, isItalic: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Roboto-Regular", result.FaceName);
        }

        [TestMethod]
        public void ResolveTypeface_RobotoBold_ReturnsCorrectFontInfo()
        {
            // Act
            var result = _resolver.ResolveTypeface("Roboto", isBold: true, isItalic: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Roboto-Bold", result.FaceName);
        }

        #endregion

        #region ResolveTypeface Tests - Case Insensitivity

        [TestMethod]
        public void ResolveTypeface_UppercaseFontName_ResolvesCaseInsensitively()
        {
            // Act
            var result = _resolver.ResolveTypeface("NOTOSANS", isBold: false, isItalic: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("NotoSans-Regular", result.FaceName);
        }

        [TestMethod]
        public void ResolveTypeface_MixedCaseFontName_ResolvesCaseInsensitively()
        {
            // Act
            var result = _resolver.ResolveTypeface("OsWaLd", isBold: true, isItalic: false);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Oswald-Bold", result.FaceName);
        }

        #endregion

        #region ResolveTypeface Tests - Unknown Fonts

        [TestMethod]
        public void ResolveTypeface_UnknownFont_FallsBackToNotoSans()
        {
            // Act
            var result = _resolver.ResolveTypeface("UnknownFont", isBold: false, isItalic: false);

            // Assert
            Assert.IsNotNull(result);
            // Should fall back to NotoSans-Regular
            Assert.AreEqual("NotoSans-Regular", result.FaceName);
        }

        #endregion

        #region ResolveTypeface Tests - Italic Flag

        [TestMethod]
        public void ResolveTypeface_ItalicFlag_IgnoredForNow()
        {
            // Note: Current implementation ignores italic flag, only checks bold
            // Act
            var regularResult = _resolver.ResolveTypeface("NotoSans", isBold: false, isItalic: true);
            var boldResult = _resolver.ResolveTypeface("NotoSans", isBold: true, isItalic: true);

            // Assert
            Assert.AreEqual("NotoSans-Regular", regularResult.FaceName);
            Assert.AreEqual("NotoSans-Bold", boldResult.FaceName);
        }

        #endregion

        #region GetFont Tests

        [TestMethod]
        public void GetFont_NotoSansRegular_ReturnsNonEmptyByteArray()
        {
            // Act
            var result = _resolver.GetFont("NotoSans-Regular");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result, "Font byte array should not be empty");
            // TTF files typically start with specific byte sequences
            Assert.IsTrue(result.Length > 100, "Font file should be reasonably sized");
        }

        [TestMethod]
        public void GetFont_NotoSansBold_ReturnsNonEmptyByteArray()
        {
            // Act
            var result = _resolver.GetFont("NotoSans-Bold");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
        }

        [TestMethod]
        public void GetFont_OswaldRegular_ReturnsNonEmptyByteArray()
        {
            // Act
            var result = _resolver.GetFont("Oswald-Regular");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
        }

        [TestMethod]
        public void GetFont_OswaldBold_ReturnsNonEmptyByteArray()
        {
            // Act
            var result = _resolver.GetFont("Oswald-Bold");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
        }

        [TestMethod]
        public void GetFont_RobotoRegular_ReturnsNonEmptyByteArray()
        {
            // Act
            var result = _resolver.GetFont("Roboto-Regular");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
        }

        [TestMethod]
        public void GetFont_RobotoBold_ReturnsNonEmptyByteArray()
        {
            // Act
            var result = _resolver.GetFont("Roboto-Bold");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
        }

        [TestMethod]
        public void GetFont_UnknownFontName_ThrowsInvalidOperationException()
        {
            // Act & Assert
            Assert.ThrowsExactly<InvalidOperationException>(() =>
            {
                _resolver.GetFont("NonExistentFont");
            });
        }

        [TestMethod]
        public void GetFont_CaseInsensitiveLookup_LoadsFont()
        {
            // Act
            var result = _resolver.GetFont("NOTOSANS-REGULAR");

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
        }

        #endregion

        #region Font Data Validation Tests

        [TestMethod]
        public void GetFont_MultipleCalls_ReturnsSameData()
        {
            // Act
            var result1 = _resolver.GetFont("NotoSans-Regular");
            var result2 = _resolver.GetFont("NotoSans-Regular");

            // Assert
            Assert.AreEqual(result1.Length, result2.Length);
            CollectionAssert.AreEqual(result1, result2);
        }

        [TestMethod]
        public void GetFont_DifferentFonts_ReturnDifferentData()
        {
            // Act
            var notoSans = _resolver.GetFont("NotoSans-Regular");
            var oswald = _resolver.GetFont("Oswald-Regular");

            // Assert
            Assert.AreNotEqual(notoSans.Length, oswald.Length);
            CollectionAssert.AreNotEqual(notoSans, oswald);
        }

        [TestMethod]
        public void GetFont_RegularVsBold_ReturnDifferentData()
        {
            // Act
            var regular = _resolver.GetFont("NotoSans-Regular");
            var bold = _resolver.GetFont("NotoSans-Bold");

            // Assert
            // Regular and Bold should be different font files
            Assert.AreNotEqual(regular.Length, bold.Length);
        }

        #endregion
    }
}
