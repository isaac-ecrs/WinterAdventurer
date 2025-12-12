// <copyright file="LocationMapResolver.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
#pragma warning disable SA1000 // Keyword 'new' should be followed by a space
#pragma warning disable SA1201 // Element should come before other declarations
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
#pragma warning disable CA1852 // Type can be sealed

namespace WinterAdventurer.Library.Services
{
    /// <summary>
    /// Resolves workshop location names to facility map overlay image filenames.
    /// Uses configuration-driven approach with JSON mapping file for flexibility.
    /// </summary>
    public partial class LocationMapResolver
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, string> _locationMappings = new(StringComparer.OrdinalIgnoreCase);
        private string _baseLayoutResourceName = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationMapResolver"/> class.
        /// Loads location mappings from embedded LocationMapConfiguration.json.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        public LocationMapResolver(ILogger<LocationMapResolver> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LoadMappingConfiguration();
        }

        /// <summary>
        /// Resolves a location name to the corresponding facility map overlay resource name.
        /// Handles case-insensitive and whitespace variations in location names.
        /// </summary>
        /// <param name="locationName">The location name to resolve (e.g., "Chapel A").</param>
        /// <returns>Full resource name for the overlay image, or null if location has no overlay.</returns>
        public string? ResolveOverlayResourceName(string locationName)
        {
            if (string.IsNullOrWhiteSpace(locationName))
            {
                return null;
            }

            // Normalize the location name
            var normalizedName = NormalizeLocationName(locationName);

            if (_locationMappings.TryGetValue(normalizedName, out var overlayFileName))
            {
                // Construct the full resource name
                var resourceName = $"WinterAdventurer.Library.Resources.Images.WatsonMaps.{overlayFileName}";
                LogInformationResolvedLocation(locationName, resourceName);
                return resourceName;
            }

            LogWarningLocationNotFound(locationName);
            return null;
        }

        /// <summary>
        /// Gets the resource name for the base facility layout image.
        /// </summary>
        public string BaseLayoutResourceName => _baseLayoutResourceName;

        /// <summary>
        /// Normalizes a location name for lookup (case-insensitive, whitespace trimmed).
        /// </summary>
        /// <param name="name">The location name to normalize.</param>
        /// <returns>Normalized location name.</returns>
        private static string NormalizeLocationName(string name)
        {
            // Trim whitespace and apply case-insensitive comparison by preserving original case
            // The dictionary lookup will be case-insensitive due to OrdinalIgnoreCase comparer
            return name.Trim();
        }

        /// <summary>
        /// Loads location mapping configuration from embedded JSON resource.
        /// </summary>
        private void LoadMappingConfiguration()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "WinterAdventurer.Library.EventSchemas.LocationMapConfiguration.json";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        LogErrorConfigurationNotFound();
                        return;
                    }

                    using (var reader = new StreamReader(stream))
                    {
                        var json = reader.ReadToEnd();
                        var config = JsonConvert.DeserializeObject<LocationMapConfig>(json);

                        if (config == null)
                        {
                            LogErrorConfigurationInvalid();
                            return;
                        }

                        _baseLayoutResourceName = config.BaseLayoutResourceName;

                        // Load mappings (case-insensitive keys)
                        foreach (var kvp in config.LocationMappings)
                        {
                            _locationMappings[kvp.Key] = kvp.Value;
                        }

                        LogInformationConfigurationLoaded(_locationMappings.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                LogErrorLoadingConfiguration(ex);
            }
        }

        /// <summary>
        /// Internal class for deserializing the location map configuration JSON.
        /// </summary>
        private class LocationMapConfig
        {
            [JsonProperty("baseLayoutResourceName")]
            public string BaseLayoutResourceName { get; set; } = string.Empty;

            [JsonProperty("locationMappings")]
            public Dictionary<string, string> LocationMappings { get; set; } = new();

            [JsonProperty("fallbackBehavior")]
            public string FallbackBehavior { get; set; } = "useBaseLayoutOnly";
        }

        #region Logging

        [LoggerMessage(
            EventId = 7001,
            Level = LogLevel.Information,
            Message = "Resolved location '{location}' to resource '{resourceName}'")]
        private partial void LogInformationResolvedLocation(string location, string resourceName);

        [LoggerMessage(
            EventId = 7002,
            Level = LogLevel.Warning,
            Message = "Location '{location}' not found in map configuration - no overlay available")]
        private partial void LogWarningLocationNotFound(string location);

        [LoggerMessage(
            EventId = 7003,
            Level = LogLevel.Information,
            Message = "LocationMapConfiguration loaded successfully with {count} location mappings")]
        private partial void LogInformationConfigurationLoaded(int count);

        [LoggerMessage(
            EventId = 7004,
            Level = LogLevel.Error,
            Message = "LocationMapConfiguration.json resource not found in assembly")]
        private partial void LogErrorConfigurationNotFound();

        [LoggerMessage(
            EventId = 7005,
            Level = LogLevel.Error,
            Message = "LocationMapConfiguration.json could not be parsed as valid JSON")]
        private partial void LogErrorConfigurationInvalid();

        [LoggerMessage(
            EventId = 7006,
            Level = LogLevel.Error,
            Message = "Error loading LocationMapConfiguration")]
        private partial void LogErrorLoadingConfiguration(Exception ex);

        #endregion
    }
}
