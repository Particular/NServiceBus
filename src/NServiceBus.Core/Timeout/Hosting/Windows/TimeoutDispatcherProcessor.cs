namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Core;
    using Features;
    using NServiceBus.Pipeline;
    using Satellites;
    using Transports;
    using Unicast.Transport;

    public class TimeoutDispatcherProcessor : IAdvancedSatellite
    {  
        public ISendMessages MessageSender { get; set; }

        public IPersistTimeouts TimeoutsPersister { get; set; }
        
        public TimeoutPersisterReceiver TimeoutPersisterReceiver { get; set; }

        public PipelineExecutor PipelineExecutor { get; set; }
      
        public Address InputAddress
        {
            get
            {
                return TimeoutManager.DispatcherAddress;
            }
        }

        public bool Disabled
        {
            get { return !Feature.IsEnabled<TimeoutManager>(); }
        }

        public bool Handle(TransportMessage message)
        {
            var timeoutId = message.Headers["Timeout.Id"];
            

            var persisterV2 = TimeoutsPersister as IPersistTimeoutsV2;
            if (persisterV2 != null)
            {
                var timeoutData = persisterV2.Peek(timeoutId);
                if (timeoutData == null)
                {
                    return true;
                }

                try
                {
                    PipelineExecutor.CurrentContext.Set("do-not-enlist-in-native-transaction", true);
                    MessageSender.Send(timeoutData.ToTransportMessage(), timeoutData.Destination);
                }
                finally
                {
                    PipelineExecutor.CurrentContext.Set("do-not-enlist-in-native-transaction", false);
                }

                return persisterV2.TryRemove(timeoutId);
            }
            else
            {
                TimeoutData timeoutData;
                if (TimeoutsPersister.TryRemove(timeoutId, out timeoutData))
                {
                    MessageSender.Send(timeoutData.ToTransportMessage(), timeoutData.Destination);
                }
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

        public Action<TransportReceiver> GetReceiverCustomization()
        {
            return receiver =>
            {
                //TODO: The line below needs to change when we refactor the slr to be:
                // transport.DisableSLR() or similar
                receiver.FailureManager = new ManageMessageFailuresWithoutSlr(receiver.FailureManager);
            };
        }
    }
}
