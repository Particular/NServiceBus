namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Routing;
    using NServiceBus.Timeout.Core;
    using NServiceBus.Transports;

    class DispatchTimeoutBehavior : SatelliteBehavior
    {
        public DispatchTimeoutBehavior(IDispatchMessages dispatcher, IPersistTimeouts persister)
        {
            this.dispatcher = dispatcher;
            this.persister = persister;
        }

        public override void Terminate(PhysicalMessageProcessingStageBehavior.Context context)
        {
            var message = context.GetPhysicalMessage();
            var timeoutId = message.Headers["Timeout.Id"];

            var timeoutData = persister.Remove(timeoutId, new TimeoutPersistenceOptions(context)).GetAwaiter().GetResult();

            if (timeoutData == null)
            {
                return;
            }

            var sendOptions = new DispatchOptions(new DirectToTargetDestination(timeoutData.Destination), context);

            timeoutData.Headers[Headers.TimeSent] = DateTimeExtensions.ToWireFormattedString(DateTime.UtcNow);
            timeoutData.Headers["NServiceBus.RelatedToTimeoutId"] = timeoutData.Id;

            dispatcher.Dispatch(new OutgoingMessage(message.Id, timeoutData.Headers, timeoutData.State), sendOptions).GetAwaiter().GetResult();
        }

        IDispatchMessages dispatcher;
        IPersistTimeouts persister;

        public class Registration : RegisterStep
        {
            public Registration()
                : base("TimeoutDispatcherProcessor", typeof(DispatchTimeoutBehavior), "Dispatches timeout messages")
            {
                InsertBeforeIfExists("FirstLevelRetries");
                InsertBeforeIfExists("ReceivePerformanceDiagnosticsBehavior");
            }
        }
    }
}