# WinterAdventurer

[![CI](https://github.com/isaac-ecrs/WinterAdventurer/workflows/CI/badge.svg)](https://github.com/isaac-ecrs/WinterAdventurer/actions)

A workshop/class registration management system for [ECRS](https://ecrs.org) Winter Adventure events. Import Excel spreadsheets from Cognito Forms and generate professional PDFs for workshop leaders and participants.

**Note:** This tool is specifically designed for ECRS Winter Adventure events and expects Excel data in the format exported from our Cognito Forms registration system. While the code is open source and contributions are welcome, it may require modification for other use cases.

## Features

- 📊 **Excel Import** - Schema-driven parsing of ECRS Cognito Forms registration data
- 📄 **PDF Generation** - Three document types:
  - Class rosters for workshop leaders
  - Individual schedules for participants
  - Master schedule grid showing all workshops
- ✏️ **Interactive Editing** - Edit workshop details (names, leaders), manage locations and timeslots
- 💾 **Data Persistence** - Locations and custom timeslots saved between sessions
- 🎨 **Professional Output** - Custom fonts, ECRS logo, optimized for B&W printing
- 🌙 **Dark Mode** - Toggle between light and dark themes

## Quick Start

### Download Pre-built Binary (Easiest)

1. Download the latest release for your OS from [Releases](https://github.com/isaac-ecrs/WinterAdventurer/releases)
2. Extract the zip file
3. Run the executable:
   - **Windows**: Double-click `WinterAdventurer.exe`
   - **Linux/macOS**: Run `./WinterAdventurer` in terminal
4. Open your browser to http://localhost:5001

### Run from Source

```bash
# Clone the repository
git clone https://github.com/isaac-ecrs/WinterAdventurer.git
cd WinterAdventurer

# Run the web application
cd WinterAdventurer
dotnet run

# Navigate to https://localhost:5001
```

### Run via CLI (Advanced)

```bash
# Process Excel file directly
cd WinterAdventurer.CLI
dotnet run -- "/path/to/your/file.xlsx"

# PDFs generated in same directory as Excel file
```

## Usage

1. **Upload Excel File** - Click "Choose File" and select your ECRS Cognito Forms registration export
2. **Review Workshops** - See all parsed workshops and participants
3. **Edit Details** - Update workshop names, leaders, and locations as needed
4. **Customize Schedule** - Adjust timeslots and periods for your event
5. **Generate PDFs** - Download class rosters, individual schedules, or master schedule

## Excel Format Requirements

**Important:** This tool expects Excel data in the specific format exported from ECRS Cognito Forms registration system for Winter Adventure events.

The system uses a schema-driven parser. See `WinterAdventurer.Library/EventSchemas/WinterAdventureSchema.json` for the expected Excel structure.

Key requirements:
- **ClassSelection sheet**: Attendee roster with registration IDs, names
- **Period sheets**: Workshop selections by period (e.g., MorningFirstPeriod, AfternoonFirstPeriod)
- Column pattern matching supported for flexibility across years

## Technology Stack

- **.NET 8** - Cross-platform runtime
- **Blazor Server** - Interactive web UI
- **MudBlazor** - Material Design component library
- **EPPlus** - Excel parsing
- **PDFsharp + MigraDoc** - PDF generation
- **Entity Framework Core** - SQLite database for persistence
- **Serilog** - Structured logging

## Development

### Prerequisites

- [.NET 8 SDK](https://dotnet.net/download/dotnet/8.0)

### Build

```bash
dotnet build
```

### Test

```bash
dotnet test
```

### Project Structure

```
WinterAdventurer/
├── WinterAdventurer/              # Blazor Server web UI
├── WinterAdventurer.Library/      # Core business logic
├── WinterAdventurer.CLI/          # Command-line interface
└── WinterAdventurer.Test/         # Unit tests
```

## Configuration

### Custom Fonts

Place TTF files in `WinterAdventurer.Library/Resources/Fonts/` and update `CustomFontResolver.cs`.

### Organization Logo

Replace `WinterAdventurer.Library/Resources/Images/ECRS_Logo_Minimal_Gray.png` with your logo (grayscale recommended for B&W printing).

### Event Schema

Create a new JSON schema in `WinterAdventurer.Library/EventSchemas/` to match your Excel format.

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

Contributions welcome! Please open an issue or submit a pull request.

**For Contributors:** This project was built with [Claude Code](https://claude.ai/code). See [CLAUDE.md](CLAUDE.md) for detailed architecture notes and development guidance that work well with AI-assisted development.

## Support

- **Issues**: [GitHub Issues](https://github.com/isaac-ecrs/WinterAdventurer/issues)
- **Documentation**: See [CLAUDE.md](CLAUDE.md) for detailed architecture notes

---

Built for [ECRS](https://ecrs.org) Winter Adventure events using .NET 8, Blazor, and Claude Code.
