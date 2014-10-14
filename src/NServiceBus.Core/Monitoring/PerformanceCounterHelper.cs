namespace NServiceBus
{
    using System;
    using System.Diagnostics;

    static class PerformanceCounterHelper
    {
        public static PerformanceCounter InstantiateCounter(string counterName, string endpointName)
        {
            if (endpointName.Length > SByte.MaxValue)
            {
                throw new Exception(string.Format("The endpoint name ('{0}') is too long (longer then {1}) to register as a performance counter instance name. Please reduce the endpoint name.", endpointName, (int)SByte.MaxValue));
            }

            try
            {
                var counter = new PerformanceCounter("NServiceBus", counterName, endpointName, false);
                //access the counter type to force a exception to be thrown if the counter doesn't exists
                // ReSharper disable once UnusedVariable
                var t = counter.CounterType;
                return counter;
            }
            catch (Exception e)
            {
                var message = string.Format("NServiceBus performance counter for {0} is not set up correctly. Please run Install-NServiceBusPerformanceCounters cmdlet to rectify this problem.", counterName);
                throw new InvalidOperationException(message, e);
            }
        }

    }
}