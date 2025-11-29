function downloadFile(filename, base64Content) {
    const linkSource = `data:application/pdf;base64,${base64Content}`;
    const downloadLink = document.createElement("a");
    downloadLink.href = linkSource;
    downloadLink.download = filename;
    downloadLink.click();
}

// Initialize tour theme on page load from localStorage
(function() {
    const savedTheme = localStorage.getItem('theme');
    const isDarkMode = savedTheme !== 'light'; // Default to dark if no preference
    document.documentElement.setAttribute('data-tour-theme', isDarkMode ? 'dark' : 'light');
    console.log('Tour theme initialized to:', isDarkMode ? 'dark' : 'light');
})();

// Update tour theme based on current app theme
window.updateTourTheme = function(isDarkMode) {
    document.documentElement.setAttribute('data-tour-theme', isDarkMode ? 'dark' : 'light');
    console.log('Tour theme updated to:', isDarkMode ? 'dark' : 'light');
};

// Set theme from welcome screen buttons
window.setTourTheme = function(theme) {
    const isDarkMode = theme === 'dark';

    // Update localStorage
    localStorage.setItem('theme', theme);
    console.log('Theme set to:', theme);

    // Update tour theme immediately
    updateTourTheme(isDarkMode);

    // Update button active states
    document.querySelectorAll('.tour-theme-button').forEach(btn => {
        btn.classList.toggle('active', btn.dataset.theme === theme);
    });

    // Trigger page reload to sync with Blazor (simple but effective)
    // The Blazor app will read the theme from localStorage on next load
    setTimeout(() => {
        location.reload();
    }, 300); // Small delay to show the button feedback
};

// Show error in tour popover
window.showTourError = function(errorMessage) {
    if (!window.tourDriver || !window.tourDriver.isActive()) {
        console.log('Tour not active, skipping tour error display');
        return; // Tour not active, use normal error display
    }

    console.log('Showing error in tour:', errorMessage);

    // Get current step
    const activeElement = document.querySelector('.driver-popover');
    if (!activeElement) {
        console.log('No active popover found');
        return;
    }

    // Find description element
    const descriptionEl = activeElement.querySelector('.driver-popover-description');
    if (!descriptionEl) {
        console.log('No description element found');
        return;
    }

    // Store original description if not already stored
    if (!descriptionEl.dataset.originalContent) {
        descriptionEl.dataset.originalContent = descriptionEl.innerHTML;
    }

    // Update with error message - keep it compact
    descriptionEl.innerHTML = `
        <div class="tour-error-message">
            <div class="tour-error-icon">⚠️</div>
            <div class="tour-error-content">
                <strong>Upload Failed</strong>
                <p><small>${errorMessage}</small></p>
            </div>
        </div>
        <p style="margin-top: 0.5rem; font-size: 0.875rem;">Try uploading a valid Excel file (.xlsx) or skip this step.</p>
    `;

    console.log('Error displayed in tour popover');

    // Refresh the driver overlay to reposition highlight after content change
    try {
        window.tourDriver.refresh();
    } catch (e) {
        console.log('Could not refresh tour overlay:', e);
    }
};

// Clear error from tour popover
window.clearTourError = function() {
    const descriptionEl = document.querySelector('.driver-popover-description');
    if (!descriptionEl || !descriptionEl.dataset.originalContent) {
        return;
    }

    console.log('Clearing tour error, restoring original content');

    // Restore original content
    descriptionEl.innerHTML = descriptionEl.dataset.originalContent;
    delete descriptionEl.dataset.originalContent;

    // Refresh the driver overlay to reposition highlight after content change
    if (window.tourDriver) {
        try {
            window.tourDriver.refresh();
        } catch (e) {
            console.log('Could not refresh tour overlay:', e);
        }
    }
};

// Tour state tracking
let tourFileUploaded = false;

// Called from Blazor when a file is successfully uploaded
window.notifyTourFileUploaded = function() {
    console.log('notifyTourFileUploaded: File uploaded, auto-advancing tour');
    tourFileUploaded = true;

    // Auto-advance the tour if it's active and on the file upload step
    if (window.tourDriver && window.tourDriver.isActive()) {
        console.log('Tour is active, advancing to next step');
        window.tourDriver.moveNext();
    }
};

// Debug helper - call from console to inspect tour elements
window.debugTourElements = function() {
    console.log('=== Tour Elements Debug ===');
    console.log('Looking for #first-workshop-location:', document.querySelector('#first-workshop-location'));
    console.log('Looking for #first-workshop-leader:', document.querySelector('#first-workshop-leader'));

    console.log('\n=== CardIndex Debug ===');
    console.log('All elements with data-card-index attribute:');
    document.querySelectorAll('[data-card-index]').forEach(el => {
        const index = el.getAttribute('data-card-index');
        const id = el.id || '(no id)';
        console.log(`  CardIndex ${index}: ID="${id}", Tag=${el.tagName}, HasAutocomplete=${el.querySelector('.mud-autocomplete') !== null}`);
    });

    console.log('\nAll elements with IDs containing "first":');
    document.querySelectorAll('[id*="first"]').forEach(el => {
        console.log(`  - ${el.id} (${el.tagName}):`, el);
    });
    console.log('\nAll elements with IDs containing "workshop":');
    document.querySelectorAll('[id*="workshop"]').forEach(el => {
        console.log(`  - ${el.id} (${el.tagName}):`, el);
    });
    console.log('\nAll elements with IDs containing "location":');
    document.querySelectorAll('[id*="location"]').forEach(el => {
        console.log(`  - ${el.id} (${el.tagName}):`, el);
    });
    console.log('\nAll MudAutocomplete elements:');
    document.querySelectorAll('.mud-autocomplete').forEach((el, i) => {
        console.log(`  Autocomplete ${i}:`, el, 'ID:', el.id, 'Parent ID:', el.parentElement?.id);
    });
    console.log('\nAll MudTextField elements:');
    document.querySelectorAll('.mud-input-control').forEach((el, i) => {
        console.log(`  TextField ${i}:`, el, 'ID:', el.id, 'Parent ID:', el.parentElement?.id);
    });
};

// Guided tour using Driver.js
window.startHomeTour = function() {
    console.log('startHomeTour called');
    console.log('window.driver:', window.driver);
    console.log('window.driver.js:', window.driver.js);
    console.log('typeof window.driver.js:', typeof window.driver.js);
    console.log('window.driver.js keys:', Object.keys(window.driver.js || {}));

    if (!window.driver || !window.driver.js) {
        console.error('Driver.js library not loaded!');
        return;
    }

    console.log('Creating driver instance...');

    // Reset tour state for fresh start
    tourFileUploaded = false;

    const allSteps = [
            {
                popover: {
                    title: 'Welcome to Winter Adventurer! 🎿',
                    description: `
                        <p>This quick tour will guide you through the process of managing workshop registrations and creating schedules.</p>
                        <div class="tour-theme-selector">
                            <p><strong>Choose your theme:</strong></p>
                            <div class="tour-theme-buttons">
                                <button onclick="setTourTheme('light')" class="tour-theme-button" data-theme="light">
                                    ☀️ Light
                                </button>
                                <button onclick="setTourTheme('dark')" class="tour-theme-button" data-theme="dark">
                                    🌙 Dark
                                </button>
                            </div>
                        </div>
                        <p>Let's get started!</p>
                    `,
                    side: 'center',
                    align: 'center',
                    showButtons: ['next', 'previous'],
                    nextBtnText: 'Start tour',
                    prevBtnText: 'Skip',
                    disableButtons: [],  // Don't disable any buttons
                    onPrevClick: () => {
                        if (window.tourDriver) {
                            window.tourDriver.destroy();
                        }
                    }
                }
            },
            {
                element: '#file-upload-section',
                popover: {
                    title: 'Step 1: Upload Excel File',
                    description: 'Start by uploading your workshop registration Excel file. The file should contain a ClassSelection sheet and period sheets with participant choices. The tour will automatically continue once your file is uploaded.',
                    side: 'bottom',
                    showButtons: ['previous', 'close']
                }
            },
            {
                element: '#timeslot-editor',
                popover: {
                    title: 'Step 2: Configure Time Slots',
                    description: 'Set up the time periods for your event. Each period needs a start and end time. You can also add custom activities like meals or free time.',
                    side: 'bottom'
                }
            },
            {
                element: '#event-name-field',
                popover: {
                    title: 'Step 3: Set Event Name',
                    description: 'Configure the event name that will appear as the header on your master schedule PDF. This defaults to "Winter Adventure [current year]" but you can customize it for your event.',
                    side: 'bottom'
                }
            },
            {
                element: '#blank-schedules-field',
                popover: {
                    title: 'Step 4: Blank Schedules',
                    description: 'Specify how many blank individual schedule cards to generate. These are useful for attendees who registered but aren\'t in your Excel roster yet.',
                    side: 'bottom'
                }
            },
            {
                element: '#first-workshop-location',
                popover: {
                    title: 'Step 5: Assign Locations',
                    description: 'After uploading your file, workshop cards will appear. Click the Location field on any workshop to assign a room or area. The autocomplete filters out locations already used by other workshops in the same time period, and you can type new locations to add them on the fly.',
                    side: 'bottom'
                }
            },
            {
                element: '#first-workshop-leader',
                popover: {
                    title: 'Step 6: Edit Leader Names',
                    description: 'You can click on any workshop leader\'s name to edit it. This is useful for correcting typos or updating facilitator information before generating PDFs.',
                    side: 'bottom'
                }
            },
            {
                element: '#pdf-generation-section',
                popover: {
                    title: 'Step 7: Generate PDFs',
                    description: 'Once everything is configured, generate your PDFs:<br><br>• <b>Class Rosters</b>: Participant lists for workshop leaders (includes blank schedules if specified)<br>• <b>Master Schedule</b>: Overview grid showing all workshops, times, and locations with your event name as the header',
                    side: 'left'
                }
            },
            {
                popover: {
                    title: 'You\'re All Set! 🎉',
                    description: 'You can restart this tour anytime from the "Show Tutorial" option in the navigation menu. Happy organizing!',
                    side: 'center',
                    align: 'center'
                }
            }
    ];

    let driver;
    window.tourDriver = null;  // Global reference for auto-advance
    try {
        driver = window.driver.js.driver({
            showProgress: true,
            showButtons: ['next', 'previous', 'close'],
            popoverClass: 'winter-adventurer-tour',
            onPopoverRender: (popover, { config, state }) => {
                // Initialize theme button active states on welcome screen
                if (state.activeIndex === 0) {
                    setTimeout(() => {
                        const currentTheme = localStorage.getItem('theme') || 'dark';
                        document.querySelectorAll('.tour-theme-button').forEach(btn => {
                            btn.classList.toggle('active', btn.dataset.theme === currentTheme);
                        });
                    }, 0);
                }
            },
            onHighlightStarted: (element, step, options) => {
                console.log('=== Tour Highlight Started ===');
                console.log('Step index:', options.state.activeIndex);
                console.log('Step element selector:', step.element);
                console.log('Element found:', element);
                console.log('Element ID:', element?.id);
                console.log('Element classes:', element?.className);

                // If element not found, log what's actually in the DOM
                if (!element && step.element) {
                    const searched = document.querySelector(step.element);
                    console.log('Manual search result:', searched);
                    console.log('All elements with "workshop" in ID:',
                        Array.from(document.querySelectorAll('[id*="workshop"]')).map(el => el.id));
                    console.log('All elements with "location" in ID:',
                        Array.from(document.querySelectorAll('[id*="location"]')).map(el => el.id));
                    console.log('All elements with "first" in ID:',
                        Array.from(document.querySelectorAll('[id*="first"]')).map(el => el.id));
                }
            },
            onHighlighted: (element, step, options) => {
                console.log('=== Tour Highlighted ===');
                console.log('Step index:', options.state.activeIndex);
                console.log('Successfully highlighted element:', element);
            },
            onDeselected: (element, step, options) => {
                console.log('=== Tour Deselected ===');
                console.log('Step index:', options.state.activeIndex);
            },
            onDestroyed: () => {
                console.log('Tour destroyed, marking as completed and resetting state');
                localStorage.setItem('tour_home_completed', 'true');
                tourFileUploaded = false; // Reset for next tour run
                window.tourDriver = null;  // Clean up global reference
            },
            steps: allSteps
        });
        window.tourDriver = driver;  // Store globally for auto-advance
        console.log('Driver instance created:', driver);
    } catch (error) {
        console.error('Error creating driver instance:', error);
        return;
    }

    // Start the tour
    console.log('Calling driver.drive()...');
    try {
        driver.drive();
        console.log('driver.drive() completed successfully');
    } catch (error) {
        console.error('Error calling driver.drive():', error);
    }
};
