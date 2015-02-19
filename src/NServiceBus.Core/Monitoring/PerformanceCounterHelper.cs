namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using NServiceBus.Logging;

    static class PerformanceCounterHelper
    {
        static ILog logger = LogManager.GetLogger(typeof(PerformanceCounterHelper));

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

        static bool TryToInstantiatePerformanceCounter(string counterName, string instanceName, out PerformanceCounter counter, bool throwIfFails)
        {
            if (instanceName.Length > 128)
            {
                throw new Exception(string.Format("The endpoint name ('{0}') is too long (longer then {1}) to register as a performance counter instance name. Please reduce the endpoint name.", instanceName, (int)SByte.MaxValue));
            }

            var message = String.Format("NServiceBus performance counter for '{0}' is not set up correctly. To rectify this problem download the latest powershell commandlets from http://www.particular.net downloads page.", counterName);

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
            
            return true;
        }
    }
}