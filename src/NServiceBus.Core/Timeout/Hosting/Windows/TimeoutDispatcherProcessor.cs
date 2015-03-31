namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using NServiceBus.Satellites;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

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
      
        public string InputAddress { get; set; }

        public bool Disabled { get; set; }

        public bool Handle(TransportMessage message)
        {
            var timeoutId = message.Headers["Timeout.Id"];
            TimeoutData timeoutData;

            if (TimeoutsPersister.TryRemove(timeoutId, out timeoutData))
            {
                var sendOptions = new TransportSendOptions(timeoutData.Destination);

                timeoutData.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
                timeoutData.Headers["NServiceBus.RelatedToTimeoutId"] = timeoutData.Id;


                MessageSender.Send(new OutgoingMessage(message.Id,timeoutData.Headers, timeoutData.State), sendOptions);
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
            };
        }
    }
}
