// <copyright file="LocationMapResolverTests.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WinterAdventurer.Library.Services;

namespace WinterAdventurer.Test.Services
{
    /// <summary>
    /// Tests for LocationMapResolver service.
    /// Covers configuration loading, location resolution, and case-insensitive matching.
    /// </summary>
    [TestClass]
    public class LocationMapResolverTests
    {
        private LocationMapResolver _resolver = null!;

        [TestInitialize]
        public void Setup()
        {
            _resolver = new LocationMapResolver(NullLogger<LocationMapResolver>.Instance);
        }

        [TestMethod]
        public void Constructor_LoadsConfiguration_Successfully()
        {
            // The constructor loads the configuration
            // If it loaded successfully, BaseLayoutResourceName should be set
            Assert.IsNotNull(_resolver.BaseLayoutResourceName);
            Assert.IsTrue(_resolver.BaseLayoutResourceName.Contains("watson_layout.png"));
        }

        [TestMethod]
        public void BaseLayoutResourceName_ReturnsCorrectResourceName()
        {
            // Arrange & Act
            var resourceName = _resolver.BaseLayoutResourceName;

            // Assert
            Assert.IsTrue(resourceName.StartsWith("WinterAdventurer.Library.Resources.Images.WatsonMaps"));
            Assert.IsTrue(resourceName.EndsWith("watson_layout.png"));
        }

        [TestMethod]
        public void ResolveOverlayResourceName_WithKnownLocation_ReturnsCorrectResourceName()
        {
            // Arrange
            var knownLocation = "Chapel A";

            // Act
            var resourceName = _resolver.ResolveOverlayResourceName(knownLocation);

            // Assert
            Assert.IsNotNull(resourceName);
            Assert.IsTrue(resourceName!.StartsWith("WinterAdventurer.Library.Resources.Images.WatsonMaps"));
            Assert.IsTrue(resourceName.Contains("chapel_a.png"), $"Expected chapel_a.png in resource name, got: {resourceName}");
        }

        [TestMethod]
        public void ResolveOverlayResourceName_CaseInsensitive_ReturnsCorrectResourceName()
        {
            // Arrange
            var locationVariation = "CHAPEL A"; // Upper case

            // Act
            var resourceName = _resolver.ResolveOverlayResourceName(locationVariation);

            // Assert
            Assert.IsNotNull(resourceName);
            Assert.IsTrue(resourceName!.Contains("chapel_a.png"), $"Expected case-insensitive match for CHAPEL A, got: {resourceName}");
        }

        [TestMethod]
        public void ResolveOverlayResourceName_MixedCase_ReturnsCorrectResourceName()
        {
            // Arrange
            var locationVariation = "chapel A"; // Mixed case

            // Act
            var resourceName = _resolver.ResolveOverlayResourceName(locationVariation);

            // Assert
            Assert.IsNotNull(resourceName);
            Assert.IsTrue(resourceName!.Contains("chapel_a.png"), $"Expected case-insensitive match for chapel A, got: {resourceName}");
        }

        [TestMethod]
        public void ResolveOverlayResourceName_WithWhitespaceVariation_ReturnsCorrectResourceName()
        {
            // Arrange
            var locationWithWhitespace = "  Chapel A  "; // Extra spaces

            // Act
            var resourceName = _resolver.ResolveOverlayResourceName(locationWithWhitespace);

            // Assert
            Assert.IsNotNull(resourceName);
            Assert.IsTrue(resourceName!.Contains("chapel_a.png"), $"Expected whitespace-normalized match, got: {resourceName}");
        }

        [TestMethod]
        public void ResolveOverlayResourceName_WithUnknownLocation_ReturnsNull()
        {
            // Arrange
            var unknownLocation = "Unknown Room That Doesnt Exist";

            // Act
            var resourceName = _resolver.ResolveOverlayResourceName(unknownLocation);

            // Assert
            Assert.IsNull(resourceName);
        }

        [TestMethod]
        public void ResolveOverlayResourceName_WithNullLocation_ReturnsNull()
        {
            // Arrange
            string? nullLocation = null;

            // Act
            var resourceName = _resolver.ResolveOverlayResourceName(nullLocation!);

            // Assert
            Assert.IsNull(resourceName);
        }

        [TestMethod]
        public void ResolveOverlayResourceName_WithEmptyString_ReturnsNull()
        {
            // Arrange
            var emptyLocation = string.Empty;

            // Act
            var resourceName = _resolver.ResolveOverlayResourceName(emptyLocation);

            // Assert
            Assert.IsNull(resourceName);
        }

        [TestMethod]
        public void ResolveOverlayResourceName_WithWhitespaceOnlyString_ReturnsNull()
        {
            // Arrange
            var whitespaceLocation = "   ";

            // Act
            var resourceName = _resolver.ResolveOverlayResourceName(whitespaceLocation);

            // Assert
            Assert.IsNull(resourceName);
        }

        [TestMethod]
        public void ResolveOverlayResourceName_MultipleLocations_EachResolvesCorrectly()
        {
            // Arrange
            var locations = new[] { "Chapel A", "Dining Room", "Library" };

            // Act
            var results = locations.Select(loc => _resolver.ResolveOverlayResourceName(loc)).ToList();

            // Assert
            Assert.IsTrue(results.All(r => r != null), "All known locations should resolve to non-null resource names");
            var result0 = results[0] !;
            var result1 = results[1] !;
            var result2 = results[2] !;
            Assert.IsTrue(result0.Contains("chapel_a"), "Chapel A should map to chapel_a");
            Assert.IsTrue(result1.Contains("dining_room"), "Dining Room should map to dining_room");
            Assert.IsTrue(result2.Contains("library"), "Library should map to library");
        }

        [TestMethod]
        public void ResolveOverlayResourceName_ReturnedResourceNameIsEmbedded()
        {
            // Arrange
            var location = "Chapel A";
            var assembly = System.Reflection.Assembly.Load("WinterAdventurer.Library");

            // Act
            var resourceName = _resolver.ResolveOverlayResourceName(location);

            // Assert
            Assert.IsNotNull(resourceName);
            var stream = assembly.GetManifestResourceStream(resourceName!);
            Assert.IsNotNull(stream, $"Resource {resourceName} should be embedded in assembly");
            Assert.IsTrue(stream!.Length > 0, $"Resource {resourceName} stream should not be empty");
        }
    }
}
