// <copyright file="MasterScheduleGenerator.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using Microsoft.Extensions.Logging;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using WinterAdventurer.Library.EventSchemas;
using WinterAdventurer.Library.Models;

namespace WinterAdventurer.Library.Services
{
    /// <summary>
    /// Generates master schedule PDFs showing all workshops organized by location, time, and days.
    /// Automatically selects landscape or portrait orientation based on number of locations.
    /// </summary>
    public class MasterScheduleGenerator : PdfFormatterBase
    {
        private readonly EventSchema _schema;

        /// <summary>
        /// Initializes a new instance of the <see cref="MasterScheduleGenerator"/> class.
        /// </summary>
        /// <param name="schema">Event schema configuration defining period structure.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public MasterScheduleGenerator(EventSchema schema, ILogger<MasterScheduleGenerator> logger)
            : base(logger)
        {
            _schema = schema ?? throw new ArgumentNullException(nameof(schema));
        }

        /// <summary>
        /// Generates master schedule grid showing all workshops organized by location, time, and days.
        /// Automatically selects landscape or portrait orientation based on number of locations.
        /// </summary>
        /// <param name="workshops">List of workshops to display in the master schedule.</param>
        /// <param name="eventName">Name of the event displayed in PDF title.</param>
        /// <param name="timeslots">Timeslots defining schedule structure. If null, uses default timeslots from schema.</param>
        /// <returns>List containing a single MigraDoc Section with the master schedule table.</returns>
        public List<Section> GenerateMasterSchedule(List<Workshop> workshops, string eventName, List<TimeSlot>? timeslots = null)
        {
            var sections = new List<Section>();

            // If no timeslots provided, create default set
            if (timeslots == null || timeslots.Count == 0)
            {
                timeslots = CreateDefaultTimeslots();
            }

            // Get unique locations sorted alphabetically
            var locations = GetUniqueLocations(workshops);

            if (locations.Count == 0)
            {
                // No locations assigned, skip master schedule
                return sections;
            }

            var section = new Section();

            // Auto-detect orientation based on location count
            if (locations.Count > 5)
            {
                section.PageSetup.Orientation = Orientation.Landscape;

                // Smaller margins to maximize space
                section.PageSetup.LeftMargin = Unit.FromInch(0.4);
                section.PageSetup.RightMargin = Unit.FromInch(0.4);
                section.PageSetup.TopMargin = Unit.FromInch(0.4);
                section.PageSetup.BottomMargin = Unit.FromInch(0.4);
            }
            else
            {
                section.PageSetup.Orientation = Orientation.Portrait;
                SetStandardMargins(section);
            }

            // Add logo to section
            AddLogoToSection(section, "master");

            // Title
            var title = section.AddParagraph();
            title.Format.Font.Name = FontNames.Oswald;
            title.Format.Font.Color = COLOR_BLACK;
            title.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.Title;
            title.Format.Alignment = ParagraphAlignment.Center;
            title.AddFormattedText(eventName, TextFormat.Bold);

            // Add vertical spacing to center content on page
            title.Format.SpaceBefore = section.PageSetup.Orientation == Orientation.Landscape
                ? Unit.FromPoint(24)
                : Unit.FromPoint(12);
            title.Format.SpaceAfter = Unit.FromPoint(12);

            // Create table
            var table = section.AddTable();
            table.Borders.Width = PdfLayoutConstants.Table.BorderWidth;

            // Column widths - make table narrower than full width to allow centering
            var timeColumnWidth = PdfLayoutConstants.ColumnWidths.MasterSchedule.Time;
            var daysColumnWidth = PdfLayoutConstants.ColumnWidths.MasterSchedule.Days;
            var pageUsableWidth = section.PageSetup.Orientation == Orientation.Landscape
                ? 10.2 // 11" - 0.4" - 0.4"
                : PdfLayoutConstants.PageDimensions.PortraitUsableWidth;

            // Make table 9.5" wide to leave room for centering
            var tableWidth = section.PageSetup.Orientation == Orientation.Landscape ? 9.5 : pageUsableWidth;
            var locationColumnWidth = (tableWidth - timeColumnWidth - daysColumnWidth) / locations.Count;

            // Add left indent to center the table horizontally (slightly more to the right)
            if (section.PageSetup.Orientation == Orientation.Landscape)
            {
                var leftIndent = ((pageUsableWidth - tableWidth) / 2) + 0.15;  // Center + extra shift right
                table.Rows.LeftIndent = Unit.FromInch(leftIndent);
            }

            // Add columns
            table.AddColumn(Unit.FromInch(timeColumnWidth));  // Time
            table.AddColumn(Unit.FromInch(daysColumnWidth));  // Days indicator
            foreach (var location in locations)
            {
                table.AddColumn(Unit.FromInch(locationColumnWidth));
            }

            // Set minimal cell padding for the entire table
            if (table.Columns != null)
            {
                foreach (Column? column in table.Columns)
                {
                    if (column != null)
                    {
                        column.LeftPadding = Unit.FromPoint(2);
                        column.RightPadding = Unit.FromPoint(2);
                    }
                }
            }

            // Header row
            var headerRow = table.AddRow();
            headerRow.Shading.Color = Color.FromRgb(220, 220, 220);
            headerRow.HeadingFormat = true;
            headerRow.TopPadding = Unit.FromPoint(3);
            headerRow.BottomPadding = Unit.FromPoint(3);

            // Time header
            var timeHeader = headerRow.Cells[0];
            var timeHeaderPara = timeHeader.AddParagraph();
            timeHeaderPara.Format.Font.Name = FontNames.NotoSans;
            timeHeaderPara.Format.Font.Bold = true;
            timeHeaderPara.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.ColumnHeader;
            timeHeaderPara.Format.Alignment = ParagraphAlignment.Center;
            timeHeaderPara.AddText("Time");

            // Days header
            var daysHeader = headerRow.Cells[1];
            var daysHeaderPara = daysHeader.AddParagraph();
            daysHeaderPara.Format.Font.Name = FontNames.NotoSans;
            daysHeaderPara.Format.Font.Bold = true;
            daysHeaderPara.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.ColumnHeader;
            daysHeaderPara.Format.Alignment = ParagraphAlignment.Center;
            daysHeaderPara.AddText("Days");

            // Location headers
            for (int i = 0; i < locations.Count; i++)
            {
                var locationHeader = headerRow.Cells[i + 2];
                var locationHeaderPara = locationHeader.AddParagraph();
                locationHeaderPara.Format.Font.Name = FontNames.NotoSans;
                locationHeaderPara.Format.Font.Bold = true;
                locationHeaderPara.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.LocationHeader;
                locationHeaderPara.Format.Alignment = ParagraphAlignment.Center;
                locationHeaderPara.AddText(locations[i]);
            }

            // Add rows for each timeslot
            foreach (var timeslot in timeslots)
            {
                if (timeslot.IsPeriod)
                {
                    // Period: create two sub-rows for Days 1-2 and Days 3-4
                    AddPeriodRows(table, timeslot, locations, workshops);
                }
                else
                {
                    // Activity: create single merged row
                    AddActivityRow(table, timeslot, locations.Count);
                }
            }

            sections.Add(section);
            return sections;
        }

        /// <summary>
        /// Adds period workshop rows to master schedule table, split by day ranges (Days 1-2 and Days 3-4).
        /// Each row shows workshops assigned to each location for that time period and day range.
        /// </summary>
        /// <param name="table">MigraDoc table to add rows to.</param>
        /// <param name="timeslot">Period timeslot containing workshop assignments.</param>
        /// <param name="locations">Ordered list of locations defining table columns.</param>
        /// <param name="workshops">List of workshops to search for location/period matches.</param>
        private void AddPeriodRows(Table table, TimeSlot timeslot, List<string> locations, List<Workshop> workshops)
        {
            // Create two rows: Days 1-2 and Days 3-4
            var row12 = table.AddRow();
            row12.TopPadding = Unit.FromPoint(2);
            row12.BottomPadding = Unit.FromPoint(2);

            var row34 = table.AddRow();
            row34.TopPadding = Unit.FromPoint(2);
            row34.BottomPadding = Unit.FromPoint(2);

            // Time cell (merge across both rows)
            var timeCell = row12.Cells[0];
            timeCell.MergeDown = 1;
            timeCell.VerticalAlignment = VerticalAlignment.Center;
            var timePara = timeCell.AddParagraph();
            timePara.Format.Font.Name = FontNames.NotoSans;
            timePara.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.TimeCell;
            timePara.Format.Alignment = ParagraphAlignment.Center;

            if (!string.IsNullOrEmpty(timeslot.TimeRange))
            {
                timePara.AddText(timeslot.TimeRange);
            }
            else
            {
                timePara.AddText(timeslot.Label);
            }

            // Days 1-2 cell
            var days12Cell = row12.Cells[1];
            days12Cell.VerticalAlignment = VerticalAlignment.Center;
            var days12Para = days12Cell.AddParagraph();
            days12Para.Format.Font.Name = FontNames.NotoSans;
            days12Para.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.DayIndicator;
            days12Para.Format.Alignment = ParagraphAlignment.Center;
            days12Para.AddText("Days\n1-2");

            // Days 3-4 cell
            var days34Cell = row34.Cells[1];
            days34Cell.VerticalAlignment = VerticalAlignment.Center;
            var days34Para = days34Cell.AddParagraph();
            days34Para.Format.Font.Name = FontNames.NotoSans;
            days34Para.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.DayIndicator;
            days34Para.Format.Alignment = ParagraphAlignment.Center;
            days34Para.AddText("Days\n3-4");

            // Location columns
            for (int i = 0; i < locations.Count; i++)
            {
                var location = locations[i];

                // Check for 4-day workshop first
                var workshop4Day = workshops.FirstOrDefault(w =>
                    w.Period.DisplayName == timeslot.Label &&
                    w.Location == location &&
                    w.Duration.StartDay == 1 &&
                    w.Duration.EndDay == 4);

                if (workshop4Day != null)
                {
                    // 4-day workshop spans both rows
                    var cell = row12.Cells[i + 2];
                    cell.MergeDown = 1;
                    cell.VerticalAlignment = VerticalAlignment.Center;
                    AddWorkshopToCell(cell, workshop4Day);
                }
                else
                {
                    // Check for Days 1-2 workshop
                    var workshop12 = workshops.FirstOrDefault(w =>
                        w.Period.DisplayName == timeslot.Label &&
                        w.Location == location &&
                        w.Duration.StartDay == 1 &&
                        w.Duration.EndDay == 2);

                    if (workshop12 != null)
                    {
                        AddWorkshopToCell(row12.Cells[i + 2], workshop12);
                    }

                    // Check for Days 3-4 workshop
                    var workshop34 = workshops.FirstOrDefault(w =>
                        w.Period.DisplayName == timeslot.Label &&
                        w.Location == location &&
                        w.Duration.StartDay == 3 &&
                        w.Duration.EndDay == 4);

                    if (workshop34 != null)
                    {
                        AddWorkshopToCell(row34.Cells[i + 2], workshop34);
                    }
                }
            }
        }

        /// <summary>
        /// Adds a single merged row to master schedule for non-period activities.
        /// Used for activities like Breakfast or Lunch that span all locations uniformly.
        /// </summary>
        /// <param name="table">MigraDoc table to add the row to.</param>
        /// <param name="timeslot">Activity timeslot containing label and time information.</param>
        /// <param name="locationCount">Number of location columns to merge across.</param>
        private void AddActivityRow(Table table, TimeSlot timeslot, int locationCount)
        {
            var row = table.AddRow();
            row.Shading.Color = Color.FromRgb(230, 230, 230);
            row.TopPadding = Unit.FromPoint(2);
            row.BottomPadding = Unit.FromPoint(2);

            // Time cell
            var timeCell = row.Cells[0];
            var timePara = timeCell.AddParagraph();
            timePara.Format.Font.Name = FontNames.NotoSans;
            timePara.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.TimeCell;
            timePara.Format.Alignment = ParagraphAlignment.Center;

            if (!string.IsNullOrEmpty(timeslot.TimeRange))
            {
                timePara.AddText(timeslot.TimeRange);
            }
            else
            {
                timePara.AddText(timeslot.Label);
            }

            // Merge days and all location columns
            var activityCell = row.Cells[1];
            activityCell.MergeRight = locationCount; // Merge Days + all location columns
            activityCell.VerticalAlignment = VerticalAlignment.Center;
            var activityPara = activityCell.AddParagraph();
            activityPara.Format.Font.Name = FontNames.Roboto;
            activityPara.Format.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.ActivityName;
            activityPara.Format.Font.Italic = true;
            activityPara.Format.Alignment = ParagraphAlignment.Center;
            activityPara.AddFormattedText(timeslot.Label, TextFormat.Bold);
        }

        /// <summary>
        /// Populates a master schedule table cell with workshop name and leader information.
        /// Formats the cell with appropriate font sizing and styling for schedule readability.
        /// </summary>
        /// <param name="cell">MigraDoc table cell to populate.</param>
        /// <param name="workshop">Workshop object containing name and leader information to display.</param>
        private void AddWorkshopToCell(Cell cell, Workshop workshop)
        {
            cell.VerticalAlignment = VerticalAlignment.Center;
            var para = cell.AddParagraph();
            para.Format.Font.Name = FontNames.NotoSans;
            para.Format.Alignment = ParagraphAlignment.Center;

            // Calculate adaptive font size based on workshop name length
            int workshopFontSize;
            if (workshop.Name.Length >= PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.VeryLong)
            {
                workshopFontSize = 7;  // Very long names (45+ chars)
            }
            else if (workshop.Name.Length >= PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Long)
            {
                workshopFontSize = 8;  // Long names (35-44 chars)
            }
            else if (workshop.Name.Length >= PdfLayoutConstants.AdaptiveFontSizing.TextLengthThresholds.Medium)
            {
                workshopFontSize = 8;  // Medium names (28-34 chars)
            }
            else
            {
                workshopFontSize = PdfLayoutConstants.FontSizes.MasterSchedule.WorkshopInfo;  // Default: 9pt
            }

            para.Format.Font.Size = workshopFontSize;

            // Workshop name
            para.AddFormattedText(workshop.Name, TextFormat.Bold);
            para.AddLineBreak();

            // Leader in parentheses
            var leaderText = para.AddFormattedText($"({workshop.Leader})");
            leaderText.Font.Size = PdfLayoutConstants.FontSizes.MasterSchedule.LeaderName;
            leaderText.Font.Italic = true;
        }

        /// <summary>
        /// Extracts unique workshop locations from workshops collection for master schedule organization.
        /// Filters out empty locations and returns sorted list for consistent column ordering.
        /// </summary>
        /// <param name="workshops">List of workshops to extract locations from.</param>
        /// <returns>Sorted list of unique, non-empty location strings.</returns>
        private List<string> GetUniqueLocations(List<Workshop> workshops)
        {
            return workshops
                .Where(w => !string.IsNullOrWhiteSpace(w.Location))
                .Select(w => w.Location)
                .Distinct()
                .OrderBy(loc => loc)
                .ToList();
        }

        /// <summary>
        /// Creates default timeslots for schedule structure when custom timeslots are not provided.
        /// Includes Breakfast, all period sheets from schema, Lunch, and Evening Program.
        /// </summary>
        /// <returns>List of TimeSlot objects representing the event's daily schedule structure.</returns>
        public List<TimeSlot> CreateDefaultTimeslots()
        {
            var timeslots = new List<TimeSlot>();

            // Add periods from schema
            foreach (var period in _schema.PeriodSheets)
            {
                timeslots.Add(new TimeSlot
                {
                    Label = period.DisplayName,
                    IsPeriod = true,
                });
            }

            // Add default non-period activities
            timeslots.Insert(0, new TimeSlot
            {
                Label = "Breakfast",
                IsPeriod = false,
            });

            timeslots.Add(new TimeSlot
            {
                Label = "Lunch",
                IsPeriod = false,
            });

            timeslots.Add(new TimeSlot
            {
                Label = "Evening Program",
                IsPeriod = false,
            });

            return timeslots;
        }
    }
}
