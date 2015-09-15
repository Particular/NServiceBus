namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Core;
    using Satellites;
    using Transports;
    using Unicast.Transport;

    class TimeoutDispatcherProcessor : IAdvancedSatellite
    {
        public TimeoutDispatcherProcessor()
        {
            Disabled = true;
        }

        public ISendMessages MessageSender { get; set; }

        public IPersistTimeouts TimeoutsPersister { get; set; }

        public TimeoutPersisterReceiver TimeoutPersisterReceiver { get; set; }

        public Configure Configure { get; set; }
      
        public Address InputAddress { get; set; }

        public bool Disabled { get; set; }

        public bool Handle(TransportMessage message)
        {
            var timeoutId = message.Headers["Timeout.Id"];
            TimeoutData timeoutData;

            if (TimeoutsPersister.TryRemove(timeoutId, out timeoutData))
            {
                try
                {
                    MessageSender.Send(timeoutData.ToTransportMessage(), timeoutData.ToSendOptions(Configure.LocalAddress));
                }
                catch
                {
                    timeoutData.Id = null;
                    timeoutData.Time = DateTime.UtcNow.AddSeconds(5);
                    TimeoutsPersister.Add(timeoutData);
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
                receiver.FailureManager = new ManageMessageFailuresWithoutSlr(receiver.FailureManager, MessageSender, Configure);
            };
        }
    }
}
