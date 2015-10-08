namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Core;
    using NServiceBus.Settings;
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

        public ReadOnlySettings Settings { get; set; }

        public bool Disabled { get; set; }

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

                var sendOptions = timeoutData.ToSendOptions(Configure.LocalAddress);
                
                if (ShouldSuppressTransaction())
                {
                    sendOptions.EnlistInReceiveTransaction = false;
                }

                MessageSender.Send(timeoutData.ToTransportMessage(), sendOptions);

                return persisterV2.TryRemove(timeoutId);
            }
            else
            {
                TimeoutData timeoutData;
                if (TimeoutsPersister.TryRemove(timeoutId, out timeoutData))
                {
                    MessageSender.Send(timeoutData.ToTransportMessage(), timeoutData.ToSendOptions(Configure.LocalAddress));
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

        bool ShouldSuppressTransaction()
        {
            var suppressDtc = Settings.Get<bool>("Transactions.SuppressDistributedTransactions");
            return !IsTransportSupportingDtc() || suppressDtc;
        }

        bool IsTransportSupportingDtc()
        {
            var selectedTransport = Settings.GetOrDefault<TransportDefinition>("NServiceBus.Transports.TransportDefinition");
            if (selectedTransport.HasSupportForDistributedTransactions.HasValue)
            {
                return selectedTransport.HasSupportForDistributedTransactions.Value;
            }

            return !selectedTransport.GetType().Name.Contains("RabbitMQ");
        }
    }
}
