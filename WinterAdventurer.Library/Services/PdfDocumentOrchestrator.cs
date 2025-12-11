using System.Collections.Generic;
using MigraDoc.DocumentObjectModel;
using Microsoft.Extensions.Logging;
using WinterAdventurer.Library.Models;

namespace WinterAdventurer.Library.Services
{
    /// <summary>
    /// Coordinates PDF generation by orchestrating the roster, individual schedule, and master schedule generators.
    /// Creates complete MigraDoc documents by combining outputs from specialized generators.
    /// </summary>
    public partial class PdfDocumentOrchestrator
    {
        private readonly WorkshopRosterGenerator _rosterGenerator;
        private readonly IndividualScheduleGenerator _scheduleGenerator;
        private readonly MasterScheduleGenerator _masterScheduleGenerator;
        private readonly ILogger<PdfDocumentOrchestrator> _logger;

        /// <summary>
        /// Initializes a new instance of the PdfDocumentOrchestrator class.
        /// </summary>
        /// <param name="rosterGenerator">Generator for workshop rosters.</param>
        /// <param name="scheduleGenerator">Generator for individual participant schedules.</param>
        /// <param name="masterScheduleGenerator">Generator for master schedule grids.</param>
        /// <param name="logger">Logger for diagnostic output.</param>
        public PdfDocumentOrchestrator(
            WorkshopRosterGenerator rosterGenerator,
            IndividualScheduleGenerator scheduleGenerator,
            MasterScheduleGenerator masterScheduleGenerator,
            ILogger<PdfDocumentOrchestrator> logger)
        {
            _rosterGenerator = rosterGenerator ?? throw new ArgumentNullException(nameof(rosterGenerator));
            _scheduleGenerator = scheduleGenerator ?? throw new ArgumentNullException(nameof(scheduleGenerator));
            _masterScheduleGenerator = masterScheduleGenerator ?? throw new ArgumentNullException(nameof(masterScheduleGenerator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Creates a complete PDF document containing workshop rosters and individual schedules.
        /// Combines class rosters for leaders with personalized schedule pages for each participant.
        /// </summary>
        /// <param name="workshops">List of workshops to include in the document.</param>
        /// <param name="eventName">Name of the event displayed in PDF headers and footers.</param>
        /// <param name="mergeWorkshopCells">Whether to merge table cells for multi-day workshops in individual schedules.</param>
        /// <param name="timeslots">Custom timeslots for schedule structure. If null, uses default timeslots.</param>
        /// <param name="blankScheduleCount">Number of blank schedule pages to append.</param>
        /// <returns>MigraDoc Document ready for rendering, or null if workshops collection is empty.</returns>
        public Document? CreateWorkshopAndSchedulePdf(
            List<Workshop> workshops,
            string eventName = "Winter Adventure",
            bool mergeWorkshopCells = true,
            List<Models.TimeSlot>? timeslots = null,
            int blankScheduleCount = 0)
        {
            // Allow creating PDF with just blank schedules if workshops is empty
            if ((workshops == null || workshops.Count == 0) && blankScheduleCount == 0)
            {
                LogWarningCannotCreatePdf();
                return null;
            }

            var document = new Document();

            // Add workshop rosters (only if workshops exist)
            if (workshops != null && workshops.Count > 0)
            {
                var rosterSections = _rosterGenerator.GenerateRosterSections(workshops, eventName, timeslots);
                foreach (var section in rosterSections)
                {
                    document.Sections.Add(section);
                }

                // Add individual schedules
                var scheduleSections = _scheduleGenerator.GenerateIndividualSchedules(
                    workshops,
                    eventName,
                    mergeWorkshopCells,
                    timeslots);
                foreach (var section in scheduleSections)
                {
                    document.Sections.Add(section);
                }
            }

            // Add blank schedules if requested
            if (blankScheduleCount > 0)
            {
                LogInformationGeneratingBlankSchedules(blankScheduleCount);
                // Pass workshops so blank schedules can include location columns
                var blankSections = _scheduleGenerator.GenerateBlankSchedules(workshops ?? new List<Workshop>(), eventName, blankScheduleCount, timeslots);
                LogInformationGeneratedBlankSchedules(blankSections.Count);

                foreach (var section in blankSections)
                {
                    document.Sections.Add(section);
                }
            }

            return document;
        }

        /// <summary>
        /// Creates a master schedule PDF showing all workshops organized by location, time, and days.
        /// Useful for event staff to see the complete workshop schedule at a glance.
        /// </summary>
        /// <param name="workshops">List of workshops to display in the master schedule.</param>
        /// <param name="eventName">Name of the event displayed in PDF title.</param>
        /// <param name="timeslots">Custom timeslots for schedule structure. If null, uses default timeslots.</param>
        /// <returns>MigraDoc Document ready for rendering, or null if workshops collection is empty.</returns>
        public Document? CreateMasterSchedulePdf(
            List<Workshop> workshops,
            string eventName = "Master Schedule",
            List<Models.TimeSlot>? timeslots = null)
        {
            if (workshops == null || workshops.Count == 0)
            {
                LogWarningCannotCreateMasterSchedulePdf();
                return null;
            }

            var document = new Document();

            var masterSections = _masterScheduleGenerator.GenerateMasterSchedule(workshops, eventName, timeslots);
            foreach (var section in masterSections)
            {
                document.Sections.Add(section);
            }

            return document;
        }

        #region Logging

        [LoggerMessage(
            EventId = 3001,
            Level = LogLevel.Warning,
            Message = "Cannot create PDF - workshops collection is empty and no blank schedules requested"
        )]
        private partial void LogWarningCannotCreatePdf();

        [LoggerMessage(
            EventId = 3002,
            Level = LogLevel.Information,
            Message = "Generating {blankScheduleCount} blank schedules"
        )]
        private partial void LogInformationGeneratingBlankSchedules(int blankScheduleCount);

        [LoggerMessage(
            EventId = 3003,
            Level = LogLevel.Information,
            Message = "Generated {blankScheduleSectionCount} blank schedule sections"
        )]
        private partial void LogInformationGeneratedBlankSchedules(int blankScheduleSectionCount);

        [LoggerMessage(
            EventId = 3004,
            Level = LogLevel.Warning,
            Message = "Cannot create master schedule PDF - workshops collection is empty"
        )]
        private partial void LogWarningCannotCreateMasterSchedulePdf();

        #endregion
    }
}
