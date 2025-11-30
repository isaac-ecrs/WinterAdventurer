using MigraDoc.DocumentObjectModel;

namespace WinterAdventurer.Library
{
    /// <summary>
    /// Centralized constants for PDF layout and formatting.
    /// Replaces magic numbers throughout the codebase to improve maintainability.
    /// </summary>
    public static class PdfLayoutConstants
    {
        /// <summary>
        /// Standard page margins used across all PDF types
        /// </summary>
        public static class Margins
        {
            /// <summary>
            /// Standard margin for all sides (0.5 inch)
            /// </summary>
            public static readonly Unit Standard = Unit.FromInch(0.5);
        }

        /// <summary>
        /// Font sizes used throughout PDF generation
        /// </summary>
        public static class FontSizes
        {
            /// <summary>
            /// Font sizes for workshop roster pages
            /// </summary>
            public static class WorkshopRoster
            {
                public const int WorkshopTitle = 25;
                public const int LeaderInfo = 18;
                public const int LocationInfo = 16;
                public const int PeriodInfo = 14;
                public const int SectionHeader = 18;
                public const int BackupNote = 14;
                public const int ParticipantName = 15;
            }

            /// <summary>
            /// Font sizes for individual participant schedules
            /// </summary>
            public static class IndividualSchedule
            {
                public const int ParticipantName = 24;
                public const int PeriodHeader = 14;
                public const int DayIndicator = 14;
                public const int TimeSlot = 12;
                public const int WorkshopName = 14;
                public const int LeaderName = 12;
                public const int LocationName = 12;
                public const int NameFieldLabel = 11;
            }

            /// <summary>
            /// Font sizes for master schedule grid
            /// </summary>
            public static class MasterSchedule
            {
                public const int Title = 28;
                public const int ColumnHeader = 12;
                public const int TimeCell = 10;
                public const int DayIndicator = 9;
                public const int ActivityName = 14;
                public const int WorkshopInfo = 10;
                public const int LeaderName = 9;
                public const int LocationHeader = 11;
            }

            /// <summary>
            /// Font sizes for blank schedules
            /// </summary>
            public static class BlankSchedule
            {
                public const int Title = 16;
                public const int TimeSlot = 12;
            }

            /// <summary>
            /// Font size for event name footer across all document types
            /// </summary>
            public const int EventFooter = 9;
        }

        /// <summary>
        /// Column widths for tables in PDFs
        /// </summary>
        public static class ColumnWidths
        {
            /// <summary>
            /// Width of participant name column in roster (3.25 inches - roughly half page width with margins)
            /// </summary>
            public static readonly Unit ParticipantColumn = Unit.FromInch(3.25);

            /// <summary>
            /// Individual schedule table column widths
            /// </summary>
            public static class IndividualSchedule
            {
                /// <summary>
                /// Time column width (1.8 inches)
                /// </summary>
                public const double Time = 1.8;

                /// <summary>
                /// Day indicator column width (1.85 inches)
                /// </summary>
                public const double Day = 1.85;
            }

            /// <summary>
            /// Master schedule grid column widths
            /// </summary>
            public static class MasterSchedule
            {
                /// <summary>
                /// Time column width (1.5 inches)
                /// </summary>
                public const double Time = 1.5;

                /// <summary>
                /// Days indicator column width (0.8 inches)
                /// </summary>
                public const double Days = 0.8;

                /// <summary>
                /// Location column width (2.0 inches)
                /// </summary>
                public const double Location = 2.0;
            }
        }

        /// <summary>
        /// Logo positioning and sizing constants
        /// </summary>
        public static class Logo
        {
            /// <summary>
            /// Standard logo height across all document types (1.0 inch)
            /// </summary>
            public static readonly Unit Height = Unit.FromInch(1.0);

            /// <summary>
            /// Logo positioning for landscape master schedule (top-right)
            /// </summary>
            public static class MasterScheduleLandscape
            {
                public static readonly Unit Top = Unit.FromInch(0.2);
                public static readonly Unit Left = Unit.FromInch(8.5);
            }

            /// <summary>
            /// Logo positioning for portrait workshop roster (top-right)
            /// </summary>
            public static class WorkshopRosterPortrait
            {
                public static readonly Unit Top = Unit.FromInch(0.15);
                public static readonly Unit Left = Unit.FromInch(6.0);
            }

            /// <summary>
            /// Logo positioning for individual schedules (bottom-right)
            /// </summary>
            public static class IndividualScheduleBottom
            {
                public static readonly Unit Top = Unit.FromInch(8.8);
                public static readonly Unit Left = Unit.FromInch(5.5);
            }
        }

        /// <summary>
        /// Facility map dimensions
        /// </summary>
        public static class FacilityMap
        {
            /// <summary>
            /// Map width when embedded in individual schedules (6.0 inches)
            /// </summary>
            public static readonly Unit Width = Unit.FromInch(6.0);
        }

        /// <summary>
        /// Table layout constants
        /// </summary>
        public static class Table
        {
            /// <summary>
            /// Left indent for individual schedule tables (0.3 inches)
            /// </summary>
            public const double IndividualScheduleLeftIndent = 0.3;

            /// <summary>
            /// Standard table border width (0.5 points)
            /// </summary>
            public const double BorderWidth = 0.5;

            /// <summary>
            /// Line spacing multiplier for participant lists (110% of font size)
            /// Provides extra room for text wrapping without excessive vertical space
            /// </summary>
            public const double ParticipantListLineSpacing = 1.1;

            /// <summary>
            /// Number of columns for two-column participant layouts
            /// </summary>
            public const int TwoColumnCount = 2;
        }

        /// <summary>
        /// Spacing constants for various PDF elements
        /// </summary>
        public static class Spacing
        {
            /// <summary>
            /// Space after period/section information (16 points)
            /// </summary>
            public static readonly Unit SectionSpacing = Unit.FromPoint(16);

            /// <summary>
            /// Space after attendee header in individual schedules (12 points)
            /// </summary>
            public static readonly Unit HeaderSpacing = Unit.FromPoint(12);

            /// <summary>
            /// Left indent for backup/alternate notes (12 points)
            /// </summary>
            public static readonly Unit BackupNoteIndent = Unit.FromPoint(12);
        }

        /// <summary>
        /// Adaptive font sizing for participant names
        /// Automatically reduces font size for longer names to prevent wrapping
        /// </summary>
        public static class AdaptiveFontSizing
        {
            /// <summary>
            /// Text length thresholds for adaptive sizing
            /// </summary>
            public static class TextLengthThresholds
            {
                public const int VeryLong = 45;
                public const int Long = 35;
                public const int Medium = 28;
            }

            /// <summary>
            /// Corresponding font sizes for each threshold
            /// </summary>
            public static class ParticipantFontSizes
            {
                public const int VeryLong = 10;
                public const int Long = 11;
                public const int Medium = 13;
                public const int Default = 15;
            }

            /// <summary>
            /// Corresponding font sizes for workshop names based on length
            /// Prevents text wrapping in schedule cells while maintaining readability
            /// </summary>
            public static class WorkshopFontSizes
            {
                public const int VeryLong = 10;  // 45+ characters
                public const int Long = 11;      // 35-44 characters
                public const int Medium = 12;    // 28-34 characters
                public const int Default = 14;   // <28 characters
            }
        }

        /// <summary>
        /// Page dimensions and usable widths
        /// </summary>
        public static class PageDimensions
        {
            /// <summary>
            /// Usable page width for landscape orientation (after margins)
            /// </summary>
            public const double LandscapeUsableWidth = 10.0;

            /// <summary>
            /// Usable page width for portrait orientation (after margins)
            /// </summary>
            public const double PortraitUsableWidth = 7.5;
        }
    }
}
