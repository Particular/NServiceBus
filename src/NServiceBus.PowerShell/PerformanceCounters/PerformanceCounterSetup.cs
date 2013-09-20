namespace NServiceBus.Setup.Windows.PerformanceCounters
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public class PerformanceCounterSetup
    {
        const string categoryName = "NServiceBus";

        public static bool DoAllCountersExist()
        {
            return 
                PerformanceCounterCategory.Exists(categoryName) && 
                Counters.All(counter => PerformanceCounterCategory.CounterExists(counter.CounterName, categoryName));
        }
        public static bool DoesCategoryExist()
        {
            return PerformanceCounterCategory.Exists(categoryName);
        }

        public static void DeleteCategory()
        {
            PerformanceCounterCategory.Delete(categoryName);
        }

        public static void SetupCounters()
        {
            var counterCreationCollection = new CounterCreationDataCollection(Counters.ToArray());
            PerformanceCounterCategory.Create(categoryName, "NServiceBus statistics", PerformanceCounterCategoryType.MultiInstance, counterCreationCollection);
            PerformanceCounter.CloseSharedResources(); // http://blog.dezfowler.com/2007/08/net-performance-counter-problems.html
        }

       internal static List<CounterCreationData> Counters = new List<CounterCreationData>
                    {
                        new CounterCreationData("Critical Time", 
                                                "Age of the oldest message in the queue.",
                                                PerformanceCounterType.NumberOfItems32),
                        new CounterCreationData("SLA violation countdown",
                                                "Seconds until the SLA for this endpoint is breached.",
                                                PerformanceCounterType.NumberOfItems32),
                        new CounterCreationData("# of msgs successfully processed / sec",
                                                "The current number of messages processed successfully by the transport per second.",
                                                PerformanceCounterType.RateOfCountsPerSecond32),
                        new CounterCreationData("# of msgs pulled from the input queue /sec",
                                                "The current number of messages pulled from the input queue by the transport per second.",
                                                PerformanceCounterType.RateOfCountsPerSecond32),
                        new CounterCreationData("# of msgs failures / sec",
                                                "The current number of failed processed messages by the transport per second.",
                                                PerformanceCounterType.RateOfCountsPerSecond32)
                    };
    }
}