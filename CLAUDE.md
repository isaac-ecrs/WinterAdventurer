# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

WinterAdventurer is a workshop/class registration management system for multi-day events. It imports Excel spreadsheets containing workshop registration data and generates:
1. Class rosters (PDFs for workshop leaders)
2. Individual participant schedules (visual schedule cards)

The system uses a **schema-driven architecture** where event-specific Excel parsing rules are defined in JSON configuration files, making it adaptable to different event formats without code changes.

## Solution Structure

- **WinterAdventurer.Library**: Core business logic (Excel parsing, data models, PDF generation)
- **WinterAdventurer.CLI**: Command-line tool for batch processing
- **WinterAdventurer**: Blazor web UI for interactive editing
- **WinterAdventurer.Test**: MSTest unit tests

## Build & Run Commands

### CLI Application
```bash
cd WinterAdventurer.CLI
dotnet run -- "/path/to/excel/file.xlsx"
```

Output is generated in the same directory as the input Excel file.

### Blazor Web Application
```bash
cd WinterAdventurer
dotnet run
# Navigate to https://localhost:5001
```

### Run Tests
```bash
dotnet test
```

## Schema-Driven Configuration System

**Key Innovation**: Excel parsing rules are defined in JSON schemas, not hardcoded.

**Schema Location**: `WinterAdventurer.Library/EventSchemas/WinterAdventureSchema.json`

**Schema Elements**:
- `classSelectionSheet`: Defines attendee roster columns
- `periodSheets[]`: Array of workshop period configurations
  - Column mappings (selectionId, firstName, lastName, choiceNumber)
  - `workshopColumns[]`: Duration configurations (4-day, 2-day splits)
- `workshopFormat`: Pattern for parsing "Workshop Name (Leader Name)"

**Pattern Matching**: Column names support both exact matches and substring patterns.

Example:
```json
"registrationId": {
  "pattern": "WinterAdventureClassRegist_Id"
}
```
Matches "2024WinterAdventureClassRegist_Id", "2025WinterAdventureClassRegist_Id", etc.

**To add new event schema**:
1. Create JSON file in `EventSchemas/` folder
2. Add to `<EmbeddedResource>` in `WinterAdventurer.Library.csproj`
3. Update `ExcelUtilities.LoadEventSchema()` to load the appropriate schema

## Service Architecture

The system uses a **service-based architecture** with specialized classes for different responsibilities:

### Excel Parsing Services
- **ExcelParser** (`WinterAdventurer.Library/Services/ExcelParser.cs`): Handles all Excel file parsing
  - Loads event schema from embedded JSON
  - Parses attendees and workshops from Excel sheets
  - Throws domain-specific exceptions (e.g., `MissingSheetException`, `ExcelParsingException`)

### PDF Generation Services
- **PdfFormatterBase** (abstract): Shared PDF utilities (logos, footers, margins, resources)
- **WorkshopRosterGenerator**: Creates class roster PDFs for workshop leaders
- **IndividualScheduleGenerator**: Creates individual participant schedule cards
- **MasterScheduleGenerator**: Creates master schedule grid (location × time)
- **PdfDocumentOrchestrator**: Coordinates the three generators to produce complete documents

### Exception Hierarchy
- **ExcelParsingException**: Base exception for Excel parsing errors
  - `MissingSheetException`: Required sheet not found in workbook
  - `MissingColumnException`: Required column not found in sheet
  - `InvalidWorkshopFormatException`: Workshop cell format invalid
  - `SchemaValidationException`: Schema validation failed
- **PdfGenerationException**: Base exception for PDF generation errors
  - `MissingResourceException`: Embedded resource (font, image) not found

### Facade Pattern
**ExcelUtilities** (467 lines) acts as a facade for backward compatibility:
- Maintains public `Workshops` field
- Delegates `ImportExcel()` to `ExcelParser.ParseFromStream()`
- Delegates `CreatePdf()` to `PdfDocumentOrchestrator`
- Wraps new exceptions in `InvalidOperationException` for compatibility

## Excel Data Flow

**Entry Point**: `ExcelUtilities.ImportExcel(Stream stream)` → delegates to `ExcelParser.ParseFromStream()`

**Process** (in ExcelParser):
1. Load event schema from embedded JSON
2. Parse attendees from ClassSelection sheet → `Dictionary<SelectionId, Attendee>`
3. For each period sheet:
   - Process all rows
   - For each workshop column (4-day, 2-day-first, 2-day-second):
     - Extract workshop name/leader using `StringExtensions`
     - Match attendee by ClassSelectionId
     - Create `WorkshopSelection` with `ChoiceNumber` (1=first choice, 2+=backup)
     - Aggregate into `Workshop` objects
4. Result: Populated `Workshops` list

**Key Classes**:
- `SheetHelper`: Abstracts Excel column access with pattern matching support
- `StringExtensions`: Parses "Workshop Name (Leader Name)" format
- `WorkshopDuration`: Represents which days a workshop runs (StartDay, EndDay)

## Domain Model

**Workshop Uniqueness**: Determined by `Period|Name|Leader|Duration` key. The same workshop name can exist in multiple periods or with different durations.

**ChoiceNumber**: Distinguishes first choice (1) from backup choices (2+). Backup participants may join if their first choice is full.

**Relational Structure**:
```
Workshop
  ├─ Period (e.g., "MorningFirstPeriod")
  ├─ Duration (e.g., Days 1-4, Days 1-2, Days 3-4)
  ├─ Leader
  └─ Selections[]
      └─ Each links to an Attendee via ClassSelectionId
         with ChoiceNumber indicating preference
```

## PDF Generation

**Entry Point**: `ExcelUtilities.CreatePdf()` → delegates to `PdfDocumentOrchestrator.CreateWorkshopAndSchedulePdf()`

**Document Structure**:
1. Workshop rosters (one per workshop) via `WorkshopRosterGenerator`
2. Individual schedules (one per participant) via `IndividualScheduleGenerator`
3. Blank schedules (optional) via `IndividualScheduleGenerator.GenerateBlankSchedules()`

**Class Roster Format** (WorkshopRosterGenerator):
- One section per workshop
- Workshop name (16pt bold), leader info (12pt), period/duration (10pt)
- **Enrolled Participants** section (ChoiceNumber=1, sorted by last name)
- **Backup/Alternate Choices** section (ChoiceNumber>1, with choice numbers shown)

**Individual Schedule Format** (IndividualScheduleGenerator):
- Master schedule grid template showing all locations × time slots
- Workshop names filled in appropriate cells
- Participant name in header
- Optional cell merging for multi-day workshops

**Font Handling** (PdfFormatterBase):
```csharp
GlobalFontSettings.FontResolver = new CustomFontResolver();
document.Styles["Normal"].Font.Name = "NotoSans";
```

Supported fonts: NotoSans, Oswald, Roboto (embedded as TTF resources)

**Rendering** (CLI):
```csharp
var renderer = new PdfDocumentRenderer();
renderer.Document = document;
renderer.RenderDocument();
renderer.Save(outputPath);
```

## Embedded Resources

**Configuration** (in `WinterAdventurer.Library.csproj`):
```xml
<EmbeddedResource Include="Resources/Fonts/**/*.ttf" />
<EmbeddedResource Include="EventSchemas/**/*.json" />
```

**Resource Naming**: Path separators become dots.
- File: `Resources/Fonts/Noto_Sans/static/NotoSans-Regular.ttf`
- Resource name: `WinterAdventurer.Library.Resources.Fonts.Noto_Sans.static.NotoSans-Regular.ttf`

## EPPlus License Configuration

**Required at startup**:
```csharp
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
```

This is set in:
- `ExcelUtilities.ImportExcel()`
- `ExcelUtilities.DumpExcelSchema()`
- Test initialization

## Utilities

**DumpExcelSchema**: Exports Excel structure to JSON for debugging
```csharp
excelUtilities.DumpExcelSchema(stream, "output.json");
```

Useful for understanding new Excel file formats before creating schemas.

## Common Development Tasks

**To modify column mappings**:
1. Edit `EventSchemas/WinterAdventureSchema.json`
2. Update `periodSheets[].columns` or `classSelectionSheet.columns`
3. Use exact matches or patterns as needed
4. Changes are automatically used by `ExcelParser.ParseFromStream()`

**To change PDF formatting**:
- **Class rosters**: Modify `WorkshopRosterGenerator.GenerateRosterSections()`
- **Individual schedules**: Modify `IndividualScheduleGenerator.GenerateIndividualSchedules()`
- **Master schedule**: Modify `MasterScheduleGenerator.GenerateMasterSchedule()`
- Use MigraDoc API: sections, paragraphs, formatting properties
- Margins set via `PdfFormatterBase.SetStandardMargins()` (currently 0.5 inch all sides)

**To add new fonts**:
1. Add TTF files to `Resources/Fonts/{FontFamily}/`
2. Update `CustomFontResolver.ResolveTypeface()` switch statement
3. Update `CustomFontResolver.GetFontResourceName()` switch statement
4. Rebuild to embed resources

**To debug Excel parsing**:
1. Use CLI (fastest feedback loop)
2. Run `ExcelUtilities.DumpExcelSchema()` to understand structure
3. Check console output for parsing diagnostics from `ExcelParser`
4. Verify workshop key format: `{Period}|{Name}|{Leader}|{StartDay}-{EndDay}`
5. Check for domain-specific exceptions (e.g., `MissingSheetException`, `MissingColumnException`)

## Architecture Notes

**Separation of Concerns**:
- Library: Pure business logic, no UI dependencies
  - Service layer: Specialized classes for Excel parsing and PDF generation
  - Facade layer: `ExcelUtilities` maintains backward compatibility
  - Exception layer: Domain-specific exception types for better error handling
- CLI: Thin wrapper for command-line execution
- Web: Interactive UI with data editing capabilities
- Models use nullable reference types (C# 8.0+)

**Service Architecture Benefits**:
- Single Responsibility Principle: Each service has one focused purpose
- Reduced complexity: ExcelUtilities reduced from 1,732 to 467 lines (73% reduction)
- Better testability: Services can be tested independently
- Improved error handling: Domain-specific exceptions provide better diagnostics
- Backward compatibility: Existing code continues to work unchanged

**Excel Row Indexing**: Rows are 1-indexed (headers at row 1, data starts at row 2)

**Workshop Aggregation**: Selections are grouped by unique workshop key. Multiple people selecting the same workshop at the same period with same duration are aggregated into one Workshop with multiple Selections.

**Fallback ID Generation**: If ClassSelection_Id is missing, the system generates a fallback ID by concatenating FirstName+LastName (spaces removed).
