namespace NServiceBus
{
    using System;
    using NServiceBus.Unicast;
    using Pipeline;
    using Transports;

    class ForwardBehavior : PhysicalMessageProcessingStageBehavior
    {
        public IAuditMessages MessageAuditer { get; set; }

        public Address ForwardReceivedMessagesTo { get; set; }

        public TimeSpan? TimeToBeReceivedOnForwardedMessages { get; set; }

        public override void Invoke(Context context, Action next)
        {
            next();

            MessageAuditer.Audit(new SendOptions(ForwardReceivedMessagesTo)
            {
                TimeToBeReceived = TimeToBeReceivedOnForwardedMessages
            }, context.PhysicalMessage);

        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("ForwardMessageTo", typeof(ForwardBehavior), "Forwards message to the specified queue in the UnicastBus config section.")
            {
                InsertBefore(WellKnownStep.ExecuteUnitOfWork);
            }
        }
    }
}