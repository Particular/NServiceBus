namespace NServiceBus.Timeout.Hosting.Windows
{
    using Core;
    using ObjectBuilder;
    using Unicast.Queuing;
    using Unicast.Queuing.Msmq;
    using Unicast.Transport;
    using Unicast.Transport.Transactional;

    public class TimeoutDispatcherProcessor : IWantToRunWhenBusStartsAndStops
    {
        public TransactionalTransport InputTransport { get; set; }

        public static readonly Address TimeoutDispatcherAddress;

        public ISendMessages MessageSender { get; set; }

        public IPersistTimeouts TimeoutsPersister { get; set; }
        public TimeoutPersisterReceiver TimeoutPersisterReceiver { get; set; }
      
        static TimeoutDispatcherProcessor()
        {
            TimeoutDispatcherAddress = Address.Parse(Configure.EndpointName).SubScope("TimeoutsDispatcher");
        }

        public void Start()
        {
            TimeoutPersisterReceiver.Start();


            //todo - the line below needs to change when we refactore the slr to be:
            // transport.DisableSLR() or similar
            InputTransport.FailureManager = new ManageMessageFailuresWithoutSlr(InputTransport.FailureManager);

            InputTransport.TransportMessageReceived += OnTransportMessageReceived;

            InputTransport.Start(TimeoutDispatcherAddress);
        }

        private void OnTransportMessageReceived(object sender, TransportMessageReceivedEventArgs e)
        {
            var transportMessage = e.Message;
            var timeoutId = transportMessage.Headers["Timeout.Id"];
            TimeoutData timeoutData;

            if (TimeoutsPersister.TryRemove(timeoutId, out timeoutData))
            {
                MessageSender.Send(timeoutData.ToTransportMessage(), timeoutData.Destination);
            }
        }

        public void Stop()
        {
            TimeoutPersisterReceiver.Stop();

            if (InputTransport != null)
            {
                InputTransport.Dispose();
            }
        }
    }
}
