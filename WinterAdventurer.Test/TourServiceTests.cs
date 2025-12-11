// <copyright file="TourServiceTests.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using Microsoft.JSInterop;
using WinterAdventurer.Services;

namespace WinterAdventurer.Test;

/// <summary>
/// Tests for TourService guided tour management.
/// </summary>
[TestClass]
public class TourServiceTests
{
    [TestMethod]
    public async Task HasCompletedTourAsync_TourCompleted_ReturnsTrue()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.SetLocalStorageValue("tour_home_completed", "true");
        var service = new TourService(jsRuntime);

        // Act
        var result = await service.HasCompletedTourAsync("home");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task HasCompletedTourAsync_TourNotCompleted_ReturnsFalse()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.SetLocalStorageValue("tour_home_completed", "false");
        var service = new TourService(jsRuntime);

        // Act
        var result = await service.HasCompletedTourAsync("home");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task HasCompletedTourAsync_NoSavedValue_ReturnsFalse()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.SetLocalStorageValue("tour_home_completed", null);
        var service = new TourService(jsRuntime);

        // Act
        var result = await service.HasCompletedTourAsync("home");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task HasCompletedTourAsync_JSInteropFails_ReturnsFalse()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.ThrowOnLocalStorageAccess = true;
        var service = new TourService(jsRuntime);

        // Act
        var result = await service.HasCompletedTourAsync("home");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task HasCompletedTourAsync_DifferentTourIds_ChecksSeparately()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.SetLocalStorageValue("tour_home_completed", "true");
        jsRuntime.SetLocalStorageValue("tour_settings_completed", "false");
        var service = new TourService(jsRuntime);

        // Act
        var homeResult = await service.HasCompletedTourAsync("home");
        var settingsResult = await service.HasCompletedTourAsync("settings");

        // Assert
        Assert.IsTrue(homeResult);
        Assert.IsFalse(settingsResult);
    }

    [TestMethod]
    public async Task StartHomeTourAsync_CallsJavaScript()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        var service = new TourService(jsRuntime);

        // Act
        await service.StartHomeTourAsync();

        // Assert
        Assert.IsTrue(jsRuntime.StartHomeTourCalled);
    }

    [TestMethod]
    public async Task StartHomeTourAsync_JSInteropFails_DoesNotThrow()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.ThrowOnStartHomeTour = true;
        var service = new TourService(jsRuntime);

        // Act & Assert - should not throw
        await service.StartHomeTourAsync();
    }

    [TestMethod]
    public async Task ResetAndStartTourAsync_HomeTour_RemovesCompletionAndStarts()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.SetLocalStorageValue("tour_home_completed", "true");
        var service = new TourService(jsRuntime);

        // Act
        await service.ResetAndStartTourAsync("home");

        // Assert
        Assert.IsTrue(jsRuntime.RemovedKeys.Contains("tour_home_completed"));
        Assert.IsTrue(jsRuntime.StartHomeTourCalled);
    }

    [TestMethod]
    public async Task ResetAndStartTourAsync_NonHomeTour_OnlyRemovesCompletion()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.SetLocalStorageValue("tour_settings_completed", "true");
        var service = new TourService(jsRuntime);

        // Act
        await service.ResetAndStartTourAsync("settings");

        // Assert
        Assert.IsTrue(jsRuntime.RemovedKeys.Contains("tour_settings_completed"));
        Assert.IsFalse(jsRuntime.StartHomeTourCalled);
    }

    [TestMethod]
    public async Task ResetAndStartTourAsync_JSInteropFails_DoesNotThrow()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.ThrowOnLocalStorageAccess = true;
        var service = new TourService(jsRuntime);

        // Act & Assert - should not throw
        await service.ResetAndStartTourAsync("home");
    }

    [TestMethod]
    public async Task TourService_CompleteWorkflow_CheckStartResetAndCheck()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        var service = new TourService(jsRuntime);

        // Act - initially not completed
        var initialCheck = await service.HasCompletedTourAsync("home");
        Assert.IsFalse(initialCheck);

        // Act - start tour
        await service.StartHomeTourAsync();
        Assert.IsTrue(jsRuntime.StartHomeTourCalled);

        // Act - mark as completed
        jsRuntime.SetLocalStorageValue("tour_home_completed", "true");
        var afterCompletion = await service.HasCompletedTourAsync("home");
        Assert.IsTrue(afterCompletion);

        // Act - reset and start again
        jsRuntime.StartHomeTourCalled = false;
        await service.ResetAndStartTourAsync("home");
        Assert.IsTrue(jsRuntime.RemovedKeys.Contains("tour_home_completed"));
        Assert.IsTrue(jsRuntime.StartHomeTourCalled);
    }

    public class MockJSRuntime : IJSRuntime
    {
        private readonly Dictionary<string, string?> _localStorage = new ();
        public HashSet<string> RemovedKeys { get; } = new ();
        public bool ThrowOnLocalStorageAccess { get; set; }
        public bool ThrowOnStartHomeTour { get; set; }
        public bool StartHomeTourCalled { get; set; }

        public void SetLocalStorageValue(string key, string? value)
        {
            _localStorage[key] = value;
        }

        public string? GetLocalStorageValue(string key)
        {
            return _localStorage.TryGetValue(key, out var value) ? value : null;
        }

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[] ? args)
        {
            if (identifier == "localStorage.getItem")
            {
                if (ThrowOnLocalStorageAccess)
                {
                    throw new JSException("localStorage not available");
                }

                var key = args?[0]?.ToString() ?? string.Empty;
                var value = GetLocalStorageValue(key);
                return new ValueTask<TValue>((TValue)(object)value!);
            }

            if (identifier == "localStorage.removeItem")
            {
                if (ThrowOnLocalStorageAccess)
                {
                    throw new JSException("localStorage not available");
                }

                var key = args?[0]?.ToString() ?? string.Empty;
                RemovedKeys.Add(key);
                _localStorage.Remove(key);
                return default;
            }

            if (identifier == "startHomeTour")
            {
                if (ThrowOnStartHomeTour)
                {
                    throw new JSException("startHomeTour failed");
                }

                StartHomeTourCalled = true;
                return default;
            }

            return default;
        }

        /// <inheritdoc/>
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[] ? args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }
    }
}

/// <summary>
/// Extension methods for TourService testing.
/// </summary>
internal static class TourServiceTestExtensions
{
    public static async Task InvokeVoidAsync(this IJSRuntime jsRuntime, string identifier, params object?[] args)
    {
        if (identifier == "localStorage.removeItem")
        {
            var mockRuntime = jsRuntime as TourServiceTests.MockJSRuntime;
            if (mockRuntime != null)
            {
                if (mockRuntime.ThrowOnLocalStorageAccess)
                {
                    throw new JSException("localStorage not available");
                }

                var key = args[0]?.ToString() ?? string.Empty;
                mockRuntime.RemovedKeys.Add(key);
            }
        }
        else if (identifier == "startHomeTour")
        {
            var mockRuntime = jsRuntime as TourServiceTests.MockJSRuntime;
            if (mockRuntime != null)
            {
                if (mockRuntime.ThrowOnStartHomeTour)
                {
                    throw new JSException("startHomeTour failed");
                }

                mockRuntime.StartHomeTourCalled = true;
            }
        }

        await Task.CompletedTask;
    }
}
