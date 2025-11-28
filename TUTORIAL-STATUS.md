# Tutorial System - Current Status & Next Steps

## Completed Features ✅

### Auto-Advance on File Upload
- Tutorial automatically advances when file is uploaded
- No manual "Next" button required on upload step
- `window.tourDriver` global reference enables Blazor-to-JavaScript communication
- File upload triggers `notifyTourFileUploaded()` → `driver.moveNext()`

### Theme Support
- Tour popovers now respect light/dark mode
- `data-tour-theme` attribute on document root
- Explicit color definitions for both themes:
  - Light mode: White background, dark text, purple accents
  - Dark mode: Dark background (#1e1e1e), light text, lighter purple accents
- Theme syncs automatically via `updateTourTheme()` JavaScript function called from ThemeService

### Welcome Step Customization
- Custom button text: "Skip" and "Start tour" (instead of Previous/Next)
- Skip button closes tour via `disableButtons: []` + `onPrevClick` handler
- More welcoming first impression for new users

### Tour Steps
1. Welcome screen (centered) - with Skip/Start tour buttons
2. Upload Excel File - auto-advances on upload, no Next button
3. Configure Time Slots
4. Set Event Name
5. Blank Schedules
6. Review & Edit Workshops
7. Assign Locations
8. Generate PDFs
9. Completion message

## Known Issues ⚠️

### Button Text Rendering (Firefox)
- **Problem**: Tour button text appears blurry/fuzzy in Firefox
- **Symptoms**: Fuzzy edges, font weight looks off
- **Affects**: Both light and dark modes
- **Status**: Attempted fix with font-smoothing properties made it worse
- **Current State**: Reverted to original CSS (font-weight: 500, no smoothing)
- **Next Steps**:
  - May need to investigate driver.js default CSS
  - Could try different font families
  - Might be browser-specific rendering that's unavoidable

## Pending Features 📋

### 1. Add Floating Action Buttons to Tour
**Priority**: High

Add tour steps to highlight the floating action buttons in top-right corner:
- **Theme toggle button** (sun/moon icon)
  - Step should explain light/dark mode switching
  - Positioned in FloatingActionButtons component
  - Element ID needed: Add `id="theme-toggle-button"` to the MudIconButton

- **Tutorial button** (help icon)
  - Critical for users who skip the tour initially
  - Should explain: "Click here anytime to restart this tutorial"
  - Element ID needed: Add `id="tutorial-button"` to the MudIconButton
  - Consider making this part of the welcome step or final step

**Implementation Notes**:
```razor
<!-- In FloatingActionButtons.razor -->
<MudIconButton id="theme-toggle-button" ... />
<MudIconButton id="tutorial-button" ... />
```

```javascript
// In site.js, add steps after welcome or before completion:
{
    element: '#theme-toggle-button',
    popover: {
        title: 'Theme Toggle',
        description: 'Switch between light and dark mode anytime using this button.',
        side: 'left'
    }
},
{
    element: '#tutorial-button',
    popover: {
        title: 'Restart Tutorial',
        description: 'If you ever need help, click here to restart this guided tour.',
        side: 'left'
    }
}
```

**Alternative Approach**:
Instead of separate steps, update the welcome or completion step to reference these buttons:
```javascript
description: '... If you skip this tour and want to see it again later, click the help icon (?) in the top-right corner.'
```

### 2. Tour Persistence Improvements
- Currently stores completion in localStorage as `tour_home_completed`
- Consider: Session-based tours (reset on page reload?)
- Consider: User preference to never show tour again

### 3. Tour Content Updates
- Update descriptions to match current UI
- Add more detail about what happens when you upload a file
- Explain the difference between class rosters and master schedule PDFs

## Files Modified

### JavaScript
- `/WinterAdventurer/wwwroot/site.js`
  - Added `updateTourTheme()` function
  - Modified `notifyTourFileUploaded()` to auto-advance
  - Added global `window.tourDriver` reference
  - Customized welcome step buttons
  - Removed Next button from upload step

### CSS
- `/WinterAdventurer/wwwroot/css/tour.css`
  - Replaced CSS variables with explicit light/dark theme colors
  - Used `[data-tour-theme="light|dark"]` selectors
  - Defined colors for popovers, titles, descriptions, buttons, progress text
  - Separate button styles for each theme

### C# Services
- `/WinterAdventurer/Services/ThemeService.cs`
  - Calls `updateTourTheme()` on initialization
  - Calls `updateTourTheme()` on theme toggle
  - Syncs JavaScript tour theme with app theme

### Blazor Components
- `/WinterAdventurer/Components/Shared/FloatingActionButtons.razor`
  - **TODO**: Add IDs to buttons for tour targeting
  - Theme toggle button
  - Tutorial restart button

## Testing Checklist

- [x] Auto-advance works on file upload
- [x] Skip button closes tour
- [x] Start tour button advances to step 2
- [x] Theme toggle updates tour colors in real-time
- [x] Tour persists completion state in localStorage
- [ ] All floating action buttons are highlighted in tour
- [ ] Tutorial button reference in welcome/completion step
- [ ] Button text renders clearly in all browsers

## Future Enhancements

- **Conditional steps**: Hide certain steps based on user actions
- **Interactive elements**: Let users try actions during the tour
- **Progress indicator**: Show "Step 3 of 9" more prominently
- **Keyboard shortcuts**: ESC to close, arrow keys to navigate
- **Mobile responsiveness**: Ensure tour works well on mobile devices
- **Tour analytics**: Track which steps users skip or spend time on
- **Multiple tours**: Different tours for different user roles or features
