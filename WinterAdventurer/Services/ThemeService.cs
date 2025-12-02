using Microsoft.JSInterop;

namespace WinterAdventurer.Services;

/// <summary>
/// Manages UI theme state and persistence for the application.
/// Handles switching between light and dark modes with browser localStorage persistence
/// and JavaScript integration for tour theme synchronization.
/// </summary>
public class ThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isDarkMode = true; // Default to dark mode

    /// <summary>
    /// Event raised when the theme changes, allowing components to react to theme updates.
    /// </summary>
    public event Action? OnThemeChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThemeService"/> class.
    /// </summary>
    /// <param name="jsRuntime">The JavaScript interop runtime for browser interactions.</param>
    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Gets a value indicating whether dark mode is currently enabled.
    /// </summary>
    /// <value>True if dark mode is active; false if light mode is active.</value>
    public bool IsDarkMode => _isDarkMode;

    /// <summary>
    /// Initializes the theme service by loading the saved theme preference from browser localStorage.
    /// If no preference is saved or localStorage is unavailable (e.g., during prerendering),
    /// defaults to dark mode. Also synchronizes the tour theme via JavaScript interop.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    public async Task InitializeAsync()
    {
        try
        {
            var savedTheme = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "theme");
            _isDarkMode = savedTheme != "light"; // Default to dark if no preference saved

            // Update tour theme to match app theme
            await _jsRuntime.InvokeVoidAsync("updateTourTheme", _isDarkMode);
        }
        catch
        {
            // If localStorage is not available (e.g., during prerendering), use default
            _isDarkMode = true;
        }
    }

    /// <summary>
    /// Toggles the current theme between light and dark mode.
    /// Persists the new theme preference to localStorage, updates the tour theme via JavaScript interop,
    /// and raises the <see cref="OnThemeChanged"/> event to notify subscribers.
    /// </summary>
    /// <returns>A task representing the asynchronous toggle operation.</returns>
    public async Task ToggleThemeAsync()
    {
        _isDarkMode = !_isDarkMode;
        await SaveThemeAsync();

        // Update tour theme to match new app theme
        try
        {
            await _jsRuntime.InvokeVoidAsync("updateTourTheme", _isDarkMode);
        }
        catch
        {
            // Ignore if JavaScript is not available
        }

        OnThemeChanged?.Invoke();
    }

    /// <summary>
    /// Saves the current theme preference to browser localStorage.
    /// Uses JavaScript interop to persist the theme as "dark" or "light".
    /// Silently fails if localStorage is unavailable.
    /// </summary>
    /// <returns>A task representing the asynchronous save operation.</returns>
    private async Task SaveThemeAsync()
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "theme", _isDarkMode ? "dark" : "light");
        }
        catch
        {
            // Ignore if localStorage is not available
        }
    }
}
