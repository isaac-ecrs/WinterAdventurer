using Microsoft.VisualStudio.TestTools.UnitTesting;
using MigraDoc.DocumentObjectModel;
using WinterAdventurer.Library;

namespace WinterAdventurer.Test
{
    /// <summary>
    /// Tests for Constants, FontNames, and PdfLayoutConstants to ensure values are defined correctly.
    /// </summary>
    [TestClass]
    public class ConstantsTests
    {
        #region Constants Tests

        [TestMethod]
        public void Constants_SheetClassSelection_IsDefined()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.SHEET_CLASS_SELECTION));
            Assert.AreEqual("ClassSelection", Constants.SHEET_CLASS_SELECTION);
        }

        [TestMethod]
        public void Constants_HeaderSelectionId_IsDefined()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.HEADER_SELECTION_ID));
            Assert.AreEqual("ClassSelection_Id", Constants.HEADER_SELECTION_ID);
        }

        [TestMethod]
        public void Constants_HeaderFirstName_IsDefined()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.HEADER_FIRST_NAME));
            Assert.AreEqual("Name_First", Constants.HEADER_FIRST_NAME);
        }

        [TestMethod]
        public void Constants_HeaderLastName_IsDefined()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.HEADER_LAST_NAME));
            Assert.AreEqual("Name_Last", Constants.HEADER_LAST_NAME);
        }

        [TestMethod]
        public void Constants_AllHeaders_AreNonEmpty()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.HEADER_EMAIL));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.HEADER_AGE));
            Assert.IsFalse(string.IsNullOrWhiteSpace(Constants.HEADER_CHOICE_NUMBER));
        }

        #endregion

        #region FontNames Tests

        [TestMethod]
        public void FontNames_NotoSans_IsDefined()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(FontNames.NotoSans));
            Assert.AreEqual("NotoSans", FontNames.NotoSans);
        }

        [TestMethod]
        public void FontNames_Oswald_IsDefined()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(FontNames.Oswald));
            Assert.AreEqual("Oswald", FontNames.Oswald);
        }

        [TestMethod]
        public void FontNames_Roboto_IsDefined()
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(FontNames.Roboto));
            Assert.AreEqual("Roboto", FontNames.Roboto);
        }

        #endregion

        #region PdfLayoutConstants Tests

        [TestMethod]
        public void PdfLayoutConstants_StandardMargin_IsHalfInch()
        {
            var margin = PdfLayoutConstants.Margins.Standard;
            Assert.AreEqual(Unit.FromInch(0.5), margin);
        }

        [TestMethod]
        public void PdfLayoutConstants_FontSizes_WorkshopRoster_ArePositive()
        {
            Assert.IsTrue(PdfLayoutConstants.FontSizes.WorkshopRoster.WorkshopTitle > 0);
            Assert.IsTrue(PdfLayoutConstants.FontSizes.WorkshopRoster.LeaderInfo > 0);
            Assert.IsTrue(PdfLayoutConstants.FontSizes.WorkshopRoster.LocationInfo > 0);
            Assert.IsTrue(PdfLayoutConstants.FontSizes.WorkshopRoster.PeriodInfo > 0);
            Assert.IsTrue(PdfLayoutConstants.FontSizes.WorkshopRoster.SectionHeader > 0);
            Assert.IsTrue(PdfLayoutConstants.FontSizes.WorkshopRoster.ParticipantName > 0);
        }

        [TestMethod]
        public void PdfLayoutConstants_FontSizes_IndividualSchedule_ArePositive()
        {
            Assert.IsTrue(PdfLayoutConstants.FontSizes.IndividualSchedule.ParticipantName > 0);
            Assert.IsTrue(PdfLayoutConstants.FontSizes.IndividualSchedule.PeriodHeader > 0);
            Assert.IsTrue(PdfLayoutConstants.FontSizes.IndividualSchedule.DayIndicator > 0);
            Assert.IsTrue(PdfLayoutConstants.FontSizes.IndividualSchedule.TimeSlot > 0);
            Assert.IsTrue(PdfLayoutConstants.FontSizes.IndividualSchedule.WorkshopName > 0);
        }

        [TestMethod]
        public void PdfLayoutConstants_FontSizes_MasterSchedule_ArePositive()
        {
            Assert.IsTrue(PdfLayoutConstants.FontSizes.MasterSchedule.Title > 0);
            Assert.IsTrue(PdfLayoutConstants.FontSizes.MasterSchedule.ColumnHeader > 0);
            Assert.IsTrue(PdfLayoutConstants.FontSizes.MasterSchedule.TimeCell > 0);
            Assert.IsTrue(PdfLayoutConstants.FontSizes.MasterSchedule.WorkshopInfo > 0);
        }

        [TestMethod]
        public void PdfLayoutConstants_ColumnWidths_ArePositive()
        {
            Assert.IsTrue(PdfLayoutConstants.ColumnWidths.IndividualSchedule.Time > 0);
            Assert.IsTrue(PdfLayoutConstants.ColumnWidths.IndividualSchedule.Day > 0);
            Assert.IsTrue(PdfLayoutConstants.ColumnWidths.MasterSchedule.Time > 0);
            Assert.IsTrue(PdfLayoutConstants.ColumnWidths.MasterSchedule.Days > 0);
        }

        [TestMethod]
        public void PdfLayoutConstants_LogoHeight_IsPositive()
        {
            Assert.IsTrue(PdfLayoutConstants.Logo.Height.Inch > 0);
        }

        [TestMethod]
        public void PdfLayoutConstants_AdaptiveFontSizing_ThresholdsAreOrdered()
        {
            // Thresholds should be in descending order
            Assert.IsTrue(PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.VeryLong >
                         PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Long);
            Assert.IsTrue(PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Long >
                         PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Medium);
        }

        [TestMethod]
        public void PdfLayoutConstants_AdaptiveFontSizing_ParticipantFontSizes_AreOrdered()
        {
            // Font sizes should decrease as text gets longer
            Assert.IsTrue(PdfLayoutConstants.AdaptiveFontSizing.ParticipantFontSizes.Default >
                         PdfLayoutConstants.AdaptiveFontSizing.ParticipantFontSizes.Medium);
            Assert.IsTrue(PdfLayoutConstants.AdaptiveFontSizing.ParticipantFontSizes.Medium >
                         PdfLayoutConstants.AdaptiveFontSizing.ParticipantFontSizes.Long);
            Assert.IsTrue(PdfLayoutConstants.AdaptiveFontSizing.ParticipantFontSizes.Long >
                         PdfLayoutConstants.AdaptiveFontSizing.ParticipantFontSizes.VeryLong);
        }

        [TestMethod]
        public void PdfLayoutConstants_AdaptiveFontSizing_WorkshopFontSizes_AreOrdered()
        {
            // Font sizes should decrease as text gets longer
            Assert.IsTrue(PdfLayoutConstants.AdaptiveFontSizing.WorkshopFontSizes.Default >
                         PdfLayoutConstants.AdaptiveFontSizing.WorkshopFontSizes.Medium);
            Assert.IsTrue(PdfLayoutConstants.AdaptiveFontSizing.WorkshopFontSizes.Medium >
                         PdfLayoutConstants.AdaptiveFontSizing.WorkshopFontSizes.Long);
            Assert.IsTrue(PdfLayoutConstants.AdaptiveFontSizing.WorkshopFontSizes.Long >
                         PdfLayoutConstants.AdaptiveFontSizing.WorkshopFontSizes.VeryLong);
        }

        [TestMethod]
        public void PdfLayoutConstants_PageDimensions_AreReasonable()
        {
            // Portrait usable width should be less than landscape usable width
            Assert.IsTrue(PdfLayoutConstants.PageDimensions.PortraitUsableWidth <
                         PdfLayoutConstants.PageDimensions.LandscapeUsableWidth);

            // Usable widths should be positive and reasonable (less than full page width)
            Assert.IsTrue(PdfLayoutConstants.PageDimensions.PortraitUsableWidth > 0);
            Assert.IsTrue(PdfLayoutConstants.PageDimensions.PortraitUsableWidth < 8.5); // US Letter width
            Assert.IsTrue(PdfLayoutConstants.PageDimensions.LandscapeUsableWidth > 0);
            Assert.IsTrue(PdfLayoutConstants.PageDimensions.LandscapeUsableWidth < 11); // US Letter height
        }

        [TestMethod]
        public void PdfLayoutConstants_TableBorderWidth_IsPositive()
        {
            Assert.IsTrue(PdfLayoutConstants.Table.BorderWidth > 0);
        }

        [TestMethod]
        public void PdfLayoutConstants_TableLineSpacing_IsReasonable()
        {
            // Line spacing should be greater than 1.0 (single spacing) but not excessive
            Assert.IsTrue(PdfLayoutConstants.Table.ParticipantListLineSpacing > 1.0);
            Assert.IsTrue(PdfLayoutConstants.Table.ParticipantListLineSpacing < 2.0);
        }

        #endregion
    }
}
