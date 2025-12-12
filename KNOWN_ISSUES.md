# Known Issues & TODO Items

## 1. Timeslot Persistence Issue

### Problem
- Custom period times and Morning Period 1 times **persist** between sessions
- Morning Period 2 and Afternoon period times **must be re-entered** every time
- This creates inconsistent user experience and extra work

### Root Cause
- Likely related to how timeslot data is stored/persisted in the web application
- Some timeslots are saved to a database or persistent storage, others are not

### Solution Needed
- Investigate the timeslot persistence logic in the Blazor web UI
- Ensure all timeslot data is stored consistently
- Verify that all periods (Morning Period 2, Afternoon) save to the same storage mechanism as the ones that work

### Files to Check
- `WinterAdventurer/Pages/` - Schedule/Timeslot pages
- `WinterAdventurer/Services/` - Data persistence services
- Database schema for timeslot storage

---

## 2. Multiple Locations Mapping to Same Overlay Image

### Problem
- Craft Room and Rec Hall should use the **same overlay image** on facility maps
- Currently they're not configured in `LocationMapConfiguration.json`
- Need to support multiple location names pointing to a single overlay image

### Current Configuration
File: `WinterAdventurer.Library/EventSchemas/LocationMapConfiguration.json`

Current mappings support one-to-one relationships only:
```json
{
  "locationMappings": {
    "Chapel A": "watson_layout_chapel_a.png",
    "Dining Room": "watson_layout_dining_room.png",
    ...
  }
}
```

### Solution Options

#### Option A: Simple Configuration (Recommended)
Add entries for Craft Room and Rec Hall pointing to the same image file:
```json
{
  "locationMappings": {
    "Chapel A": "watson_layout_chapel_a.png",
    "Craft Room": "watson_layout_crafts.png",
    "Rec Hall": "watson_layout_crafts.png",
    ...
  }
}
```

#### Option B: Support Array Values
Modify `LocationMapConfiguration.json` to support array values:
```json
{
  "locationMappings": {
    "Chapel A": ["watson_layout_chapel_a.png"],
    "Craft Room": ["watson_layout_crafts.png"],
    "Rec Hall": ["watson_layout_crafts.png"]
  }
}
```
This requires code changes to `LocationMapResolver.cs` to handle multiple overlay images per location.

### Steps to Implement

1. **Identify the overlay image file**:
   - Which Watson map overlay should represent Craft Room/Rec Hall?
   - If no file exists, create one using ImageMagick:
     ```bash
     # Create overlay PNG with same dimensions as other overlays (565x206)
     # Must have transparent background and crosshatch pattern
     ```

2. **Add to LocationMapConfiguration.json**:
   - Add "Craft Room" and "Rec Hall" entries
   - Both should point to the correct overlay image file
   - File must be added to `WinterAdventurer.Library/Resources/Images/WatsonMaps/`

3. **Test**:
   - Generate PDF with attendee assigned to Craft Room
   - Verify overlay appears on individual schedule map
   - Repeat for Rec Hall
   - Run `dotnet test` to ensure no regressions

4. **Optional: Update LocationMapConfiguration Schema**:
   - If supporting multiple overlays per location, update `LocationMapResolver.cs`
   - Modify `LocationMapConfiguration.json` schema accordingly
   - Update related unit tests

### Files to Modify
- `WinterAdventurer.Library/EventSchemas/LocationMapConfiguration.json` - Add mappings
- `WinterAdventurer.Library/Resources/Images/WatsonMaps/` - Add overlay image (if needed)
- `WinterAdventurer.Library/Services/LocationMapResolver.cs` - If supporting array values
- `WinterAdventurer.Test/Services/LocationMapResolverTests.cs` - Update tests if needed

---

## Completed Features ✅

- ✅ Personalized facility maps with location-based overlay selection
- ✅ Multi-location compositing (multiple overlays per schedule)
- ✅ Image dimension matching (565x206 pixels)
- ✅ PDF layout spacing optimization (4pt before/after maps)
- ✅ Proper image quality (PNG compression level 9)
- ✅ Diagnostic logging for image dimensions and composition
- ✅ 495 unit tests passing
- ✅ All E2E tests passing (17/17)
