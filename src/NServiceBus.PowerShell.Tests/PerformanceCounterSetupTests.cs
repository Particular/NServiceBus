namespace NServiceBus.PowerShell.Tests
{
    using System.Diagnostics;
    using NUnit.Framework;
    using Setup.Windows.PerformanceCounters;

    [TestFixture]
    public class PerformanceCounterSetupTests
    {

        [SetUp]
        public void Setup()
        {
            PerformanceCounter.CloseSharedResources();
            if (PerformanceCounterCategory.Exists("NServiceBus"))
            {
                PerformanceCounterSetup.DeleteCategory();
            }
        }

        [TearDown]
        public void TearDown()
        {
            PerformanceCounter.CloseSharedResources();
            if (PerformanceCounterCategory.Exists("NServiceBus"))
            {
                PerformanceCounterSetup.DeleteCategory();
            }
        }


        [Explicit]
        [Test]
        public void DoAllCountersExist_returns_false_when_category_missing()
        {
            Assert.IsFalse(PerformanceCounterSetup.DoAllCountersExist());
        }

        [Explicit]
        [Test]
        public void DoAllCountersExist_returns_true_when_all_exist()
        {
            PerformanceCounterSetup.SetupCounters();
            PerformanceCounter.CloseSharedResources();
            Assert.IsTrue(PerformanceCounterSetup.DoAllCountersExist());
        }

        [Explicit]
        [Test]
        public void DoAllCountersExist_returns_false_when_some_counters_are_missing_exist()
        {
            var counters = new CounterCreationDataCollection
                           {
                               new CounterCreationData("Critical Time","Age of the oldest message in the queue.",PerformanceCounterType.NumberOfItems32),
                           };
            PerformanceCounterCategory.Create("NServiceBus", "NServiceBus statistics", PerformanceCounterCategoryType.MultiInstance, counters);
            PerformanceCounter.CloseSharedResources();
            Assert.IsFalse(PerformanceCounterSetup.DoAllCountersExist());
        }

        [Explicit]
        [Test]
        public void CreateBadCounters()
        {
            var counters = new CounterCreationDataCollection
                           {
                               new CounterCreationData("Critical Time","Age of the oldest message in the queue.",PerformanceCounterType.NumberOfItems32),
                           };
            PerformanceCounterCategory.Create("NServiceBus", "NServiceBus statistics", PerformanceCounterCategoryType.MultiInstance, counters);
        }
    }
}
