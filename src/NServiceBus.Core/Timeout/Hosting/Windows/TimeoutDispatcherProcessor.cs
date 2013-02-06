namespace NServiceBus.Timeout.Hosting.Windows
{
    using Core;
    using Satellites;
    using Unicast.Queuing;

    public class TimeoutDispatcherProcessor : ISatellite
    {  
        public static readonly Address TimeoutDispatcherAddress;

        public ISendMessages MessageSender { get; set; }

        public IPersistTimeouts TimeoutsPersister { get; set; }
        public TimeoutPersisterReceiver TimeoutPersisterReceiver { get; set; }
      
        static TimeoutDispatcherProcessor()
        {
            TimeoutDispatcherAddress = Address.Parse(Configure.EndpointName).SubScope("TimeoutsDispatcher");
        }

        public Address InputAddress { get { return TimeoutDispatcherAddress; } }

        public bool Disabled
        {
            get { return !TimeoutManager.Enabled; }
        }

        public bool Handle(TransportMessage message)
        {
            var timeoutId = message.Headers["Timeout.Id"];
            TimeoutData timeoutData;

            if (TimeoutsPersister.TryRemove(timeoutId, out timeoutData))
            {
                MessageSender.Send(timeoutData.ToTransportMessage(), timeoutData.Destination);
            }

            return true;
        }

        public void Start()
        {
            TimeoutPersisterReceiver.Start();
        }

        public void Stop()
        {
            TimeoutPersisterReceiver.Stop();
        }
    }
}
