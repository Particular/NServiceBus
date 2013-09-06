namespace NServiceBus.Setup.Windows.PerformanceCounters
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;

    public class PerformanceCounterSetup
    {
        const string categoryName = "NServiceBus";

        public static bool CheckCounters()
        {
            if (PerformanceCounterCategory.Exists(categoryName))
            {
                var needToRecreateCategory = false;

                foreach (CounterCreationData counter in Counters)
                {
                    if (!PerformanceCounterCategory.CounterExists(counter.CounterName, categoryName))
                    {
                        needToRecreateCategory = true;
                    }

                }

                if (!needToRecreateCategory)
                {
                    return true;
                }
            }

            return false;
        }

        public static void SetupCounters()
        {
            try
            {
                PerformanceCounterCategory.Delete(categoryName);
            }
            catch (Win32Exception)
            {
                //Making sure this won't stop the process.
            }
            catch (Exception)
            {
                //Ignore exception.
                //We need to ensure that we attempt to delete category before recreating it. 
            }

            PerformanceCounterCategory.Create(categoryName, "NServiceBus statistics",
                                              PerformanceCounterCategoryType.MultiInstance, Counters);
            PerformanceCounter.CloseSharedResources(); // http://blog.dezfowler.com/2007/08/net-performance-counter-problems.html
        }

        static readonly CounterCreationDataCollection Counters = new CounterCreationDataCollection
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