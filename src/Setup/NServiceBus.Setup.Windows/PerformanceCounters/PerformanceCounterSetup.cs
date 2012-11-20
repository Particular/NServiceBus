namespace NServiceBus.Setup.Windows.PerformanceCounters
{
    using System;
    using System.Diagnostics;

    public class PerformanceCounterSetup
    {
        public static bool SetupCounters(bool allowModifications = false)
        {
            var categoryName = "NServiceBus";

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

                if (!allowModifications)
                    return false;

                Console.WriteLine("Category " + categoryName + " already exist, going to delete first");
                PerformanceCounterCategory.Delete(categoryName);
            }

            if (!allowModifications)
                return false;

            PerformanceCounterCategory.Create(categoryName, "NServiceBus statistics",
                                              PerformanceCounterCategoryType.MultiInstance, Counters);

            return true;
        }

        static CounterCreationDataCollection Counters = new CounterCreationDataCollection
                    {
                        new CounterCreationData("Critical Time", "Age of the oldest message in the queue",
                                                PerformanceCounterType.NumberOfItems32),
                        new CounterCreationData("SLA violation countdown",
                                                "Seconds until the SLA for this endpoint is breached",
                                                PerformanceCounterType.NumberOfItems32)
                    };
    }
}