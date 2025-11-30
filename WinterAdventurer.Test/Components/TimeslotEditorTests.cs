using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MudBlazor.Services;
using BunitTestContext = Bunit.TestContext;

namespace WinterAdventurer.Test.Components
{
    /// <summary>
    /// Tests for timeslot editor component.
    /// Note: This is a placeholder test file. The timeslot editor is part of Home.razor
    /// and would require more complex setup to test in isolation.
    /// For timeslot validation testing, see TimeslotValidationServiceTests.
    /// </summary>
    [TestClass]
    public class TimeslotEditorTests : BunitTestContext
    {
        [TestInitialize]
        public void Setup()
        {
            Services.AddMudServices();
        }

        [TestMethod]
        [Ignore("Timeslot editor is tested via TimeslotValidationServiceTests. Component testing requires Home.razor refactoring.")]
        public void TimeslotEditor_OverlappingTimes_ShowsValidationError()
        {
            // This test is marked as ignored because the timeslot editor functionality
            // is integrated into Home.razor and testing it in isolation would require
            // extracting it into a separate component.
            //
            // Timeslot validation is comprehensively tested in:
            // - WinterAdventurer.Test/TimeslotValidationServiceTests.cs
            // - WinterAdventurer.Test/HomeValidationIntegrationTests.cs
            Assert.Inconclusive("Timeslot validation is tested via service tests");
        }
    }
}
