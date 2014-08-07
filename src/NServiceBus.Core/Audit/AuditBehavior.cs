namespace NServiceBus.Audit
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;
    using Unicast;


    class AuditBehavior : IBehavior<IncomingContext>
    {
        public IAuditMessages MessageAuditer { get; set; }

        public Address AuditQueue { get; set; }

        public TimeSpan? TimeToBeReceivedOnForwardedMessages { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            next();

            var sendOptions = new SendOptions(AuditQueue)
            {
                TimeToBeReceived = TimeToBeReceivedOnForwardedMessages
            };
            MessageAuditer.Audit(sendOptions, context.PhysicalMessage);
        }

        public class Registration:RegisterStep
        {
            public Registration()
                : base(WellKnownStep.AuditProcessedMessage, typeof(AuditBehavior), "Send a copy of the successfully processed message to the configured audit queue")
            {
                InsertBefore("ProcessingStatistics");
            }
        }
    }
}