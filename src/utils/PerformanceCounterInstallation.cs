using System.Diagnostics;
using Common.Logging;

namespace NServiceBus.Utils
{
    /// <summary>
    /// Performs installation of nessesary categories and conters for NServiceBus
    /// </summary>
    public class PerformanceCounterInstallation
    {
        /// <summary>
        /// Starts the install
        /// </summary>
        public static void InstallCounters()
        {
            Logger.Debug("Starting installation of PerformanceCounters ");

            var categoryName = "NServiceBus";
            var counterName = "Critical Time";

            if (PerformanceCounterCategory.Exists(categoryName))
            {
                Logger.Warn("Category " + categoryName + " already exist, going to delete first");
                PerformanceCounterCategory.Delete(categoryName);
            }
                

            var data = new CounterCreationDataCollection();

            var c1 = new CounterCreationData(counterName, "Age of the oldest message in the queue",
                                             PerformanceCounterType.NumberOfItems32);
            data.Add(c1);

            PerformanceCounterCategory.Create(categoryName, "NServiceBus statistics",
                                              PerformanceCounterCategoryType.MultiInstance, data);

            Logger.Debug("Installation of PerformanceCounters successful.");
        }

        private static readonly ILog Logger = LogManager.GetLogger("NServiceBus.Utils");
    }
}