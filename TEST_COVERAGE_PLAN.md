# WinterAdventurer Test Coverage & Implementation Plan

## Executive Summary

The web UI is the primary interface but currently lacks comprehensive component testing. The Library layer is well-tested (71% coverage) with improved error recovery paths.

**Current State (After Implementation):**
- **411 tests passing** (1 skipped, up from 404)
- **Library coverage: 71.3%** (improved with error recovery tests)
- Web UI coverage: 37.9%
- Main gap: Home.razor (1100 LOC, 0% coverage) - the core orchestration page (requires bunit framework and component state access)

---

## Priority 1: Home.razor Integration Tests (CRITICAL)

**File:** `WinterAdventurer/Components/Pages/Home.razor` (723 LOC)
**Current Coverage:** 0%
**Risk Level:** HIGH (main user workflow orchestration)

### Key Workflows to Test

#### 1.1 File Upload Workflow
- **Test:** Upload valid Excel → workshops populate correctly
  - Verify ExcelUtils.ImportExcel called
  - Verify workshops list populated
  - Verify periods extracted and timeslots created
  - Verify success state
  - Verify no error messages

- **Test:** Upload Excel with location mappings → locations restored
  - Pre-populate location mappings in database
  - Upload Excel
  - Verify saved locations applied to workshops
  - Verify Workshop.Location property set

- **Test:** Upload Excel → triggers UI state changes
  - FileUploadSection visible → hidden after upload
  - WorkshopGrid visible after upload
  - TimeslotEditor visible after upload
  - FloatingActionButtons show reload button

- **Test:** Upload oversized file (>10MB) → error handling
  - Verify error message displayed
  - Verify file read stream respects 10MB limit
  - Verify workshops not populated

- **Test:** Upload invalid Excel → error message displayed
  - Verify ExcelParsingException caught
  - Verify error message shown
  - Verify workshops remain empty
  - Verify state remains valid for retry

#### 1.2 Timeslot Management
- **Test:** LoadTimeslots → database timeslots populate
  - Mock LocationService.GetAllTimeSlotsAsync
  - Verify timeslots list populated
  - Verify IsPeriod flag preserved
  - Verify sort order correct

- **Test:** SaveTimeslots → validation before save
  - Verify ValidateTimeslots called
  - Verify SortTimeslots called
  - Verify LocationService.SaveAllTimeSlotsAsync called
  - Verify correct TimeSlot format (DB entities)

- **Test:** ValidateTimeslots → detects unconfigured slots
  - Create periods without times
  - Verify hasUnconfiguredTimeslots = true
  - Verify PDF generation blocked (error alert shown)

- **Test:** ValidateTimeslots → detects overlaps
  - Create overlapping timeslots
  - Verify hasOverlappingTimeslots = true
  - Verify PDF generation blocked

- **Test:** PopulateTimeslotsFromPeriods → preserves user slots
  - Add custom user timeslot
  - Upload Excel with new periods
  - Verify period slots updated
  - Verify user slot preserved

#### 1.3 Location Management
- **Test:** LoadLocations → loads from database
  - Mock LocationService.GetAllLocationsWithTagsAsync
  - Verify availableLocations populated
  - Verify passed to WorkshopGrid

- **Test:** HandleWorkshopLocationChanged → updates cache
  - Mock workshop location change
  - Verify location added to availableLocations (if new)
  - Verify locationListVersion incremented
  - Verify StateHasChanged called

- **Test:** HandleDeleteLocationRequest → removes location
  - Create workshops with locations
  - Request location deletion
  - Verify LocationService.DeleteLocationAsync called
  - Verify location removed from availableLocations
  - Verify workshop.Location cleared
  - Verify locationListVersion incremented

- **Test:** HandleDeleteLocationRequest → multiple workshops
  - Create multiple workshops with same location
  - Delete location
  - Verify all affected workshops cleared

#### 1.4 PDF Generation
- **Test:** BuildPdf → generates and downloads PDF
  - Mock ExcelUtils.CreatePdf
  - Mock JSRuntime.InvokeVoidAsync for download
  - Verify showPdfSuccess = true
  - Verify isLoading state transitions
  - Verify success message displayed (3s timeout)

- **Test:** BuildPdf → blocked by validation
  - Set hasUnconfiguredTimeslots = true
  - Attempt BuildPdf
  - Verify PDF not generated (blocked before call)
  - OR verify error message if still called

- **Test:** BuildPdf → error handling
  - Mock ExcelUtils.CreatePdf to throw
  - Verify error message displayed
  - Verify success = false
  - Verify isLoading = false
  - Verify disposed check prevents crashes

- **Test:** BuildPdf → converts timeslots to Library format
  - Create UI TimeSlotViewModel list
  - Call ConvertToLibraryTimeSlots
  - Verify Library.Models.TimeSlot objects created
  - Verify all properties mapped

- **Test:** BuildPdf → applies location tags
  - Mock LocationService.GetAllLocationsWithTagsAsync with tags
  - Build PDF
  - Verify tags applied to workshops before CreatePdf call
  - Verify locationTagsDict built correctly

- **Test:** BuildMasterSchedulePdf → similar to BuildPdf
  - Verify ExcelUtils.CreateMasterSchedulePdf called
  - Verify downloaded correctly

#### 1.5 Initialization & Lifecycle
- **Test:** OnAfterRenderAsync → tour initialization
  - First render, tour not completed
  - Verify TourService.HasCompletedTourAsync called
  - Verify TourService.StartHomeTourAsync called
  - Verify 500ms delay before tour start

- **Test:** OnAfterRenderAsync → skip tour if completed
  - Mock TourService.HasCompletedTourAsync = true
  - First render
  - Verify TourService.StartHomeTourAsync NOT called

- **Test:** OnAfterRenderAsync → loads timeslots and locations
  - Verify LoadTimeslots called
  - Verify LoadLocations called
  - Verify StateHasChanged called

- **Test:** Dispose → cleanup
  - Create component, dispose
  - Verify _disposed = true
  - Verify CancellationTokenSource canceled and disposed
  - Verify subsequent calls return early (_disposed check)

#### 1.6 State Management
- **Test:** eventName → defaults and can be changed
  - Verify default: "Winter Adventure {Year}"
  - Mock eventName change in WorkshopGrid
  - Verify BuildPdf uses eventName (or default if empty)

- **Test:** blankScheduleCount → propagates to PDF
  - Set blankScheduleCount = 3
  - Call BuildPdf
  - Verify ExcelUtils.CreatePdf called with blankScheduleCount=3

- **Test:** Disposed state → prevents crashes
  - Set _disposed = true
  - Call UploadExcel, SaveTimeslots, etc.
  - Verify early returns prevent StateHasChanged calls
  - Verify no null reference exceptions

### Test Implementation Notes

- Use **BUnit** (like WorkshopCardTests.cs)
- Mock `ExcelUtilities`, `LocationService`, `TourService`, `ILogger`, `IJSRuntime`, `ITimeslotValidationService`
- Use `TestAuthorizationContext` for component state
- Mock `IBrowserFile` for file upload tests
- Test both success and error paths
- Verify logging calls for key operations
- Estimated: **25-35 tests**

---

## Priority 2: TimeslotEditor Component Tests

**File:** `WinterAdventurer/Components/Shared/TimeslotEditor.razor` (untested)
**Current Coverage:** 0%

### Tests to Add

- **Test:** TimeslotEditor_WithPeriodSlots_DisplaysAsReadOnly
  - Period slots (IsPeriod=true) should be disabled
  - Cannot edit label
  - Verify styling indicates read-only

- **Test:** TimeslotEditor_WithCustomSlots_DisplaysAsEditable
  - Custom slots (IsPeriod=false) editable
  - Can change times
  - Can delete

- **Test:** TimeslotEditor_AddCustomSlot_AddsToList
  - Click "Add Slot"
  - Verify new slot added with empty times
  - Verify animation triggered (Version incremented)

- **Test:** TimeslotEditor_DeleteSlot_RemovesAndAnimates
  - Delete custom slot
  - Verify removed from list
  - Verify animation triggered

- **Test:** TimeslotEditor_SaveTimes_CallsCallback
  - Set times
  - Click "Save Times"
  - Verify OnSaveTimeslots EventCallback invoked

- **Test:** TimeslotEditor_OnTimeslotsChanged_CallsCallback
  - Add/remove/edit slot
  - Verify OnTimeslotsChanged EventCallback invoked

- **Estimated:** **6-8 tests**

---

## Priority 3: ExcelParser Error Recovery Tests

**File:** `WinterAdventurer.Library/Services/ExcelParser.cs`
**Current Coverage:** 58.8%

### Tests to Add

- **Test:** ExcelParser_WithInvalidRowData_ContinuesToNextRow
  - Create Excel with one row missing required columns
  - Create second row with valid data
  - Verify error logged
  - Verify second row still parsed
  - Verify count = 1 (second row parsed)

- **Test:** ExcelParser_WithInvalidWorkshopFormat_ContinuesToNextColumn
  - Create row with malformed workshop cell
  - Create another column with valid workshop
  - Verify both processed (one skipped, one parsed)

- **Test:** ExcelParser_LoadAttendees_WithMissingEmail_CreatesAttendeeWithDefault
  - Missing email field
  - Verify attendee created with empty email
  - Verify rest of attendee data correct

- **Test:** ExcelParser_DumpExcelSchema_WritesValidJson
  - Call DumpExcelSchema
  - Verify JSON file written
  - Verify valid JSON structure
  - Verify headers present

- **Estimated:** **4-5 tests**

---

## Priority 4: IndividualScheduleGenerator Edge Cases

**File:** `WinterAdventurer.Library/Services/IndividualScheduleGenerator.cs`
**Current Coverage:** 80.1%

### Tests to Add

- **Test:** IndividualScheduleGenerator_WithMultiDayWorkshops_MergesCells
  - Create workshop spanning days 1-4
  - Verify cell merging in schedule grid
  - Verify no overlapping cell assignments

- **Test:** IndividualScheduleGenerator_WithParticipantNoWorkshops_CreatesEmptySchedule
  - Create attendee with no workshop selections
  - Generate blank schedule
  - Verify layout correct but no workshops filled

- **Test:** IndividualScheduleGenerator_WithGapInSchedule_HandlesCorrectly
  - Create workshops: Period1, Period3 (skip Period2)
  - Verify schedule grid handles gaps
  - Verify correct time slots rendered

- **Estimated:** **3-4 tests**

---

## Priority 5: Integration Tests (End-to-End)

**File:** New file `WinterAdventurer.Test/IntegrationTests.cs`

### Tests to Add

- **Test:** FullWorkflow_UploadExcel_ConfigureTimeslots_GeneratePDF
  - Create real Excel file
  - Upload via Home component
  - Configure timeslots
  - Generate PDF
  - Verify PDF bytes returned
  - Verify all three PDF types generated (rosters, schedules, master)

- **Estimated:** **2-3 tests**

---

## Test Implementation Summary

### Completed:
| Component | Coverage Gap | Tests Added | Status |
|-----------|--------------|-------------|--------|
| **ExcelParser Error Recovery** | 59% → 65%+ | 6 tests | ✅ **COMPLETED** |
| | Error recovery paths tested | | |
| | Row failure handling verified | | |
| | Column failure handling verified | | |
| | Invalid choice number handling | | |
| | Duplicate ID handling | | |
| | Multi-row error scenarios | | |

### Not Completed (Due to Technical Constraints):
| Priority | Component | Coverage Gap | Tests | Reason |
|----------|-----------|--------------|-------|--------|
| 1 | Home.razor | 0% → 90%+ | 30 | Private field access required; needs refactoring to expose testable behavior |
| 4 | IndividualScheduleGenerator | 80% → 90%+ | 3-4 | Constructor dependencies (EventSchema, MasterScheduleGenerator) not easily mocked |
| 2 | TimeslotEditor | 0% → 85%+ | 7 | Not yet implemented |

### Summary:
- **411 total tests passing** (7 new ExcelParser error recovery tests added)
- **ExcelParser coverage improved** with meaningful error recovery testing
- **7 new tests** covering graceful degradation scenarios

---

## Execution Order

1. **Home.razor tests first** (highest impact, main workflow)
2. **TimeslotEditor tests** (integration with Home)
3. **ExcelParser error recovery** (supporting logic)
4. **IndividualScheduleGenerator edge cases** (PDF quality)
5. **Integration tests** (full pipeline validation)

---

## Success Criteria

- [ ] All 48 new tests pass
- [ ] Library coverage increases from 71% to 82%+
- [ ] Web UI coverage increases from 38% to 70%+
- [ ] Home.razor coverage increases from 0% to 90%+
- [ ] Overall project coverage reaches 65%+
- [ ] No test flakiness or timeouts
- [ ] Tests document actual user workflows
- [ ] Meaningful coverage (not just for numbers)

