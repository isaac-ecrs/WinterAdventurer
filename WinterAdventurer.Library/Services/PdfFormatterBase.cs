using System.IO;
using System.Reflection;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using Microsoft.Extensions.Logging;
using WinterAdventurer.Library.Models;

namespace WinterAdventurer.Library.Services
{
    /// <summary>
    /// Abstract base class providing shared PDF formatting utilities for document generation.
    /// Contains common functionality for adding logos, footers, facility maps, and setting margins.
    /// </summary>
    public abstract partial class PdfFormatterBase
    {
        /// <summary>
        /// Logger instance for diagnostic information, warnings, and error reporting during PDF generation.
        /// </summary>
        protected readonly ILogger _logger;

        /// <summary>
        /// Standard black color used throughout PDF document generation.
        /// </summary>
        protected readonly Color COLOR_BLACK = Color.FromRgb(0, 0, 0);

        /// <summary>
        /// Initializes a new instance of the PdfFormatterBase class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        protected PdfFormatterBase(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Sets standard 0.5 inch margins on all sides of a PDF section.
        /// </summary>
        /// <param name="section">The section to configure</param>
        protected void SetStandardMargins(Section section)
        {
            section.PageSetup.TopMargin = PdfLayoutConstants.Margins.Standard;
            section.PageSetup.LeftMargin = PdfLayoutConstants.Margins.Standard;
            section.PageSetup.RightMargin = PdfLayoutConstants.Margins.Standard;
            section.PageSetup.BottomMargin = PdfLayoutConstants.Margins.Standard;
        }

        /// <summary>
        /// Adds ECRS logo to PDF section with position/size determined by document type.
        /// Logo is loaded from embedded resources and positioned appropriately for portrait or landscape layouts.
        /// </summary>
        /// <param name="section">MigraDoc section to add logo to.</param>
        /// <param name="documentType">Type of document ("roster", "individual", or "master") determining logo position.</param>
        protected void AddLogoToSection(Section section, string documentType = "roster")
        {
            try
            {
                // Load logo from embedded resources
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "WinterAdventurer.Library.Resources.Images.ECRS_Logo_Minimal_Gray.png";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        // Save to temporary file (MigraDoc requires file path for images)
                        var tempPath = Path.Combine(Path.GetTempPath(), "ecrs_logo_temp.png");
                        using (var fileStream = File.Create(tempPath))
                        {
                            stream.CopyTo(fileStream);
                        }

                        // Add logo with position/size based on document type
                        var logo = section.AddImage(tempPath);
                        logo.LockAspectRatio = true;
                        logo.RelativeVertical = RelativeVertical.Page;
                        logo.RelativeHorizontal = RelativeHorizontal.Margin;
                        logo.WrapFormat.Style = WrapStyle.Through;

                        // Adjust size and position based on document type
                        if (documentType == "individual")
                        {
                            // Individual schedules are landscape - logo on far right
                            logo.Height = PdfLayoutConstants.Logo.Height;
                            logo.Top = PdfLayoutConstants.Logo.MasterScheduleLandscape.Top;
                            logo.Left = PdfLayoutConstants.Logo.MasterScheduleLandscape.Left; // Far right for landscape
                        }
                        else if (documentType == "master")
                        {
                            // Master schedule - check orientation for proper logo placement
                            logo.Height = PdfLayoutConstants.Logo.Height;

                            if (section.PageSetup.Orientation == Orientation.Landscape)
                            {
                                logo.Top = PdfLayoutConstants.Logo.MasterScheduleLandscape.Top;
                                logo.Left = PdfLayoutConstants.Logo.MasterScheduleLandscape.Left;
                            }
                            else
                            {
                                logo.Top = PdfLayoutConstants.Logo.WorkshopRosterPortrait.Top;
                                logo.Left = PdfLayoutConstants.Logo.WorkshopRosterPortrait.Left;
                            }
                        }
                        else // roster (default)
                        {
                            // Class rosters - portrait, bottom right to avoid overlapping long workshop names
                            // Page is 11" tall with 0.5" margins = 10" content area
                            // Position at 10" - 1.0" logo - 0.2" margin = 8.8" from top
                            logo.Height = PdfLayoutConstants.Logo.Height;
                            logo.Top = PdfLayoutConstants.Logo.IndividualScheduleBottom.Top;
                            logo.Left = PdfLayoutConstants.Logo.IndividualScheduleBottom.Left;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail PDF generation
                LogWarningErrorAddingLogo(ex, documentType);
            }
        }

        /// <summary>
        /// Adds a footer to the section with the event name centered at the bottom
        /// </summary>
        protected void AddEventNameFooter(Section section, string eventName)
        {
            var footer = section.Footers.Primary;
            var paragraph = footer.AddParagraph();
            paragraph.Format.Font.Name = FontNames.NotoSans;
            paragraph.Format.Font.Size = PdfLayoutConstants.FontSizes.EventFooter;
            paragraph.Format.Alignment = ParagraphAlignment.Center;
            paragraph.AddText(eventName);
        }

        /// <summary>
        /// Adds Watson facility map image to PDF section to help participants navigate the venue.
        /// Map is centered and sized appropriately for the page layout.
        /// </summary>
        /// <param name="section">MigraDoc section to add the map to.</param>
        protected void AddFacilityMapToSection(Section section)
        {
            try
            {
                // Load facility map from embedded resources
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "WinterAdventurer.Library.Resources.Images.watson_map.png";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        // Save to temporary file (MigraDoc requires file path for images)
                        var tempPath = Path.Combine(Path.GetTempPath(), "watson_map_temp.png");
                        using (var fileStream = File.Create(tempPath))
                        {
                            stream.CopyTo(fileStream);
                        }

                        // Add spacing before map
                        section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(8);

                        // Add facility map centered
                        var mapParagraph = section.AddParagraph();
                        mapParagraph.Format.Alignment = ParagraphAlignment.Center;
                        var map = mapParagraph.AddImage(tempPath);
                        map.LockAspectRatio = true;
                        map.Width = PdfLayoutConstants.FacilityMap.Width; // Smaller map to fit on one page
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail PDF generation
                LogWarningErrorAddingFacilityMap(ex);
            }
        }

        #region Logging

        [LoggerMessage(
            EventId = 6001,
            Level = LogLevel.Warning,
            Message = "Error adding logo to PDF section (type: {documentType})"
        )]
        private partial void LogWarningErrorAddingLogo(Exception ex, string documentType);

        [LoggerMessage(
            EventId = 6002,
            Level = LogLevel.Warning,
            Message = "Error adding facility map to PDF section"
        )]
        private partial void LogWarningErrorAddingFacilityMap(Exception ex);

        #endregion
    }
}
