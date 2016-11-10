namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using Logging;

    static class PerformanceCounterHelper
    {
        public static PerformanceCounter InstantiatePerformanceCounter(string counterName, string instanceName)
        {
            PerformanceCounter counter;

            TryToInstantiatePerformanceCounter(counterName, instanceName, out counter, true);

            return counter;
        }

        public static IPerformanceCounterInstance TryToInstantiatePerformanceCounter(string counterName, string instanceName)
        {
            PerformanceCounter counter;
            var success = TryToInstantiatePerformanceCounter(counterName, instanceName, out counter, false);
            if (success)
            {
                return new PerformanceCounterInstance(counter);
            }
            return new NonFunctionalPerformanceCounterInstance();
        }

        static bool TryToInstantiatePerformanceCounter(string counterName, string instanceName, out PerformanceCounter counter, bool throwIfFails)
        {
            if (instanceName.Length > 128)
            {
                throw new Exception($"The endpoint name ('{instanceName}') is too long (longer then {(int) sbyte.MaxValue}) to register as a performance counter instance name. Reduce the endpoint name.");
            }

            var message = $"NServiceBus performance counter for '{counterName}' is not set up correctly. To rectify this problem, consult the NServiceBus performance counters documentation.";

            try
            {
                counter = new PerformanceCounter("NServiceBus", counterName, instanceName, false);
            }
            catch (Exception ex)
            {
                if (throwIfFails)
                {
                    throw new InvalidOperationException(message, ex);
                }

                logger.Info(message);
                counter = null;

                return false;
            }
            logger.DebugFormat("'{0}' counter initialized for '{1}'", counterName, instanceName);
            return true;
        }

        static ILog logger = LogManager.GetLogger(typeof(PerformanceCounterHelper));
    }
}