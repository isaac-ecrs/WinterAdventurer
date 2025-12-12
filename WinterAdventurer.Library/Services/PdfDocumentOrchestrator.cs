// <copyright file="PdfDocumentOrchestrator.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using Microsoft.Extensions.Logging;
using MigraDoc.DocumentObjectModel;
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
        /// Initializes a new instance of the <see cref="PdfDocumentOrchestrator"/> class.
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
        /// Individual schedules include personalized facility maps showing only attendee's workshop locations.
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
            List<TimeSlot>? timeslots = null,
            int blankScheduleCount = 0)
        {
            // Allow creating PDF with just blank schedules if workshops is empty
            if ((workshops == null || workshops.Count == 0) && blankScheduleCount == 0)
            {
                LogWarningCannotCreatePdf();
                return null;
            }

            var document = new Document();

            // Create map compositor for personalized facility maps
            // NOTE: Do NOT dispose mapCompositor here! MigraDoc adds image file paths to the document
            // but doesn't load the actual image data until rendering time (which happens after this method returns).
            // If we dispose here, the temp files get deleted before rendering. Instead, let it be garbage collected.
            var locationMapResolverLogger = new LoggerAdapter<LocationMapResolver>(_logger);
            var locationMapResolver = new LocationMapResolver(locationMapResolverLogger);
            var mapCompositorLogger = new LoggerAdapter<MapCompositor>(_logger);
            var mapCompositor = new MapCompositor(mapCompositorLogger, locationMapResolver);

            // Add workshop rosters (only if workshops exist)
            if (workshops != null && workshops.Count > 0)
            {
                var rosterSections = _rosterGenerator.GenerateRosterSections(workshops, eventName, timeslots);
                foreach (var section in rosterSections)
                {
                    document.Sections.Add(section);
                }

                // Add individual schedules with personalized facility maps
                // Create a new schedule generator with map compositor for personalized maps
                var scheduleGeneratorLogger = new LoggerAdapter<IndividualScheduleGenerator>(_logger);
                var scheduleGeneratorWithMaps = new IndividualScheduleGenerator(
                    _scheduleGenerator._schema,
                    _scheduleGenerator._masterScheduleGenerator,
                    scheduleGeneratorLogger,
                    mapCompositor);

                var scheduleSections = scheduleGeneratorWithMaps.GenerateIndividualSchedules(
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
            List<TimeSlot>? timeslots = null)
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
            Message = "Cannot create PDF - workshops collection is empty and no blank schedules requested")]
        private partial void LogWarningCannotCreatePdf();

        [LoggerMessage(
            EventId = 3002,
            Level = LogLevel.Information,
            Message = "Generating {blankScheduleCount} blank schedules")]
        private partial void LogInformationGeneratingBlankSchedules(int blankScheduleCount);

        [LoggerMessage(
            EventId = 3003,
            Level = LogLevel.Information,
            Message = "Generated {blankScheduleSectionCount} blank schedule sections")]
        private partial void LogInformationGeneratedBlankSchedules(int blankScheduleSectionCount);

        [LoggerMessage(
            EventId = 3004,
            Level = LogLevel.Warning,
            Message = "Cannot create master schedule PDF - workshops collection is empty")]
        private partial void LogWarningCannotCreateMasterSchedulePdf();

        #endregion
    }
}
