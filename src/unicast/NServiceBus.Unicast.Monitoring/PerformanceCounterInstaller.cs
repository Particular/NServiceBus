namespace NServiceBus.Unicast.Monitoring
{
    using System;
    using System.Diagnostics;
    using System.Security.Principal;
    using Installation;
    using Installation.Environments;

    /// <summary>
    /// Performs installation of the performance counters 
    /// </summary>
    public class PerformanceCounterInstaller : INeedToInstallInfrastructure<Windows>
    {
        public void Install(WindowsIdentity identity)
        {
            Console.WriteLine("Starting installation of PerformanceCounters ");

            var categoryName = "NServiceBus";

            if (PerformanceCounterCategory.Exists(categoryName))
            {
                Console.WriteLine("Category " + categoryName + " already exist, going to delete first");
                PerformanceCounterCategory.Delete(categoryName);
            }


            var data = new CounterCreationDataCollection();

            data.Add(new CounterCreationData("Critical Time", "Age of the oldest message in the queue",
                                             PerformanceCounterType.NumberOfItems32));

            data.Add(new CounterCreationData("SLA violation countdown", "Seconds until the SLA for this endpoint is breached",
                                            PerformanceCounterType.NumberOfItems32));

            PerformanceCounterCategory.Create(categoryName, "NServiceBus statistics",
                                              PerformanceCounterCategoryType.MultiInstance, data);

            Console.WriteLine("Installation of PerformanceCounters successful.");
        }
    }
}