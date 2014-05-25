namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using Core;
    using Features;
    using Satellites;
    using Transports;
    using Unicast.Transport;

    class TimeoutDispatcherProcessor : IAdvancedSatellite
    {
        Configure configure;
        ISendMessages messageSender;
        IPersistTimeouts timeoutsPersister;
        TimeoutPersisterReceiver timeoutPersisterReceiver;

        public TimeoutDispatcherProcessor(Configure configure, ISendMessages messageSender, IPersistTimeouts timeoutsPersister, TimeoutPersisterReceiver timeoutPersisterReceiver)
        {
            this.configure = configure;
            this.messageSender = messageSender;
            this.timeoutsPersister = timeoutsPersister;
            this.timeoutPersisterReceiver = timeoutPersisterReceiver;
        }

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
            TimeoutData timeoutData;

            if (timeoutsPersister.TryRemove(timeoutId, out timeoutData))
            {
                messageSender.Send(timeoutData.ToTransportMessage(), timeoutData.ToSendOptions());
            }

            return true;
        }

        public void Start()
        {
            timeoutPersisterReceiver.Start();
        }

        public void Stop()
        {
            timeoutPersisterReceiver.Stop();
        }

        public Action<TransportReceiver> GetReceiverCustomization()
        {
            return receiver =>
            {
                //TODO: The line below needs to change when we refactor the slr to be:
                // transport.DisableSLR() or similar
                receiver.FailureManager = new ManageMessageFailuresWithoutSlr(receiver.FailureManager, configure);
            };
        }
    }
}
