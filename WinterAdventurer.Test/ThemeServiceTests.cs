using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinterAdventurer.Services;

namespace WinterAdventurer.Test;

/// <summary>
/// Tests for ThemeService theme management and persistence.
/// </summary>
[TestClass]
public class ThemeServiceTests
{
    #region InitializeAsync Tests

    [TestMethod]
    public async Task InitializeAsync_NoSavedTheme_DefaultsToDarkMode()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.SetLocalStorageValue("theme", null);
        var service = new ThemeService(jsRuntime);

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.IsTrue(service.IsDarkMode);
    }

    [TestMethod]
    public async Task InitializeAsync_SavedThemeDark_LoadsDarkMode()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.SetLocalStorageValue("theme", "dark");
        var service = new ThemeService(jsRuntime);

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.IsTrue(service.IsDarkMode);
    }

    [TestMethod]
    public async Task InitializeAsync_SavedThemeLight_LoadsLightMode()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.SetLocalStorageValue("theme", "light");
        var service = new ThemeService(jsRuntime);

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.IsFalse(service.IsDarkMode);
    }

    [TestMethod]
    public async Task InitializeAsync_InvalidSavedTheme_DefaultsToDarkMode()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.SetLocalStorageValue("theme", "invalid");
        var service = new ThemeService(jsRuntime);

        // Act
        await service.InitializeAsync();

        // Assert - anything other than "light" should result in dark mode
        Assert.IsTrue(service.IsDarkMode);
    }

    [TestMethod]
    public async Task InitializeAsync_JSInteropFails_DefaultsToDarkMode()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.ThrowOnLocalStorageAccess = true;
        var service = new ThemeService(jsRuntime);

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.IsTrue(service.IsDarkMode);
    }

    [TestMethod]
    public async Task InitializeAsync_UpdatesTourTheme()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.SetLocalStorageValue("theme", "dark");
        var service = new ThemeService(jsRuntime);

        // Act
        await service.InitializeAsync();

        // Assert
        Assert.IsTrue(jsRuntime.UpdateTourThemeCalled);
        Assert.IsTrue(jsRuntime.LastTourThemeValue);
    }

    #endregion

    #region ToggleThemeAsync Tests

    [TestMethod]
    public async Task ToggleThemeAsync_FromDarkToLight_UpdatesTheme()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.SetLocalStorageValue("theme", "dark");
        var service = new ThemeService(jsRuntime);
        await service.InitializeAsync();

        // Act
        await service.ToggleThemeAsync();

        // Assert
        Assert.IsFalse(service.IsDarkMode);
    }

    [TestMethod]
    public async Task ToggleThemeAsync_FromLightToDark_UpdatesTheme()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.SetLocalStorageValue("theme", "light");
        var service = new ThemeService(jsRuntime);
        await service.InitializeAsync();

        // Act
        await service.ToggleThemeAsync();

        // Assert
        Assert.IsTrue(service.IsDarkMode);
    }

    [TestMethod]
    public async Task ToggleThemeAsync_SavesThemeToLocalStorage()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        var service = new ThemeService(jsRuntime);
        await service.InitializeAsync();

        // Act - toggle to light mode
        await service.ToggleThemeAsync();

        // Assert
        var savedValue = jsRuntime.GetLocalStorageValue("theme");
        Assert.AreEqual("light", savedValue);
    }

    [TestMethod]
    public async Task ToggleThemeAsync_MultipleToggles_AlternatesCorrectly()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        var service = new ThemeService(jsRuntime);
        await service.InitializeAsync();
        var initialMode = service.IsDarkMode;

        // Act & Assert
        await service.ToggleThemeAsync();
        Assert.AreEqual(!initialMode, service.IsDarkMode);

        await service.ToggleThemeAsync();
        Assert.AreEqual(initialMode, service.IsDarkMode);

        await service.ToggleThemeAsync();
        Assert.AreEqual(!initialMode, service.IsDarkMode);
    }

    [TestMethod]
    public async Task ToggleThemeAsync_UpdatesTourTheme()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        var service = new ThemeService(jsRuntime);
        await service.InitializeAsync();
        jsRuntime.UpdateTourThemeCalled = false;

        // Act
        await service.ToggleThemeAsync();

        // Assert
        Assert.IsTrue(jsRuntime.UpdateTourThemeCalled);
        Assert.IsFalse(jsRuntime.LastTourThemeValue); // Should be light mode
    }

    [TestMethod]
    public async Task ToggleThemeAsync_RaisesOnThemeChangedEvent()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        var service = new ThemeService(jsRuntime);
        await service.InitializeAsync();

        bool eventRaised = false;
        service.OnThemeChanged += () => eventRaised = true;

        // Act
        await service.ToggleThemeAsync();

        // Assert
        Assert.IsTrue(eventRaised);
    }

    [TestMethod]
    public async Task ToggleThemeAsync_JSInteropFails_StillUpdatesTheme()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        var service = new ThemeService(jsRuntime);
        await service.InitializeAsync();

        // Make JSInterop fail
        jsRuntime.ThrowOnLocalStorageAccess = true;
        var initialMode = service.IsDarkMode;

        // Act
        await service.ToggleThemeAsync();

        // Assert - theme should still toggle even if persistence fails
        Assert.AreEqual(!initialMode, service.IsDarkMode);
    }

    [TestMethod]
    public async Task ToggleThemeAsync_UpdateTourThemeFails_DoesNotThrow()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        var service = new ThemeService(jsRuntime);
        await service.InitializeAsync();
        jsRuntime.ThrowOnUpdateTourTheme = true;

        // Act & Assert - should not throw
        await service.ToggleThemeAsync();
        Assert.IsFalse(service.IsDarkMode); // Should still toggle
    }

    #endregion

    #region Integration Tests

    [TestMethod]
    public async Task ThemeService_CompleteWorkflow_InitializeToggleAndPersist()
    {
        // Arrange
        var jsRuntime = new MockJSRuntime();
        jsRuntime.SetLocalStorageValue("theme", "dark");
        var service = new ThemeService(jsRuntime);

        // Act - initialize
        await service.InitializeAsync();
        Assert.IsTrue(service.IsDarkMode);

        // Act - toggle to light
        await service.ToggleThemeAsync();
        Assert.IsFalse(service.IsDarkMode);
        Assert.AreEqual("light", jsRuntime.GetLocalStorageValue("theme"));

        // Act - toggle back to dark
        await service.ToggleThemeAsync();
        Assert.IsTrue(service.IsDarkMode);
        Assert.AreEqual("dark", jsRuntime.GetLocalStorageValue("theme"));
    }

    #endregion

    #region Mock JSRuntime

    public class MockJSRuntime : IJSRuntime
    {
        private readonly Dictionary<string, string?> _localStorage = new();
        public bool ThrowOnLocalStorageAccess { get; set; }
        public bool ThrowOnUpdateTourTheme { get; set; }
        public bool UpdateTourThemeCalled { get; set; }
        public bool LastTourThemeValue { get; set; }

        public void SetLocalStorageValue(string key, string? value)
        {
            _localStorage[key] = value;
        }

        public string? GetLocalStorageValue(string key)
        {
            return _localStorage.TryGetValue(key, out var value) ? value : null;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            if (identifier == "localStorage.getItem")
            {
                if (ThrowOnLocalStorageAccess)
                    throw new JSException("localStorage not available");

                var key = args?[0]?.ToString() ?? "";
                var value = GetLocalStorageValue(key);
                return new ValueTask<TValue>((TValue)(object)value!);
            }

            if (identifier == "localStorage.setItem")
            {
                if (ThrowOnLocalStorageAccess)
                    throw new JSException("localStorage not available");

                var key = args?[0]?.ToString() ?? "";
                var value = args?[1]?.ToString();
                SetLocalStorageValue(key, value);
                return default;
            }

            if (identifier == "updateTourTheme")
            {
                if (ThrowOnUpdateTourTheme)
                    throw new JSException("updateTourTheme failed");

                UpdateTourThemeCalled = true;
                if (args != null && args.Length > 0)
                {
                    LastTourThemeValue = (bool)args[0]!;
                }
                return default;
            }

            return default;
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, args);
        }
    }

    #endregion
}

/// <summary>
/// Extension methods for ThemeService testing.
/// </summary>
internal static class ThemeServiceTestExtensions
{
    public static async Task InvokeVoidAsync(this IJSRuntime jsRuntime, string identifier, params object?[] args)
    {
        if (identifier == "localStorage.setItem")
        {
            var mockRuntime = jsRuntime as ThemeServiceTests.MockJSRuntime;
            if (mockRuntime != null)
            {
                if (mockRuntime.ThrowOnLocalStorageAccess)
                    throw new JSException("localStorage not available");

                var key = args[0]?.ToString() ?? "";
                var value = args[1]?.ToString();
                mockRuntime.SetLocalStorageValue(key, value);
            }
        }
        else if (identifier == "updateTourTheme")
        {
            var mockRuntime = jsRuntime as ThemeServiceTests.MockJSRuntime;
            if (mockRuntime != null)
            {
                if (mockRuntime.ThrowOnUpdateTourTheme)
                    throw new JSException("updateTourTheme failed");

                mockRuntime.UpdateTourThemeCalled = true;
                if (args.Length > 0)
                {
                    mockRuntime.LastTourThemeValue = (bool)args[0]!;
                }
            }
        }

        await Task.CompletedTask;
    }
}
