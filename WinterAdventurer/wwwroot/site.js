function downloadFile(filename, base64Content) {
    const linkSource = `data:application/pdf;base64,${base64Content}`;
    const downloadLink = document.createElement("a");
    downloadLink.href = linkSource;
    downloadLink.download = filename;
    downloadLink.click();
}

// Update tour theme based on current app theme
window.updateTourTheme = function(isDarkMode) {
    document.documentElement.setAttribute('data-tour-theme', isDarkMode ? 'dark' : 'light');
    console.log('Tour theme updated to:', isDarkMode ? 'dark' : 'light');
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
                    description: 'This quick tour will guide you through the process of managing workshop registrations and creating schedules. Let\'s get started!',
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
                element: '#workshop-grid',
                popover: {
                    title: 'Step 5: Review & Edit Workshops',
                    description: 'After uploading, you\'ll see workshop cards here. Each card shows the workshop name, leader, period, and participants. You can click on any workshop name or leader to edit them before generating PDFs.',
                    side: 'top'
                }
            },
            {
                element: '#workshop-grid',
                popover: {
                    title: 'Step 6: Assign Locations',
                    description: 'Click on the location field in any workshop card to assign a location. The location autocomplete will filter to show relevant locations for that period, and you can add new locations on the fly.',
                    side: 'top'
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
