namespace NServiceBus.Unicast.Transport.Monitoring
{
    using System;
    using System.Diagnostics;
    using NServiceBus.Logging;

    class ReceivePerformanceDiagnostics
    {
        public ReceivePerformanceDiagnostics(Address receiveAddress)
        {
            this.receiveAddress = receiveAddress;
        }

        public void Initialize()
        {
            if (!InstantiateCounter())
                return;

            enabled = true;
        }

        public void MessageProcessed()
        {
            if (!enabled)
                return;

            successRateCounter.Increment();
        }

        public void MessageFailed()
        {
            if (!enabled)
                return;

            failureRateCounter.Increment();
        }

        public void MessageDequeued()
        {
            if (!enabled)
                return;

            throughputCounter.Increment();
        }


        bool InstantiateCounter()
        {
            return SetupCounter("# of msgs successfully processed / sec", ref successRateCounter)
                && SetupCounter("# of msgs pulled from the input queue /sec", ref throughputCounter)
                && SetupCounter("# of msgs failures / sec", ref failureRateCounter);
        }

        bool SetupCounter(string counterName,ref PerformanceCounter counter)
        {
            try
            {
                counter = new PerformanceCounter(CategoryName, counterName, receiveAddress.Name, false);

                //access the counter type to force a exception to be thrown if the counter doesn't exists
                var t = successRateCounter.CounterType;
            }
            catch (Exception)
            {
                Logger.InfoFormat(
                    "NServiceBus performance counter for {1} not set up correctly, no statistics will be emitted for the {0} queue. Execute the Install-PerformanceCounters powershell command to create the counter",
                    receiveAddress.Name, counterName);
                return false;
            }
            Logger.DebugFormat("Throughput counter initialized for transport: {0}", receiveAddress);
            return true;
        }

        readonly Address receiveAddress;

        bool enabled;

        PerformanceCounter successRateCounter;
        PerformanceCounter throughputCounter;
        PerformanceCounter failureRateCounter;

        static readonly ILog Logger = LogManager.GetLogger(typeof(ReceivePerformanceDiagnostics));

        const string CategoryName = "NServiceBus";
    }

}