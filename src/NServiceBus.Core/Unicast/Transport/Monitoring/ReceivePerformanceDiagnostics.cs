namespace NServiceBus.Unicast.Transport.Monitoring
{
    using System;
    using System.Diagnostics;
    using Logging;

    internal class ReceivePerformanceDiagnostics : IDisposable
    {
        const string CategoryName = "NServiceBus";
        static readonly ILog Logger = LogManager.GetLogger(typeof (ReceivePerformanceDiagnostics));
        readonly Address receiveAddress;
        bool disposed;
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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
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
            disposed = true;
        }

        ~ReceivePerformanceDiagnostics()
        {
            Dispose(false);
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
                counter = new PerformanceCounter(CategoryName, counterName, receiveAddress.Queue, false);
                var t = counter.CounterType; //access the counter type to force a exception to be thrown if the counter doesn't exists
            }
            catch (Exception)
            {
                Logger.InfoFormat(
                    "NServiceBus performance counter for {1} is not set up correctly, no statistics will be emitted for the {0} queue. Execute the Install-NServiceBusPerformanceCounters cmdlet to create the counter.",
                    receiveAddress.Queue, counterName);
                return false;
            }
            Logger.DebugFormat("'{0}' counter initialized for '{1}'", counterName, receiveAddress);
            return true;
        }
    }
}