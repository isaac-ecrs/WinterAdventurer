using Microsoft.JSInterop;

namespace WinterAdventurer.Services;

public class ThemeService
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isDarkMode = true; // Default to dark mode

    public event Action? OnThemeChanged;

    public ThemeService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public bool IsDarkMode => _isDarkMode;

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
