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
                bool needToRecreateCategory = false;

                foreach (CounterCreationData counter in Counters)
                {
                    if (!PerformanceCounterCategory.CounterExists(counter.CounterName, categoryName))
                        needToRecreateCategory = true;

                }

                if (!needToRecreateCategory)
                    return true;
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
        }

        static readonly CounterCreationDataCollection Counters = new CounterCreationDataCollection
                    {
                        new CounterCreationData("Critical Time", "Age of the oldest message in the queue",
                                                PerformanceCounterType.NumberOfItems32),
                        new CounterCreationData("SLA violation countdown",
                                                "Seconds until the SLA for this endpoint is breached",
                                                PerformanceCounterType.NumberOfItems32),
                        new CounterCreationData("Current Throughput",
                                                "The current number of messages per second flowing through the transport",
                                                PerformanceCounterType.NumberOfItems32)
                    };
    }
}