namespace NServiceBus.Unicast.Transport.Transactional.Monitoring
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Logging;

    public class ReceivePerformanceDiagnostics
    {
        public ReceivePerformanceDiagnostics(Address receiveAddress)
        {
            this.receiveAddress = receiveAddress;
        }

        public void Initialize()
        {
            if (!InstantiateCounter())
                return;

            timer = new Timer(UpdateCounters, null, 0, 1000);

            enabled = true;
        }

        public void MessageProcessed()
        {
            if (!enabled)
                return;

            Interlocked.Increment(ref numberOfSuccessfullMessages);
        }

        public void MessageFailed()
        {
            if (!enabled)
                return;

            Interlocked.Increment(ref numberOfFailedMessages);
        }

        void UpdateCounters(object state)
        {
            var currentThroughput = Interlocked.Exchange(ref numberOfSuccessfullMessages, 0);

            currentThroughputCounter.RawValue = currentThroughput;

            var currentFailureRate = Interlocked.Exchange(ref numberOfFailedMessages, 0);

            failureRateCounter.RawValue = currentFailureRate;

            dequeueRateCounter.RawValue = currentThroughput + currentFailureRate;
        }


        bool InstantiateCounter()
        {
            return SetupCounter("Current Throughput",ref currentThroughputCounter)
                && SetupCounter("Dequeue rate", ref dequeueRateCounter)
                && SetupCounter("Failure rate", ref failureRateCounter);
        }

        bool SetupCounter(string counterName,ref PerformanceCounter counter)
        {
            try
            {
                counter = new PerformanceCounter(CategoryName, counterName, receiveAddress.Queue, false);

                //access the counter type to force a exception to be thrown if the counter doesn't exists
                var t = currentThroughputCounter.CounterType;
            }
            catch (Exception)
            {
                Logger.InfoFormat(
                    "NServiceBus performance counter for {1} not set up correctly, no statistics will be emitted for the {0} queue. Execute the Install-PerformanceCounters powershell command to create the counter",
                    receiveAddress.Queue, counterName);
                return false;
            }
            Logger.DebugFormat("Throughput counter initialized for transport: {0}", receiveAddress);
            return true;
        }

        readonly Address receiveAddress;

        bool enabled;

        PerformanceCounter currentThroughputCounter;
        PerformanceCounter dequeueRateCounter;
        PerformanceCounter failureRateCounter;

        int numberOfSuccessfullMessages;
        int numberOfFailedMessages;
        Timer timer;

        static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionalTransport));

        const string CategoryName = "NServiceBus";
    }

}