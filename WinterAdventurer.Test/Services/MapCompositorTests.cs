// <copyright file="MapCompositorTests.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinterAdventurer.Library.Exceptions;
using WinterAdventurer.Library.Services;

namespace WinterAdventurer.Test.Services
{
    /// <summary>
    /// Tests for MapCompositor service.
    /// Covers image compositing, caching, file handling, and error scenarios.
    /// </summary>
    [TestClass]
    public class MapCompositorTests
    {
        private MapCompositor _compositor = null!;
        private LocationMapResolver _resolver = null!;

        [TestInitialize]
        public void Setup()
        {
            _resolver = new LocationMapResolver(NullLogger<LocationMapResolver>.Instance);
            _compositor = new MapCompositor(NullLogger<MapCompositor>.Instance, _resolver);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _compositor?.Dispose();
        }

        [TestMethod]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = new MapCompositor(null!, _resolver);
            });
            Assert.IsNotNull(ex);
        }

        [TestMethod]
        public void Constructor_WithNullResolver_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = new MapCompositor(NullLogger<MapCompositor>.Instance, null!);
            });
            Assert.IsNotNull(ex);
        }

        [TestMethod]
        public void CompositeMap_WithNullLocationList_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _compositor.CompositeMap(null!);
            });
        }

        [TestMethod]
        public void CompositeMap_WithEmptyLocationList_ReturnsBaseLayoutPath()
        {
            // Arrange
            var locations = new List<string>();

            // Act
            var mapPath = _compositor.CompositeMap(locations);

            // Assert
            Assert.IsNotNull(mapPath);
            Assert.IsTrue(File.Exists(mapPath), "Composited map file should exist");
            Assert.IsTrue(new FileInfo(mapPath).Length > 0, "Composited map file should not be empty");
        }

        [TestMethod]
        public void CompositeMap_WithSingleKnownLocation_ReturnsValidImagePath()
        {
            // Arrange
            var locations = new List<string> { "Chapel A" };

            // Act
            var mapPath = _compositor.CompositeMap(locations);

            // Assert
            Assert.IsNotNull(mapPath);
            Assert.IsTrue(File.Exists(mapPath), "Composited map file should exist");
            Assert.IsTrue(mapPath.EndsWith(".png"), "Composited map should be PNG file");
            Assert.IsTrue(new FileInfo(mapPath).Length > 0, "Composited map file should not be empty");
        }

        [TestMethod]
        public void CompositeMap_WithMultipleKnownLocations_ReturnsValidImagePath()
        {
            // Arrange
            var locations = new List<string> { "Chapel A", "Dining Room", "Library" };

            // Act
            var mapPath = _compositor.CompositeMap(locations);

            // Assert
            Assert.IsNotNull(mapPath);
            Assert.IsTrue(File.Exists(mapPath), "Composited map file should exist");
            Assert.IsTrue(new FileInfo(mapPath).Length > 0, "Composited map file should not be empty");
        }

        [TestMethod]
        public void CompositeMap_WithUnknownLocation_SkipsOverlayGracefully()
        {
            // Arrange
            var locations = new List<string> { "Unknown Room", "Chapel A" };

            // Act
            var mapPath = _compositor.CompositeMap(locations);

            // Assert
            Assert.IsNotNull(mapPath);
            Assert.IsTrue(File.Exists(mapPath), "Should still create map with known locations despite unknown location");
        }

        [TestMethod]
        public void CompositeMap_CachesSameLocationSet_ReturnsSamePath()
        {
            // Arrange
            var locations = new List<string> { "Chapel A", "Dining Room" };

            // Act
            var mapPath1 = _compositor.CompositeMap(locations);
            var mapPath2 = _compositor.CompositeMap(locations);

            // Assert
            Assert.AreEqual(mapPath1, mapPath2, "Same location set should return cached path");
        }

        [TestMethod]
        public void CompositeMap_DifferentLocationSets_ReturnsDifferentPaths()
        {
            // Arrange
            var locations1 = new List<string> { "Chapel A" };
            var locations2 = new List<string> { "Dining Room" };

            // Act
            var mapPath1 = _compositor.CompositeMap(locations1);
            var mapPath2 = _compositor.CompositeMap(locations2);

            // Assert
            Assert.AreNotEqual(mapPath1, mapPath2, "Different location sets should create different maps");
        }

        [TestMethod]
        public void CompositeMap_SameLocationsInDifferentOrder_ReturnsSamePath()
        {
            // Arrange
            var locations1 = new List<string> { "Chapel A", "Dining Room" };
            var locations2 = new List<string> { "Dining Room", "Chapel A" };

            // Act
            var mapPath1 = _compositor.CompositeMap(locations1);
            var mapPath2 = _compositor.CompositeMap(locations2);

            // Assert
            Assert.AreEqual(mapPath1, mapPath2, "Same locations in different order should use cache");
        }

        [TestMethod]
        public void Dispose_DeletesTemporaryFiles()
        {
            // Arrange
            var locations = new List<string> { "Chapel A" };
            var mapPath = _compositor.CompositeMap(locations);
            Assert.IsTrue(File.Exists(mapPath), "File should exist before dispose");

            // Act
            _compositor.Dispose();

            // Assert
            Assert.IsFalse(File.Exists(mapPath), "Temp file should be deleted after dispose");
        }

        [TestMethod]
        public void Dispose_WithMultipleTemporaryFiles_DeletesAll()
        {
            // Arrange
            var paths = new List<string>();
            paths.Add(_compositor.CompositeMap(new List<string> { "Chapel A" }));
            paths.Add(_compositor.CompositeMap(new List<string> { "Dining Room" }));
            paths.Add(_compositor.CompositeMap(new List<string> { "Library" }));

            // Verify all files exist
            foreach (var path in paths)
            {
                Assert.IsTrue(File.Exists(path), $"File {path} should exist before dispose");
            }

            // Act
            _compositor.Dispose();

            // Assert
            foreach (var path in paths)
            {
                Assert.IsFalse(File.Exists(path), $"File {path} should be deleted after dispose");
            }
        }

        [TestMethod]
        public void Dispose_CalledMultipleTimes_DoesNotThrow()
        {
            // Arrange
            var locations = new List<string> { "Chapel A" };
            _compositor.CompositeMap(locations);

            // Act & Assert - should not throw
            _compositor.Dispose();
            _compositor.Dispose(); // Second dispose should be safe
        }

        [TestMethod]
        public void CompositeMap_CreatedFilesAreValidPngs()
        {
            // Arrange
            var locations = new List<string> { "Chapel A", "Dining Room" };

            // Act
            var mapPath = _compositor.CompositeMap(locations);

            // Assert
            Assert.IsTrue(File.Exists(mapPath));
            var fileBytes = File.ReadAllBytes(mapPath);

            // PNG file signature: 89 50 4E 47
            Assert.IsTrue(
                fileBytes[0] == 0x89 && fileBytes[1] == 0x50 && fileBytes[2] == 0x4E && fileBytes[3] == 0x47,
                "File should be valid PNG with correct signature");
        }

        [TestMethod]
        public void CompositeMap_WithCaseInsensitiveLocations_HandlesProperly()
        {
            // Arrange
            var locations1 = new List<string> { "Chapel A" };
            var locations2 = new List<string> { "CHAPEL A" };

            // Act
            var mapPath1 = _compositor.CompositeMap(locations1);
            var mapPath2 = _compositor.CompositeMap(locations2);

            // Assert
            Assert.AreEqual(mapPath1, mapPath2, "Case-insensitive location names should cache consistently");
        }

        [TestMethod]
        public void CompositeMap_WithWhitespaceVariations_HandlesProperly()
        {
            // Arrange
            var locations1 = new List<string> { "Chapel A" };
            var locations2 = new List<string> { "  Chapel A  " };

            // Act
            var mapPath1 = _compositor.CompositeMap(locations1);
            var mapPath2 = _compositor.CompositeMap(locations2);

            // Assert
            Assert.AreEqual(mapPath1, mapPath2, "Whitespace variations should cache consistently");
        }

        [TestMethod]
        public void CompositeMap_TempFilesInCorrectDirectory()
        {
            // Arrange
            var expectedTempDir = Path.GetTempPath();

            // Act
            var mapPath = _compositor.CompositeMap(new List<string> { "Chapel A" });

            // Assert
            Assert.IsTrue(mapPath.StartsWith(expectedTempDir), "Temp file should be in system temp directory");
            Assert.IsTrue(mapPath.Contains("personalized_map_"), "Temp file should have expected naming pattern");
        }

        [TestMethod]
        public void CompositeMap_MultipleInstances_ProduceValidMaps()
        {
            // Arrange - create multiple compositor instances
            var compositor1 = new MapCompositor(NullLogger<MapCompositor>.Instance, _resolver);
            var compositor2 = new MapCompositor(NullLogger<MapCompositor>.Instance, _resolver);
            var locations = new List<string> { "Chapel A" };

            try
            {
                // Act
                var mapPath1 = compositor1.CompositeMap(locations);
                var mapPath2 = compositor2.CompositeMap(locations);

                // Assert
                Assert.IsNotNull(mapPath1);
                Assert.IsNotNull(mapPath2);
                Assert.IsTrue(File.Exists(mapPath1));
                Assert.IsTrue(File.Exists(mapPath2));

                // Different instances create different temp files (even for same locations)
                Assert.AreNotEqual(mapPath1, mapPath2);
            }
            finally
            {
                compositor1.Dispose();
                compositor2.Dispose();
            }
        }
    }
}
