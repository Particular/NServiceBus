namespace NServiceBus.Unicast.Transport.Transactional.Monitoring
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Logging;

    public class ThroughputPerformanceCounter
    {
        public ThroughputPerformanceCounter(Address receiveAddress)
        {
            this.receiveAddress = receiveAddress;
        }

        public void Initialize()
        {
            if (!InstantiateCounter())
                return;

            timer = new Timer(UpdateCounter, null, 0, 1000);

            enabled = true;
        }

        public void MessageProcessed()
        {
            if (!enabled)
                return;

            Interlocked.Increment(ref numberOfMessagesProcessed);
        }

        void UpdateCounter(object state)
        {
            var currentThroughput = Interlocked.Exchange(ref numberOfMessagesProcessed, 0);

            counter.RawValue = currentThroughput;
        }


        bool InstantiateCounter()
        {
            try
            {
                counter = new PerformanceCounter(CategoryName, "Current Throughput", receiveAddress.Queue, false);

                //access the counter type to force a exception to be thrown if the counter doesn't exists
                var t = counter.CounterType;
            }
            catch (Exception)
            {
                Logger.WarnFormat("NServiceBus performance counter for CurrentThroughput not set up correctly, no statistics will be emitted for the {0} queue. Execute the Install-PerformanceCounters powershell command to create the counter", receiveAddress.Queue);
                return false;
            }
            Logger.InfoFormat("Throughput counter initialized for transport: {0}", receiveAddress);
            return true;
        }

        readonly Address receiveAddress;

        bool enabled;

        PerformanceCounter counter;

        int numberOfMessagesProcessed;
        Timer timer;

        static readonly ILog Logger = LogManager.GetLogger(typeof(TransactionalTransport));

        const string CategoryName = "NServiceBus";
    }

}