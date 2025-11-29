# WinterAdventurer E2E Tests

End-to-end tests for the WinterAdventurer web application using Playwright.

## Setup

### 1. Install Playwright Browsers

Before running tests for the first time, install Playwright browsers:

```bash
cd WinterAdventurer.E2ETests
dotnet build
playwright install
```

If `playwright` command is not found, use PowerShell (Windows) or install Playwright CLI:

**Windows (PowerShell):**
```powershell
pwsh bin/Debug/net9.0/playwright.ps1 install
```

**Linux/Mac:**
```bash
pwsh bin/Debug/net9.0/playwright.ps1 install
```

Or install globally:
```bash
npm install -g playwright
playwright install
```

### 2. Start the Application

The tests expect the WinterAdventurer web app to be running on `http://localhost:5000`.

In a separate terminal:
```bash
cd WinterAdventurer
dotnet run
```

Adjust the `BaseUrl` in `TourTests.cs` if your app runs on a different port.

## Running Tests

### Run all E2E tests
```bash
cd WinterAdventurer.E2ETests
dotnet test
```

### Run with verbose output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run specific test
```bash
dotnet test --filter "FullyQualifiedName~DebugTourElements"
```

## Available Tests

### `Tour_AssignLocationsStep_ShouldHighlightLocationField`
Tests that the "Assign Locations" step of the tutorial correctly highlights the location field when workshops are loaded.

### `DebugTourElements_LogsAllRelevantElements`
Debug test that logs all elements related to the tour (helpful for troubleshooting).
Run this test first to see what IDs are actually present in the DOM.

### `Tour_CanAccessDebugFunction`
Verifies that the JavaScript debug helper functions are available.

## Debugging

### Browser Console Debugging

You can manually call the debug function in your browser console while the app is running:

```javascript
debugTourElements()
```

This will log all elements with IDs containing "first", "workshop", or "location", plus all MudAutocomplete and MudTextField components.

### Tour Event Logging

The tour now logs detailed information about each step:
- When highlighting starts
- Which element is being targeted
- Whether the element was found
- What elements exist in the DOM if the target wasn't found

Check your browser's developer console for these logs.

## Troubleshooting

### Tests fail with "Could not find 'playwright' command"
Install Playwright browsers using the setup instructions above.

### Tests fail with "Failed to navigate to http://localhost:5000"
Make sure the WinterAdventurer web app is running:
```bash
cd WinterAdventurer
dotnet run
```

### Element not found errors
Run the `DebugTourElements_LogsAllRelevantElements` test to see what elements actually exist in the DOM. This helps identify if:
- The element ID isn't being rendered
- MudBlazor isn't applying the UserAttributes correctly
- The workshops haven't loaded yet

## Headless vs Headed Mode

By default, Playwright runs in headless mode. To see the browser during testing:

Add this to your test class:
```csharp
[TestInitialize]
public async Task Setup()
{
    await using var browser = await Playwright.Chromium.LaunchAsync(new() { Headless = false });
    // ... rest of setup
}
```

Or set environment variable:
```bash
HEADED=1 dotnet test
```
