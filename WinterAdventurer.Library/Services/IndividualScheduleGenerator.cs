using System.Collections.Generic;
using System.Linq;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using Microsoft.Extensions.Logging;
using WinterAdventurer.Library.EventSchemas;
using WinterAdventurer.Library.Models;

namespace WinterAdventurer.Library.Services
{
    /// <summary>
    /// Generates individual schedule PDFs for participants showing their workshops in a visual calendar format.
    /// Creates landscape-oriented schedule pages with workshop details, leader info, and locations.
    /// </summary>
    public partial class IndividualScheduleGenerator : PdfFormatterBase
    {
        private readonly EventSchema _schema;
        private readonly MasterScheduleGenerator _masterScheduleGenerator;
        private static readonly string[] LeaderDelimiter = new[] { " and " };

        /// <summary>
        /// Initializes a new instance of the IndividualScheduleGenerator class.
        /// </summary>
        /// <param name="schema">Event schema configuration defining period structure.</param>
        /// <param name="masterScheduleGenerator">Master schedule generator for blank schedule templates.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public IndividualScheduleGenerator(
            EventSchema schema,
            MasterScheduleGenerator masterScheduleGenerator,
            ILogger<IndividualScheduleGenerator> logger) : base(logger)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
            _masterScheduleGenerator = masterScheduleGenerator ?? throw new ArgumentNullException(nameof(masterScheduleGenerator));
        }

        /// <summary>
        /// Generates landscape-oriented individual schedule pages for each registered participant.
        /// Shows participant's workshops across all days and periods in a visual calendar format.
        /// </summary>
        /// <param name="workshops">List of all workshops to extract participant schedules from.</param>
        /// <param name="eventName">Name of the event displayed in section footers.</param>
        /// <param name="mergeWorkshopCells">Whether to merge table cells for multi-day workshops to show continuity.</param>
        /// <param name="timeslots">Timeslots defining schedule structure. If null, uses default timeslots from schema.</param>
        /// <returns>List of MigraDoc Section objects, one per participant.</returns>
        /// @TODO: break this up to smaller methods
        public List<Section> GenerateIndividualSchedules(
            List<Workshop> workshops,
            string eventName,
            bool mergeWorkshopCells = true,
            List<Models.TimeSlot>? timeslots = null)
        {
            var sections = new List<Section>();

            // If no timeslots provided, create default minimal set
            if (timeslots == null || timeslots.Count == 0)
            {
                timeslots = CreateDefaultTimeslots();
            }

            // Get all unique attendees from workshop selections (first choice only)
            var attendeeSchedules = new Dictionary<string, List<WorkshopSelection>>();

            foreach (var workshop in workshops)
            {
                foreach (var selection in workshop.Selections.Where(s => s.ChoiceNumber == 1))
                {
                    if (!attendeeSchedules.ContainsKey(selection.ClassSelectionId))
                    {
                        attendeeSchedules[selection.ClassSelectionId] = new List<WorkshopSelection>();
                    }
                    attendeeSchedules[selection.ClassSelectionId].Add(selection);
                }
            }

            // Create a schedule page for each attendee
            foreach (var kvp in attendeeSchedules.OrderBy(a => a.Value.First().LastName).ThenBy(a => a.Value.First().FirstName))
            {
                var attendeeSelections = kvp.Value;
                var attendee = attendeeSelections.First(); // Get attendee info from first selection

                var section = new Section();

                // Set landscape orientation for individual schedules
                section.PageSetup.Orientation = Orientation.Landscape;

                // Set margins for landscape pages
                SetStandardMargins(section);

                // Add logo to section
                AddLogoToSection(section, "individual");

                // Header with attendee name - Oswald
                var header = section.AddParagraph();
                header.Format.Font.Name = FontNames.Oswald;
                header.Format.Font.Color = COLOR_BLACK;
                header.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.ParticipantName;
                header.Format.Alignment = ParagraphAlignment.Center;
                header.AddFormattedText($"{attendee.FullName}'s Schedule", TextFormat.Bold);
                header.Format.SpaceAfter = PdfLayoutConstants.Spacing.HeaderSpacing;

                // Create schedule table
                var table = section.AddTable();
                table.Borders.Width = PdfLayoutConstants.Table.BorderWidth;

                // Column widths: 11" page - 1" margins = 10" usable width
                // Make table narrower (9.2") and center it with left indent
                var timeColumnWidth = PdfLayoutConstants.ColumnWidths.IndividualSchedule.Time;
                var dayColumnWidth = PdfLayoutConstants.ColumnWidths.IndividualSchedule.Day;
                var tableWidth = timeColumnWidth + (_schema.TotalDays * dayColumnWidth); // 1.8 + 7.4 = 9.2

                table.Rows.LeftIndent = Unit.FromInch(PdfLayoutConstants.Table.IndividualScheduleLeftIndent);

                table.AddColumn(Unit.FromInch(PdfLayoutConstants.ColumnWidths.IndividualSchedule.Time));

                // Columns 1-4: Days
                for (int i = 0; i < _schema.TotalDays; i++)
                {
                    table.AddColumn(Unit.FromInch(PdfLayoutConstants.ColumnWidths.IndividualSchedule.Day));
                }

                // Header row with day numbers
                var headerRow = table.AddRow();
                headerRow.Shading.Color = Color.FromRgb(220, 220, 220);
                headerRow.HeadingFormat = true;

                var periodHeaderCell = headerRow.Cells[0];
                var periodHeaderPara = periodHeaderCell.AddParagraph();
                periodHeaderPara.Format.Font.Name = FontNames.NotoSans;
                periodHeaderPara.Format.Font.Bold = true;
                periodHeaderPara.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.PeriodHeader;
                periodHeaderPara.Format.Alignment = ParagraphAlignment.Center;
                periodHeaderPara.AddText("Time");

                for (int day = 1; day <= _schema.TotalDays; day++)
                {
                    var dayCell = headerRow.Cells[day];
                    var dayPara = dayCell.AddParagraph();
                    dayPara.Format.Font.Name = FontNames.NotoSans;
                    dayPara.Format.Font.Bold = true;
                    dayPara.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.DayIndicator;
                    dayPara.Format.Alignment = ParagraphAlignment.Center;
                    dayPara.AddText($"Day {day}");
                }

                // Add rows dynamically based on timeslots
                foreach (var timeslot in timeslots)
                {
                    if (timeslot.IsPeriod)
                    {
                        // Find the matching period from schema
                        var periodConfig = _schema.PeriodSheets.FirstOrDefault(p =>
                            p.DisplayName.Equals(timeslot.Label, StringComparison.OrdinalIgnoreCase));

                        if (periodConfig == null)
                        {
                            // If we can't find a matching period, skip this timeslot
                            continue;
                        }

                        var row = table.AddRow();
                        row.VerticalAlignment = VerticalAlignment.Top;

                        // Time cell (first column) - only show time range, no label
                        var timeCell = row.Cells[0];
                        if (!string.IsNullOrWhiteSpace(timeslot.TimeRange))
                        {
                            var timePara = timeCell.AddParagraph();
                            timePara.Format.Font.Name = FontNames.Roboto;
                            timePara.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.TimeSlot;
                            timePara.Format.Alignment = ParagraphAlignment.Center;
                            timePara.AddText(timeslot.TimeRange);
                        }

                        // Build a map of day -> workshop for this period
                        var dayWorkshopMap = new Dictionary<int, (Workshop? workshop, bool isLeading)>();
                        for (int day = 1; day <= _schema.TotalDays; day++)
                        {
                            // Find workshop they're enrolled in for this period and day
                            var workshopForDay = attendeeSelections
                                .Where(s => s.Duration.StartDay <= day && s.Duration.EndDay >= day)
                                .Select(s => workshops.FirstOrDefault(w =>
                                    w.Name == s.WorkshopName &&
                                    w.Duration.StartDay == s.Duration.StartDay &&
                                    w.Duration.EndDay == s.Duration.EndDay &&
                                    w.Period.SheetName == periodConfig.SheetName))
                                .FirstOrDefault(w => w != null);

                            // Also check if they're leading a workshop in this period/day
                            var leadingWorkshop = workshops.FirstOrDefault(w =>
                                w.Leader.Contains(attendee.FullName) &&
                                w.Period.SheetName == periodConfig.SheetName &&
                                w.Duration.StartDay <= day &&
                                w.Duration.EndDay >= day);

                            // Prefer the workshop they're leading if both exist
                            var workshopToShow = leadingWorkshop ?? workshopForDay;
                            bool isLeading = leadingWorkshop != null;

                            dayWorkshopMap[day] = (workshopToShow, isLeading);
                        }

                        // Process days and merge cells for consecutive same workshops
                        int currentDay = 1;
                        while (currentDay <= _schema.TotalDays)
                        {
                            var (workshop, isLeading) = dayWorkshopMap[currentDay];

                            if (workshop != null)
                            {
                                // Find how many consecutive days have the same workshop
                                int spanDays = 1;
                                for (int nextDay = currentDay + 1; nextDay <= _schema.TotalDays; nextDay++)
                                {
                                    var (nextWorkshop, nextIsLeading) = dayWorkshopMap[nextDay];
                                    if (nextWorkshop != null &&
                                        nextWorkshop.Name == workshop.Name &&
                                        nextWorkshop.Leader == workshop.Leader &&
                                        nextIsLeading == isLeading)
                                    {
                                        spanDays++;
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                // Get the cell and merge if spanning multiple days
                                var dayCell = row.Cells[currentDay];
                                if (mergeWorkshopCells && spanDays > 1)
                                {
                                    dayCell.MergeRight = spanDays - 1;
                                }

                                // Add workshop content with adaptive font sizing
                                var workshopPara = dayCell.AddParagraph();
                                workshopPara.Format.Alignment = ParagraphAlignment.Center;

                                // Calculate adaptive font size based on workshop name length
                                var workshopDisplayText = isLeading ? $"Leading {workshop.Name}" : workshop.Name;
                                int workshopFontSize;
                                if (workshopDisplayText.Length >= PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.VeryLong)
                                {
                                    workshopFontSize = PdfLayoutConstants.AdaptiveFontSizing.WorkshopFontSizes.VeryLong;
                                }
                                else if (workshopDisplayText.Length >= PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Long)
                                {
                                    workshopFontSize = PdfLayoutConstants.AdaptiveFontSizing.WorkshopFontSizes.Long;
                                }
                                else if (workshopDisplayText.Length >= PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Medium)
                                {
                                    workshopFontSize = PdfLayoutConstants.AdaptiveFontSizing.WorkshopFontSizes.Medium;
                                }
                                else
                                {
                                    workshopFontSize = PdfLayoutConstants.AdaptiveFontSizing.WorkshopFontSizes.Default;
                                }

                                workshopPara.Format.Font.Size = workshopFontSize;

                                if (isLeading)
                                {
                                    workshopPara.Format.Font.Name = FontNames.NotoSans;
                                    workshopPara.Format.Font.Bold = true;
                                    workshopPara.AddText(workshopDisplayText);
                                }
                                else
                                {
                                    workshopPara.Format.Font.Name = FontNames.Roboto;
                                    workshopPara.AddText(workshopDisplayText);
                                }

                                // Add leader info
                                if (isLeading)
                                {
                                    // Check if co-leading (leader field contains "and")
                                    if (workshop.Leader.Contains(" and "))
                                    {
                                        // Extract the other leader's name
                                        var leaders = workshop.Leader.Split(LeaderDelimiter, StringSplitOptions.None);
                                        var otherLeader = leaders.FirstOrDefault(l => l.Trim() != attendee.FullName)?.Trim();

                                        if (!string.IsNullOrEmpty(otherLeader))
                                        {
                                            var leaderPara = dayCell.AddParagraph();
                                            leaderPara.Format.Font.Name = FontNames.Roboto;
                                            leaderPara.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.LeaderName;
                                            leaderPara.Format.Font.Italic = true;
                                            leaderPara.Format.Alignment = ParagraphAlignment.Center;
                                            leaderPara.AddText($"with {otherLeader}");
                                        }
                                    }
                                    // If solo leader, don't show anything
                                }
                                else
                                {
                                    // Not leading, show full leader info
                                    var leaderPara = dayCell.AddParagraph();
                                    leaderPara.Format.Font.Name = FontNames.Roboto;
                                    leaderPara.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.LeaderName;
                                    leaderPara.Format.Font.Italic = true;
                                    leaderPara.Format.Alignment = ParagraphAlignment.Center;
                                    leaderPara.AddText($"{workshop.Leader}");
                                }

                                // Add location info with tags
                                if (!string.IsNullOrWhiteSpace(workshop.Location))
                                {
                                    var locationPara = dayCell.AddParagraph();
                                    locationPara.Format.Font.Name = FontNames.Roboto;
                                    locationPara.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.LocationName;
                                    locationPara.Format.Font.Bold = true;
                                    locationPara.Format.Alignment = ParagraphAlignment.Center;

                                    // Add location name in bold
                                    locationPara.AddText(workshop.Location);

                                    // Add tags in lowercase and not bold
                                    if (workshop.Tags != null && workshop.Tags.Count > 0)
                                    {
                                        var tagNames = BuildTagListString(workshop.Tags);
                                        var tagText = locationPara.AddFormattedText($" ({tagNames})");
                                        tagText.Font.Bold = false;
                                    }
                                }

                                // If merging, skip the merged days; otherwise just move to next day
                                currentDay += mergeWorkshopCells ? spanDays : 1;
                            }
                            else
                            {
                                currentDay++;
                            }
                        }
                    }
                    else
                    {
                        // Non-period activity (Breakfast, Lunch, etc.)
                        AddMergedActivityRow(table, timeslot.Label, _schema.TotalDays, timeslot.TimeRange);
                    }
                }

                // Add facility map to the footer
                AddFacilityMapToSection(section);

                // Add event name footer
                AddEventNameFooter(section, eventName);

                sections.Add(section);
            }

            return sections;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "Converting to lowercase for display, not sorting or comparison")]
        private string BuildTagListString(List<LocationTag> tags)
        {
            var tagString = string.Join(", ", tags
                                    .OrderBy(t => t.Name)
                                        .Select(t => t.Name.ToLowerInvariant()));

            return tagString;
        }

        /// <summary>
        /// Generates blank schedule templates for attendees who did not preregister for classes.
        /// Each schedule includes a name field and empty master schedule grid to be filled in manually.
        /// </summary>
        /// <param name="workshops">List of workshops with locations (used to generate location columns in blank schedule).</param>
        /// <param name="eventName">Name of the event displayed in section footers.</param>
        /// <param name="count">Number of blank schedule copies to generate.</param>
        /// <param name="timeslots">Timeslots defining schedule structure. If null, uses default timeslots from schema.</param>
        /// <returns>List of MigraDoc Section objects containing blank schedules.</returns>
        public List<Section> GenerateBlankSchedules(List<Workshop> workshops, string eventName, int count, List<Models.TimeSlot>? timeslots = null)
        {
            LogInformationGenerateBlankSchedulesCalled(count);
            var sections = new List<Section>();

            // Generate multiple copies of the master schedule for blank schedules
            for (int i = 0; i < count; i++)
            {
                // Get master schedule sections (typically just one section)
                // Pass workshops so master schedule can extract locations for the grid
                var masterSections = _masterScheduleGenerator.GenerateMasterSchedule(
                    workshops: workshops,
                    eventName: "Workshop Schedule",
                    timeslots: timeslots);

                foreach (var section in masterSections)
                {
                    // Insert a name field at the very top of the section
                    var nameField = new Paragraph();
                    nameField.Format.Font.Name = FontNames.NotoSans;
                    nameField.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.NameFieldLabel;
                    nameField.Format.Alignment = ParagraphAlignment.Left;
                    nameField.Format.SpaceAfter = Unit.FromPoint(8);
                    nameField.AddText("Name: _____________________________");

                    // Insert at the beginning of the section
                    section.Elements.InsertObject(0, nameField);

                    // Add facility map to the footer
                    AddFacilityMapToSection(section);

                    // Add event name footer
                    AddEventNameFooter(section, eventName);

                    sections.Add(section);
                }
            }

            LogInformationGeneratedBlankScheduleSections(sections.Count);
            return sections;
        }

        #region Logging

        [LoggerMessage(
            EventId = 5001,
            Level = LogLevel.Information,
            Message = "GenerateBlankSchedules called with count={count}"
        )]
        private partial void LogInformationGenerateBlankSchedulesCalled(int count);

        [LoggerMessage(
            EventId = 5002,
            Level = LogLevel.Information,
            Message = "Generated {sectionCount} blank schedule sections"
        )]
        private partial void LogInformationGeneratedBlankScheduleSections(int sectionCount);

        #endregion

        /// <summary>
        /// Adds a merged row to schedule table for non-period activities that span all days.
        /// Used for activities like Breakfast, Lunch, or Evening Program that don't vary by day.
        /// </summary>
        /// <param name="table">MigraDoc table to add the row to.</param>
        /// <param name="activityName">Name of the activity (e.g., "Breakfast", "Lunch").</param>
        /// <param name="totalDays">Number of days to merge across (typically 4 for WinterAdventure).</param>
        /// <param name="timeRange">Optional time range to display (e.g., "8:00 AM - 9:00 AM"). Empty if time is not fixed.</param>
        private void AddMergedActivityRow(Table table, string activityName, int totalDays, string timeRange = "")
        {
            var row = table.AddRow();
            row.Shading.Color = Color.FromRgb(230, 230, 230);

            // First cell: time range (not merged)
            var timeCell = row.Cells[0];
            if (!string.IsNullOrWhiteSpace(timeRange))
            {
                var timePara = timeCell.AddParagraph();
                timePara.Format.Font.Name = FontNames.Roboto;
                timePara.Format.Font.Size = PdfLayoutConstants.FontSizes.IndividualSchedule.TimeSlot;
                timePara.Format.Alignment = ParagraphAlignment.Center;
                timePara.AddText(timeRange);
            }

            // Second cell: activity name merged across all day columns
            var activityCell = row.Cells[1];
            activityCell.MergeRight = totalDays - 1; // Merge across day columns only

            var para = activityCell.AddParagraph();
            para.Format.Font.Name = FontNames.Roboto;
            para.Format.Font.Size = PdfLayoutConstants.FontSizes.BlankSchedule.Title;
            para.Format.Font.Italic = true;
            para.Format.Alignment = ParagraphAlignment.Center;
            para.AddFormattedText(activityName, TextFormat.Bold);
        }

        /// <summary>
        /// Creates default timeslots for schedule structure when custom timeslots are not provided.
        /// Includes Breakfast, all period sheets from schema, Lunch, and Evening Program.
        /// </summary>
        /// <returns>List of TimeSlot objects representing the event's daily schedule structure.</returns>
        public List<Models.TimeSlot> CreateDefaultTimeslots()
        {
            var timeslots = new List<Models.TimeSlot>();

            // Add periods from schema
            foreach (var period in _schema.PeriodSheets)
            {
                timeslots.Add(new Models.TimeSlot
                {
                    Label = period.DisplayName,
                    IsPeriod = true,
                });
            }

            // Add default non-period activities
            timeslots.Insert(0, new Models.TimeSlot
            {
                Label = "Breakfast",
                IsPeriod = false,
            });

            timeslots.Add(new Models.TimeSlot
            {
                Label = "Lunch",
                IsPeriod = false,
            });

            timeslots.Add(new Models.TimeSlot
            {
                Label = "Evening Program",
                IsPeriod = false,
            });

            return timeslots;
        }
    }
}
