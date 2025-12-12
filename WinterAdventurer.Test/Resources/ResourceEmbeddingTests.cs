// <copyright file="ResourceEmbeddingTests.cs" company="ECRS">
// Copyright (c) ECRS.
// </copyright>

using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WinterAdventurer.Test.Resources
{
    [TestClass]
    public class ResourceEmbeddingTests
    {
        [TestMethod]
        public void WatsonLayoutBase_IsEmbedded()
        {
            var assembly = Assembly.Load("WinterAdventurer.Library");
            var resourceName = "WinterAdventurer.Library.Resources.Images.WatsonMaps.watson_layout.png";

            var stream = assembly.GetManifestResourceStream(resourceName);

            Assert.IsNotNull(stream, $"Resource not found: {resourceName}");
            Assert.IsTrue(stream.Length > 0, "Resource stream is empty");
        }

        [TestMethod]
        public void LocationMapConfiguration_IsEmbedded()
        {
            var assembly = Assembly.Load("WinterAdventurer.Library");
            var resourceName = "WinterAdventurer.Library.EventSchemas.LocationMapConfiguration.json";

            var stream = assembly.GetManifestResourceStream(resourceName);

            Assert.IsNotNull(stream, $"Resource not found: {resourceName}");
            Assert.IsTrue(stream.Length > 0, "Resource stream is empty");
        }

        [TestMethod]
        public void AllWatsonMapResources_AreEmbedded()
        {
            var assembly = Assembly.Load("WinterAdventurer.Library");
            var resources = assembly.GetManifestResourceNames();

            var watsonResources = resources
                .Where(r => r.Contains("WatsonMaps", StringComparison.OrdinalIgnoreCase))
                .ToList();

            Assert.IsTrue(watsonResources.Count > 0, "No Watson map resources found");

            foreach (var resource in watsonResources)
            {
                var stream = assembly.GetManifestResourceStream(resource);
                Assert.IsNotNull(stream, $"Watson resource stream is null: {resource}");
                Assert.IsTrue(stream.Length > 0, $"Watson resource stream is empty: {resource}");
            }
        }
    }
}
