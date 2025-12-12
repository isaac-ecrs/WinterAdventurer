// <copyright file="MapCompositor.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using System.Reflection;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using WinterAdventurer.Library.Exceptions;
#pragma warning disable SA1000 // Keyword 'new' should be followed by a space
#pragma warning disable SA1515 // Single-line comment should be preceded by blank line
#pragma warning disable CA1308 // Normalize strings to uppercase
#pragma warning disable CA1510 // Use ArgumentNullException.ThrowIfNull

namespace WinterAdventurer.Library.Services
{
    /// <summary>
    /// Handles facility map image composition by layering transparent PNG overlays on base layout.
    /// Uses SixLabors.ImageSharp for cross-platform image manipulation and supports caching for performance.
    /// </summary>
    public partial class MapCompositor : IDisposable
    {
        private readonly ILogger _logger;
        private readonly LocationMapResolver _locationMapResolver;
        private readonly Dictionary<string, string> _mapCache = new();
        private readonly List<string> _tempFiles = new();
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapCompositor"/> class.
        /// </summary>
        /// <param name="logger">Logger for diagnostic output.</param>
        /// <param name="locationMapResolver">Resolver for mapping location names to overlay images.</param>
        public MapCompositor(ILogger<MapCompositor> logger, LocationMapResolver locationMapResolver)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _locationMapResolver = locationMapResolver ?? throw new ArgumentNullException(nameof(locationMapResolver));
        }

        /// <summary>
        /// Composites a personalized facility map showing only the specified locations.
        /// Layers location-specific overlays on top of the base facility layout.
        /// Results are cached by location set for performance optimization.
        /// </summary>
        /// <param name="locationNames">List of location names to highlight on the map.</param>
        /// <returns>Path to temporary file containing the composited map image.</returns>
        /// <exception cref="MapCompositingException">Raised if image compositing fails.</exception>
        public string CompositeMap(List<string> locationNames)
        {
            if (locationNames == null)
            {
                throw new ArgumentNullException(nameof(locationNames));
            }

            // Create cache key from sorted, normalized location names
            var cacheKey = BuildCacheKey(locationNames);

            // Check cache first
            if (_mapCache.TryGetValue(cacheKey, out var cachedPath))
            {
                LogInformationMapCacheHit(cacheKey);
                return cachedPath;
            }

            try
            {
                LogInformationComposingMap(locationNames.Count);

                // Load base layout
                var baseLayoutResourceName = _locationMapResolver.BaseLayoutResourceName;
                using (var baseStream = LoadEmbeddedImage(baseLayoutResourceName))
                using (var baseImage = Image.Load<Rgba32>(baseStream))
                {
                    LogInformationBaseImageLoaded(baseImage.Width, baseImage.Height);

                    // Create working image by cloning the base
                    using (var compositedImage = baseImage.Clone())
                    {
                        // Layer overlays for each location
                        foreach (var location in locationNames)
                        {
                            var overlayResourceName = _locationMapResolver.ResolveOverlayResourceName(location);
                            if (overlayResourceName != null)
                            {
                                try
                                {
                                    using (var overlayStream = LoadEmbeddedImage(overlayResourceName))
                                    using (var overlayImage = Image.Load<Rgba32>(overlayStream))
                                    {
                                        LogInformationOverlayImageLoaded(location, overlayImage.Width, overlayImage.Height);

                                        // Ensure overlay matches base image dimensions
                                        // If overlay is different size, log warning but proceed (overlays should be same size as base)
                                        if (overlayImage.Width != compositedImage.Width || overlayImage.Height != compositedImage.Height)
                                        {
                                            LogWarningOverlaySizeMismatch(location, overlayImage.Width, overlayImage.Height, compositedImage.Width, compositedImage.Height);
                                        }

                                        // Composite overlay onto the image with full opacity for transparency blending
                                        compositedImage.Mutate(x => x.DrawImage(overlayImage, new Point(0, 0), 1.0f));
                                        LogInformationLayeredOverlay(location);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LogWarningFailedToLoadOverlay(location, ex);
                                    // Continue with next overlay, don't fail entire composition
                                }
                            }
                        }

                        // Save composited image to temp file
                        var tempPath = CreateCompositedImageFile(compositedImage);
                        _mapCache[cacheKey] = tempPath;

                        LogInformationMapComposited(cacheKey);
                        return tempPath;
                    }
                }
            }
            catch (MapCompositingException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new MapCompositingException("Failed to composite facility map", ex);
            }
        }

        /// <summary>
        /// Loads an embedded resource image stream from the assembly.
        /// </summary>
        /// <param name="resourceName">Full resource name of the image file.</param>
        /// <returns>Stream containing the image data.</returns>
        /// <exception cref="MapCompositingException">Raised if resource not found.</exception>
        private Stream LoadEmbeddedImage(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var stream = assembly.GetManifestResourceStream(resourceName);

            if (stream == null)
            {
                throw new MapCompositingException($"Embedded resource not found: {resourceName}");
            }

            return stream;
        }

        /// <summary>
        /// Saves composited image to a temporary PNG file.
        /// </summary>
        /// <param name="image">ImageSharp image containing the composited image.</param>
        /// <returns>Path to the temporary file.</returns>
        /// <exception cref="MapCompositingException">Raised if file save fails.</exception>
        private string CreateCompositedImageFile(Image<Rgba32> image)
        {
            try
            {
                var tempPath = Path.Combine(Path.GetTempPath(), $"personalized_map_{Guid.NewGuid()}.png");

                // Save with maximum PNG compression level for quality
                // Compression level 9 (maximum) provides best quality without lossy compression
                using (var fileStream = File.Create(tempPath))
                {
                    var encoder = new PngEncoder { CompressionLevel = (PngCompressionLevel)9 };
                    image.SaveAsPng(fileStream, encoder);
                }

                _tempFiles.Add(tempPath);
                LogInformationSavedTempFile(tempPath);
                return tempPath;
            }
            catch (Exception ex)
            {
                throw new MapCompositingException("Failed to save composited map to temporary file", ex);
            }
        }

        /// <summary>
        /// Builds a cache key from a list of location names.
        /// Normalizes by sorting and joining with pipe character.
        /// </summary>
        /// <param name="locationNames">List of location names to create cache key from.</param>
        /// <returns>Cache key string.</returns>
        private static string BuildCacheKey(List<string> locationNames)
        {
            return string.Join("|", locationNames.OrderBy(l => l).Select(l => l.Trim().ToLowerInvariant()));
        }

        /// <summary>
        /// Disposes resources and cleans up temporary files.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Internal dispose implementation.
        /// </summary>
        /// <param name="disposing">True if called from Dispose, false if from finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Clean up temporary files
                foreach (var file in _tempFiles)
                {
                    try
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                            LogInformationDeletedTempFile(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        LogWarningFailedToDeleteTempFile(file, ex);
                    }
                }

                _tempFiles.Clear();
                _mapCache.Clear();
            }

            _disposed = true;
        }

        #region Logging

        [LoggerMessage(
            EventId = 8001,
            Level = LogLevel.Information,
            Message = "Cache hit for map composition: {cacheKey}")]
        private partial void LogInformationMapCacheHit(string cacheKey);

        [LoggerMessage(
            EventId = 8002,
            Level = LogLevel.Information,
            Message = "Starting map composition for {locationCount} locations")]
        private partial void LogInformationComposingMap(int locationCount);

        [LoggerMessage(
            EventId = 8003,
            Level = LogLevel.Information,
            Message = "Layered overlay for location: {location}")]
        private partial void LogInformationLayeredOverlay(string location);

        [LoggerMessage(
            EventId = 8004,
            Level = LogLevel.Warning,
            Message = "Failed to load overlay for location '{location}'")]
        private partial void LogWarningFailedToLoadOverlay(string location, Exception ex);

        [LoggerMessage(
            EventId = 8005,
            Level = LogLevel.Information,
            Message = "Map composition completed and cached: {cacheKey}")]
        private partial void LogInformationMapComposited(string cacheKey);

        [LoggerMessage(
            EventId = 8006,
            Level = LogLevel.Information,
            Message = "Saved composited map to temporary file: {tempPath}")]
        private partial void LogInformationSavedTempFile(string tempPath);

        [LoggerMessage(
            EventId = 8007,
            Level = LogLevel.Information,
            Message = "Deleted temporary map file: {tempPath}")]
        private partial void LogInformationDeletedTempFile(string tempPath);

        [LoggerMessage(
            EventId = 8008,
            Level = LogLevel.Warning,
            Message = "Failed to delete temporary map file: {tempPath}")]
        private partial void LogWarningFailedToDeleteTempFile(string tempPath, Exception ex);

        [LoggerMessage(
            EventId = 8009,
            Level = LogLevel.Information,
            Message = "Base layout image loaded: {baseImageWidth}x{baseImageHeight}")]
        private partial void LogInformationBaseImageLoaded(int baseImageWidth, int baseImageHeight);

        [LoggerMessage(
            EventId = 8010,
            Level = LogLevel.Information,
            Message = "Overlay image loaded for '{location}': {overlayImageWidth}x{overlayImageHeight}")]
        private partial void LogInformationOverlayImageLoaded(string location, int overlayImageWidth, int overlayImageHeight);

        [LoggerMessage(
            EventId = 8011,
            Level = LogLevel.Warning,
            Message = "Overlay size mismatch for '{location}': overlay is {overlayImageWidth}x{overlayImageHeight}, base is {baseImageWidth}x{baseImageHeight}")]
        private partial void LogWarningOverlaySizeMismatch(string location, int overlayImageWidth, int overlayImageHeight, int baseImageWidth, int baseImageHeight);

        #endregion
    }
}
