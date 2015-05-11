namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Timeout.Hosting.Windows;
    using NServiceBus.Transports;

    class TimeoutDispatcherProcessorBehavior : SatelliteBehavior
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

        public override Task Warmup()
        {
            TimeoutPersisterReceiver.Start();
            return base.Warmup();
        }

        public override Task Cooldown()
        {
            TimeoutPersisterReceiver.Stop();
            return base.Cooldown();
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("TimeoutDispatcherProcessor", typeof(TimeoutDispatcherProcessorBehavior), "Dispatches timeout messages")
            {
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");
            }
        }
    }
}
