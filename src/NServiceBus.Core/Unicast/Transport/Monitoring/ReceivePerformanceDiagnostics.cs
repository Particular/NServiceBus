namespace NServiceBus.Unicast.Transport.Monitoring
{
    using System;
    using System.Diagnostics;
    using Logging;

    class ReceivePerformanceDiagnostics : IDisposable
    {
        const string CategoryName = "NServiceBus";
        static ILog Logger = LogManager.GetLogger<ReceivePerformanceDiagnostics>();
        string endpointName;
        bool enabled;
        PerformanceCounter failureRateCounter;
        PerformanceCounter successRateCounter;
        PerformanceCounter throughputCounter;

        public ReceivePerformanceDiagnostics(string endpointName)
        {
            this.endpointName = endpointName;
        }


        public void Dispose()
        {
            //Injected at compile time
        }

        void DisposeManaged()
        {
            if (successRateCounter != null)
            {
                successRateCounter.Dispose();
            }
            if (throughputCounter != null)
            {
                throughputCounter.Dispose();
            }
            if (failureRateCounter != null)
            {
                failureRateCounter.Dispose();
            }
        }


        public void Initialize()
        {
            if (!InstantiateCounter())
            {
                return;
            }

            enabled = true;
        }

        public void MessageProcessed()
        {
            if (!enabled)
            {
                return;
            }

            successRateCounter.Increment();
        }

        public void MessageFailed()
        {
            if (!enabled)
            {
                return;
            }

            failureRateCounter.Increment();
        }

        public void MessageDequeued()
        {
            if (!enabled)
            {
                return;
            }

            throughputCounter.Increment();
        }


        bool InstantiateCounter()
        {
            return SetupCounter("# of msgs successfully processed / sec", ref successRateCounter)
                   && SetupCounter("# of msgs pulled from the input queue /sec", ref throughputCounter)
                   && SetupCounter("# of msgs failures / sec", ref failureRateCounter);
        }

        bool SetupCounter(string counterName, ref PerformanceCounter counter)
        {
            try
            {
                counter = new PerformanceCounter(CategoryName, counterName, endpointName, false);
                //access the counter type to force a exception to be thrown if the counter doesn't exists
                // ReSharper disable once UnusedVariable
                var t = counter.CounterType;
            }
            catch (Exception)
            {
                Logger.InfoFormat(
                    "NServiceBus performance counter for {1} is not set up correctly, no statistics will be emitted for the {0} endpoint. Execute the Install-NServiceBusPerformanceCounters cmdlet to create the counter.",
                    endpointName, counterName);
                return false;
            }
            Logger.DebugFormat("'{0}' counter initialized for '{1}'", counterName, endpointName);
            return true;
        }
    }
}