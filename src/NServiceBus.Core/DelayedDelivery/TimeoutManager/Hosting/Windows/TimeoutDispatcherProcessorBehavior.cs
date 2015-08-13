namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.ConsistencyGuarantees;
    using NServiceBus.DeliveryConstraints;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Timeout.Hosting.Windows;
    using NServiceBus.Transports;

    class TimeoutDispatcherProcessorBehavior : SatelliteBehavior
    {
        public IDispatchMessages MessageSender { get; set; }
        public IPersistTimeouts TimeoutsPersister { get; set; }
        public TimeoutPersisterReceiver TimeoutPersisterReceiver { get; set; }
        public Configure Configure { get; set; }
        public string InputAddress { get; set; }

        public override void Terminate(PhysicalMessageProcessingStageBehavior.Context context)
        {
            var message = context.GetPhysicalMessage();
            var timeoutId = message.Headers["Timeout.Id"];
            // TODO Daniel; Change when SatelliteTerminator PR is merged
            var options = new TimeoutPersistenceOptions(new ContextBag());
            TimeoutData timeoutData;

            if (TimeoutsPersister.TryRemove(timeoutId, options, out timeoutData))
            {
                var sendOptions = new DispatchOptions(timeoutData.Destination,new AtomicWithReceiveOperation(), new List<DeliveryConstraint>(), new ContextBag());

                timeoutData.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
                timeoutData.Headers["NServiceBus.RelatedToTimeoutId"] = timeoutData.Id;

                MessageSender.Dispatch(new OutgoingMessage(message.Id, timeoutData.Headers, timeoutData.State), sendOptions);
            }
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