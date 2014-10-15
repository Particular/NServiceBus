namespace NServiceBus.Unicast.Transport.Monitoring
{
    using System;
    using System.Diagnostics;
    using Logging;

    class ReceivePerformanceDiagnostics : IDisposable
    {
        static ILog Logger = LogManager.GetLogger<ReceivePerformanceDiagnostics>();
        readonly Address receiveAddress;
        bool enabled;
        PerformanceCounter failureRateCounter;
        PerformanceCounter successRateCounter;
        PerformanceCounter throughputCounter;

        public ReceivePerformanceDiagnostics(Address receiveAddress)
        {
            this.receiveAddress = receiveAddress;
        }

        public void Dispose()
        {
            //Injected at compile time
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
            return SetupCounter("# of msgs successfully processed / sec", out successRateCounter)
                   && SetupCounter("# of msgs pulled from the input queue /sec", out throughputCounter)
                   && SetupCounter("# of msgs failures / sec", out failureRateCounter);
        }

        bool SetupCounter(string counterName, out PerformanceCounter counter)
        {
                if (!PerformanceCounterHelper.TryToInstantiatePerformanceCounter(counterName, receiveAddress.Queue, out counter))
                {
                    return false;
                }

                Logger.DebugFormat("'{0}' counter initialized for '{1}'", counterName, receiveAddress);

                return true;
        }
    }
}
