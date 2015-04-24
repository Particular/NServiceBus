namespace NServiceBus.Timeout.Hosting.Windows
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Transports;

    class TimeoutDispatcherProcessor : SatelliteBehavior
    {
        public ISendMessages MessageSender { get; set; }
        public IPersistTimeouts TimeoutsPersister { get; set; }
        public TimeoutPersisterReceiver TimeoutPersisterReceiver { get; set; }
        public Configure Configure { get; set; }
        public string InputAddress { get; set; }

        protected override bool Handle(TransportMessage message)
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

        public override void OnStarting()
        {
            TimeoutPersisterReceiver.Start();
        }

        public override void OnStopped()
        {
            TimeoutPersisterReceiver.Stop();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("TimeoutDispatcherProcessor", typeof(TimeoutDispatcherProcessor), "Dispatches timeout messages")
            {
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");
            }
        }
    }
}
