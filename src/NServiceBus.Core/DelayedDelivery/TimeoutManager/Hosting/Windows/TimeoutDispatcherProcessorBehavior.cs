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
        readonly IDispatchMessages dispatchMessages;
        readonly IPersistTimeouts persistTimeouts;
        readonly TimeoutPersisterReceiver timeoutPersisterReceiver;

        public TimeoutDispatcherProcessorBehavior(IDispatchMessages dispatcher, IPersistTimeouts persister, TimeoutPersisterReceiver receiver)
        {
            timeoutPersisterReceiver = receiver;
            persistTimeouts = persister;
            dispatchMessages = dispatcher;
        }

        // ReSharper disable once UnusedAutoPropertyAccessor.Global
        public string InputAddress { get; set; }

        public override void Terminate(PhysicalMessageProcessingStageBehavior.Context context)
        {
            var message = context.GetPhysicalMessage();
            var timeoutId = message.Headers["Timeout.Id"];
            var options = new TimeoutPersistenceOptions(context);
            TimeoutData timeoutData;
            if (!persistTimeouts.TryRemove(timeoutId, options, out timeoutData))
            {
                return;
            }

            var sendOptions = new DispatchOptions(timeoutData.Destination, new AtomicWithReceiveOperation(), new List<DeliveryConstraint>(), new ContextBag());

            timeoutData.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
            timeoutData.Headers["NServiceBus.RelatedToTimeoutId"] = timeoutData.Id;

            dispatchMessages.Dispatch(new OutgoingMessage(message.Id, timeoutData.Headers, timeoutData.State), sendOptions);
        }

        public override Task Warmup()
        {
            timeoutPersisterReceiver.Start();
            return base.Warmup();
        }

        public override Task Cooldown()
        {
            timeoutPersisterReceiver.Stop();
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