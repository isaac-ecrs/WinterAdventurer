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

The tests expect the WinterAdventurer web app to be running. The default URL is `http://localhost:5000` (CI/production), but for local development you'll need to override this.

In a separate terminal:
```bash
cd WinterAdventurer
dotnet run
```

By default, the development server runs on port **5004** (see `launchSettings.json`). Set the `E2E_BASE_URL` environment variable to match:

```bash
export E2E_BASE_URL=http://localhost:5004  # Linux/Mac
# or
set E2E_BASE_URL=http://localhost:5004     # Windows CMD
# or
$env:E2E_BASE_URL="http://localhost:5004"  # Windows PowerShell
```

## Running Tests

### Run all E2E tests

**Local development** (with app running on port 5004):
```bash
E2E_BASE_URL=http://localhost:5004 dotnet test
```

**CI/production** (app running on port 5000):
```bash
dotnet test  # Uses default http://localhost:5000
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

### Regression Tests (CardIndex Bug)

#### `CardIndex_ShouldBeSequential_NotAllTheSame`
**Critical regression test** that prevents the bug where all workshops had `CardIndex=20` instead of sequential values (0, 1, 2, ...).

This bug caused the tour highlighting to fail because only the first workshop (CardIndex=0) gets the `#first-workshop-location` and `#first-workshop-leader` IDs. Without sequential CardIndex values, no workshop would get CardIndex=0, so the tour couldn't find the elements to highlight.

Requires workshops to be loaded (upload an Excel file before running).

#### `FirstWorkshop_ShouldHaveCorrectIDs`
Verifies that the first workshop (CardIndex=0) has the expected IDs and structure for tour highlighting.

Requires workshops to be loaded.

### Tour Tests

#### `Tour_AssignLocationsStep_ShouldHighlightLocationField`
Tests that the "Assign Locations" step of the tutorial correctly highlights the location field when workshops are loaded.

Requires workshops to be loaded and tour to be activated.

#### `DebugTourElements_LogsAllRelevantElements`
Debug test that logs all elements related to the tour (helpful for troubleshooting).
Run this test first to see what IDs are actually present in the DOM.

#### `Tour_CanAccessDebugFunction`
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

### Tests fail with "Failed to navigate" or "Connection refused"
Make sure:
1. The WinterAdventurer web app is running
2. The `E2E_BASE_URL` environment variable matches your app's port

```bash
# Start the app
cd WinterAdventurer
dotnet run  # Note the port in the output (usually 5004 for dev)

# In another terminal, set the URL and run tests
export E2E_BASE_URL=http://localhost:5004
dotnet test --filter "FullyQualifiedName~E2ETests"
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
