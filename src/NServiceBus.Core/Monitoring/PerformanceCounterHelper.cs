namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using NServiceBus.Logging;

    static class PerformanceCounterHelper
    {
        static ILog logger = LogManager.GetLogger("PerformanceCountersSetup");

        public static PerformanceCounter InstantiatePerformanceCounter(string counterName, string instanceName)
        {
            PerformanceCounter counter;
            
            TryToInstantiatePerformanceCounter(counterName, instanceName, out counter, true);
            
            return counter;
        }

        public static bool TryToInstantiatePerformanceCounter(string counterName, string instanceName, out PerformanceCounter counter)
        {
            return TryToInstantiatePerformanceCounter(counterName, instanceName, out counter, false);
        }

        static bool PerformanceCounterHealthy(PerformanceCounter counter)
        {
            // Fire this off on an separate thread
            var task = Task.Factory.StartNew(() =>
            {
                // Access the counter type to force a exception to be thrown if the counter doesn't exists
// ReSharper disable once UnusedVariable
                var _ = counter.CounterType;

            }).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    t.Exception.Handle(ex => true);

                    return false;
                }

                return true;
            });

            if (!task.Wait(TimeSpan.FromSeconds(2)))
            {
                // If it timed out then assume counter is screwed
                return false;
            }

            return task.Result;
        }

        static bool TryToInstantiatePerformanceCounter(string counterName, string instanceName, out PerformanceCounter counter, bool throwIfFails)
        {
            if (instanceName.Length > 128)
            {
                throw new Exception(string.Format("The endpoint name ('{0}') is too long (longer then {1}) to register as a performance counter instance name. Please reduce the endpoint name.", instanceName, (int)SByte.MaxValue));
            }

            var message = String.Format("NServiceBus performance counter for '{0}' is not set up correctly. Please run Install-NServiceBusPerformanceCounters cmdlet to rectify this problem.", counterName);

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

                logger.Warn(message);
                counter = null;

                return false;
            }
            
            if (!PerformanceCounterHealthy(counter))
            {
                if (throwIfFails)
                {
                    throw new InvalidOperationException(message);
                }

                logger.Warn(message);
                return false;
            }
           
            return true;
        }
    }
}