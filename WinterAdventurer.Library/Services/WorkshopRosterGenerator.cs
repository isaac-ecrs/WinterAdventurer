// <copyright file="WorkshopRosterGenerator.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using Microsoft.Extensions.Logging;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using WinterAdventurer.Library.Models;

namespace WinterAdventurer.Library.Services
{
    /// <summary>
    /// Generates workshop roster PDFs for leaders showing enrolled and backup participants.
    /// Creates one section per workshop with two-column participant lists and adaptive font sizing.
    /// </summary>
    public class WorkshopRosterGenerator : PdfFormatterBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkshopRosterGenerator"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        public WorkshopRosterGenerator(ILogger<WorkshopRosterGenerator> logger)
            : base(logger)
        {
        }

        /// <summary>
        /// Generates roster sections for all workshops.
        /// Each section includes workshop header, leader info, location, period/duration, and participant lists.
        /// </summary>
        /// <param name="workshops">List of workshops to generate rosters for.</param>
        /// <param name="eventName">Name of the event displayed in section footers.</param>
        /// <param name="timeslots">Timeslots to find period time ranges. If null, time ranges are omitted from rosters.</param>
        /// <returns>List of MigraDoc Section objects, one per workshop.</returns>
        public List<Section> GenerateRosterSections(List<Workshop> workshops, string eventName, List<TimeSlot>? timeslots = null)
        {
            var sections = new List<Section>();

            foreach (var workshopListing in workshops)
            {
                var section = new Section();

                // Add logo to section
                AddLogoToSection(section, "roster");

                // Workshop name - Oswald (top level header)
                var header = section.AddParagraph();
                header.Format.Font.Name = FontNames.Oswald;
                header.Format.Font.Color = COLOR_BLACK;
                header.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.WorkshopTitle;
                header.Format.Alignment = ParagraphAlignment.Center;
                header.AddFormattedText(workshopListing.Name, TextFormat.Bold);

                // Leader info - Noto Sans (second level header)
                var leaderInfo = section.AddParagraph();
                leaderInfo.Format.Font.Name = FontNames.NotoSans;
                leaderInfo.Format.Font.Color = COLOR_BLACK;
                leaderInfo.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.LeaderInfo;
                leaderInfo.Format.Alignment = ParagraphAlignment.Center;
                leaderInfo.AddFormattedText(workshopListing.Leader);

                // Location info - Noto Sans
                if (!string.IsNullOrWhiteSpace(workshopListing.Location))
                {
                    var locationInfo = section.AddParagraph();
                    locationInfo.Format.Font.Name = FontNames.NotoSans;
                    locationInfo.Format.Font.Color = COLOR_BLACK;
                    locationInfo.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.LocationInfo;
                    locationInfo.Format.Alignment = ParagraphAlignment.Center;
                    locationInfo.AddFormattedText($"Location: {workshopListing.Location}");
                }

                // Period and duration info - Noto Sans (second level header)
                var periodInfo = section.AddParagraph();
                periodInfo.Format.Font.Name = FontNames.NotoSans;
                periodInfo.Format.Font.Color = COLOR_BLACK;
                periodInfo.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.PeriodInfo;
                periodInfo.Format.Alignment = ParagraphAlignment.Center;

                // Find the timeslot for this period to get the time range
                var periodTimeslot = timeslots?.FirstOrDefault(t =>
                    t.IsPeriod && t.Label == workshopListing.Period.DisplayName);

                string periodText = workshopListing.Period.DisplayName;
                if (periodTimeslot != null && !string.IsNullOrEmpty(periodTimeslot.TimeRange))
                {
                    periodText += $" ({periodTimeslot.TimeRange})";
                }

                periodText += $" - {workshopListing.Duration.Description}";

                periodInfo.AddFormattedText(periodText);
                periodInfo.Format.SpaceAfter = PdfLayoutConstants.Spacing.SectionSpacing;

                // Separate first choice from backup choices and sort by last name
                var firstChoiceAttendees = workshopListing.Selections
                    .Where(s => s.ChoiceNumber == 1)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ToList();
                var backupAttendees = workshopListing.Selections
                    .Where(s => s.ChoiceNumber > 1)
                    .OrderBy(s => s.LastName)
                    .ThenBy(s => s.FirstName)
                    .ThenBy(s => s.ChoiceNumber)
                    .ToList();

                // First choice participants
                if (firstChoiceAttendees.Count > 0)
                {
                    var firstChoiceHeader = section.AddParagraph();
                    firstChoiceHeader.Format.Font.Name = FontNames.NotoSans;
                    firstChoiceHeader.Format.Font.Color = COLOR_BLACK;
                    firstChoiceHeader.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.SectionHeader;
                    firstChoiceHeader.AddFormattedText($"Enrolled Participants ({firstChoiceAttendees.Count}):", TextFormat.Bold);
                    firstChoiceHeader.Format.SpaceAfter = Unit.FromPoint(8);

                    AddTwoColumnParticipantList(section, firstChoiceAttendees, showChoiceNumber: false);
                }

                // Backup participants
                if (backupAttendees.Count > 0)
                {
                    var backupHeader = section.AddParagraph();
                    backupHeader.Format.Font.Name = FontNames.NotoSans;
                    backupHeader.Format.Font.Color = COLOR_BLACK;
                    backupHeader.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.SectionHeader;
                    backupHeader.Format.SpaceAfter = Unit.FromPoint(8);
                    backupHeader.Format.SpaceBefore = PdfLayoutConstants.Spacing.SectionSpacing;
                    backupHeader.AddFormattedText($"Backup/Alternate Choices ({backupAttendees.Count}):", TextFormat.Bold);

                    var backupNote = section.AddParagraph();
                    backupNote.Format.Font.Name = FontNames.Roboto;
                    backupNote.Format.Font.Color = COLOR_BLACK;
                    backupNote.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.BackupNote;
                    backupNote.Format.Font.Italic = true;
                    backupNote.Format.LeftIndent = PdfLayoutConstants.Spacing.BackupNoteIndent;
                    backupNote.Format.SpaceAfter = Unit.FromPoint(8);
                    backupNote.AddText("These participants may join if their first choice is full:");

                    AddTwoColumnParticipantList(section, backupAttendees, showChoiceNumber: true);
                }

                // Add event name footer
                AddEventNameFooter(section, eventName);

                sections.Add(section);
            }

            return sections;
        }

        /// <summary>
        /// Adds a two-column participant list table to maximize roster space efficiency.
        /// Applies adaptive font sizing and includes optional choice numbers for backup participants.
        /// </summary>
        /// <param name="section">MigraDoc section to add the table to.</param>
        /// <param name="participants">Workshop selections to display in the list.</param>
        /// <param name="showChoiceNumber">Whether to display choice numbers (e.g., "Choice #2") after participant names.</param>
        private void AddTwoColumnParticipantList(Section section, List<WorkshopSelection> participants, bool showChoiceNumber)
        {
            // Create a table with 2 columns
            var table = section.AddTable();
            table.Borders.Visible = false;

            // Define column widths (equal width)
            var columnWidth = PdfLayoutConstants.ColumnWidths.ParticipantColumn;
            table.AddColumn(columnWidth);
            table.AddColumn(columnWidth);

            // Split participants into two columns
            int halfCount = (int)Math.Ceiling(participants.Count / (double)PdfLayoutConstants.Table.TwoColumnCount);
            var leftColumn = participants.Take(halfCount).ToList();
            var rightColumn = participants.Skip(halfCount).ToList();

            // Add rows (one row per participant in left column)
            for (int i = 0; i < leftColumn.Count; i++)
            {
                var row = table.AddRow();
                row.VerticalAlignment = VerticalAlignment.Top;
                row.TopPadding = 0;
                row.BottomPadding = 0;

                // Left column participant
                var leftCell = row.Cells[0];
                leftCell.VerticalAlignment = VerticalAlignment.Top;
                var leftPara = leftCell.AddParagraph();
                leftPara.Format.Font.Name = FontNames.Roboto;
                leftPara.Format.Font.Color = COLOR_BLACK;

                int leftCounter = i + 1;
                int leftFontSize = GetParticipantFontSize(
                    leftColumn[i].FullName,
                    leftCounter,
                    showChoiceNumber,
                    leftColumn[i].ChoiceNumber);

                leftPara.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.ParticipantName;
                leftPara.Format.LeftIndent = Unit.FromPoint(6);
                leftPara.Format.SpaceBefore = 0;
                leftPara.Format.SpaceAfter = Unit.FromPoint(6);
                leftPara.Format.LineSpacingRule = LineSpacingRule.Multiple;
                leftPara.Format.LineSpacing = PdfLayoutConstants.Table.ParticipantListLineSpacing;

                // Add number at base size (bold)
                leftPara.AddFormattedText($"{leftCounter}. ", TextFormat.Bold);

                // Add name at calculated size
                var nameText = leftPara.AddFormattedText(leftColumn[i].FullName);
                nameText.Font.Size = leftFontSize;

                if (showChoiceNumber)
                {
                    var choiceText = leftPara.AddFormattedText($" (Choice #{leftColumn[i].ChoiceNumber})");
                    choiceText.Font.Size = leftFontSize;
                }

                var checkboxText = leftPara.AddFormattedText(" [\u2003]");
                checkboxText.Font.Size = leftFontSize;

                // Right column participant (if exists)
                if (i < rightColumn.Count)
                {
                    var rightCell = row.Cells[1];
                    rightCell.VerticalAlignment = VerticalAlignment.Top;
                    var rightPara = rightCell.AddParagraph();
                    rightPara.Format.Font.Name = FontNames.Roboto;
                    rightPara.Format.Font.Color = COLOR_BLACK;

                    int rightCounter = halfCount + i + 1;
                    int rightFontSize = GetParticipantFontSize(
                        rightColumn[i].FullName,
                        rightCounter,
                        showChoiceNumber,
                        rightColumn[i].ChoiceNumber);

                    rightPara.Format.Font.Size = PdfLayoutConstants.FontSizes.WorkshopRoster.ParticipantName;
                    rightPara.Format.LeftIndent = Unit.FromPoint(6);
                    rightPara.Format.SpaceBefore = 0;
                    rightPara.Format.SpaceAfter = Unit.FromPoint(6);
                    rightPara.Format.LineSpacingRule = LineSpacingRule.Multiple;
                    rightPara.Format.LineSpacing = PdfLayoutConstants.Table.ParticipantListLineSpacing;

                    // Add number at base size (bold)
                    rightPara.AddFormattedText($"{rightCounter}. ", TextFormat.Bold);

                    // Add name at calculated size
                    var rightNameText = rightPara.AddFormattedText(rightColumn[i].FullName);
                    rightNameText.Font.Size = rightFontSize;

                    if (showChoiceNumber)
                    {
                        var rightChoiceText = rightPara.AddFormattedText($" (Choice #{rightColumn[i].ChoiceNumber})");
                        rightChoiceText.Font.Size = rightFontSize;
                    }

                    var rightCheckboxText = rightPara.AddFormattedText(" [\u2003]");
                    rightCheckboxText.Font.Size = rightFontSize;
                }
            }
        }

        /// <summary>
        /// Calculates adaptive font size for participant names to prevent text wrapping in roster columns.
        /// Uses progressively smaller fonts for longer names based on total text length including number and checkbox.
        /// </summary>
        /// <param name="fullName">Participant's full name.</param>
        /// <param name="counter">Numbered position in the list.</param>
        /// <param name="showChoiceNumber">Whether to include choice number in length calculation.</param>
        /// <param name="choiceNumber">Choice number (1=first choice, 2+=backup) if applicable.</param>
        /// <returns>Font size in points (10, 11, 13, or 15) based on calculated text length.</returns>
        private int GetParticipantFontSize(string fullName, int counter, bool showChoiceNumber, int? choiceNumber = null)
        {
            // Calculate full text length including number, name, choice indicator, and checkbox
            string text = $"{counter}. {fullName}";
            if (showChoiceNumber && choiceNumber.HasValue)
            {
                text += $" (Choice #{choiceNumber})";
            }

            text += " [\u2003]";  // Using em space for wider checkbox

            // Use progressively smaller fonts for longer names to prevent wrapping
            if (text.Length > PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.VeryLong)
            {
                return PdfLayoutConstants.AdaptiveFontSizing.ParticipantFontSizes.VeryLong;
            }

            if (text.Length > PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Long)
            {
                return PdfLayoutConstants.AdaptiveFontSizing.ParticipantFontSizes.Long;
            }

            if (text.Length > PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Medium)
            {
                return PdfLayoutConstants.AdaptiveFontSizing.ParticipantFontSizes.Medium;
            }

            return PdfLayoutConstants.AdaptiveFontSizing.ParticipantFontSizes.Default;
        }
    }
}
