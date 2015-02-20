namespace NServiceBus.Unicast.Transport.Monitoring
{
    using System;
    using System.Diagnostics;
    using Logging;

    class ReceivePerformanceDiagnostics : IDisposable
    {
        readonly string queueName;
        static ILog Logger = LogManager.GetLogger<ReceivePerformanceDiagnostics>();
        bool enabled;
        PerformanceCounter failureRateCounter;
        PerformanceCounter successRateCounter;
        PerformanceCounter throughputCounter;

        public ReceivePerformanceDiagnostics(string queueName)
        {
            this.queueName = queueName;
            if (!InstantiateCounter())
            {
                return;
            }

            enabled = true;
        }

        public void Dispose()
        {
            //Injected at compile time
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
                if (!PerformanceCounterHelper.TryToInstantiatePerformanceCounter(counterName, queueName, out counter))
                {
                    return false;
                }

                Logger.DebugFormat("'{0}' counter initialized for '{1}'", counterName, queueName);

                return true;
        }
    }
}
