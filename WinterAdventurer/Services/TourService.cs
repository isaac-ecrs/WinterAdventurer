using Microsoft.JSInterop;

namespace WinterAdventurer.Services;

/// <summary>
/// Manages guided tutorial/tour functionality using Driver.js and localStorage persistence.
/// Tracks tour completion state and provides methods to start/reset tours.
/// </summary>
public class TourService
{
    private readonly IJSRuntime _jsRuntime;

    public TourService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    /// <summary>
    /// Checks if a specific tour has been completed by the user.
    /// Uses localStorage to persist completion state across browser sessions.
    /// </summary>
    /// <param name="tourId">Identifier for the tour (e.g., "home").</param>
    /// <returns>True if tour was completed, false otherwise.</returns>
    public async Task<bool> HasCompletedTourAsync(string tourId)
    {
        try
        {
            var result = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", $"tour_{tourId}_completed");
            Console.WriteLine($"TourService: Tour '{tourId}' completed = {result == "true"}");
            return result == "true";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TourService: Error checking tour completion: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Starts the home page guided tour using Driver.js.
    /// Tour will automatically mark itself as completed upon finish or skip.
    /// </summary>
    public async Task StartHomeTourAsync()
    {
        try
        {
            Console.WriteLine("TourService: Calling startHomeTour");
            await _jsRuntime.InvokeVoidAsync("startHomeTour");
            Console.WriteLine("TourService: startHomeTour completed");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"TourService: Error calling startHomeTour: {ex.Message}");
        }
    }

    /// <summary>
    /// Resets tour completion state and restarts the tour.
    /// Used when user manually requests to see the tour again.
    /// </summary>
    /// <param name="tourId">Identifier for the tour to reset (e.g., "home").</param>
    public async Task ResetAndStartTourAsync(string tourId)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", $"tour_{tourId}_completed");

            if (tourId == "home")
            {
                await StartHomeTourAsync();
            }
        }
        catch
        {
            // Silently fail if JavaScript is not available
        }
    }
}
