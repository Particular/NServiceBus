namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;

    class ForwardBehavior : IBehavior<IncomingContext>
    {
        public IAuditMessages MessageAuditer { get; set; }

        public Address ForwardReceivedMessagesTo { get; set; }

        public TimeSpan? TimeToBeReceivedOnForwardedMessages { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            next();

            if (ForwardReceivedMessagesTo != null && ForwardReceivedMessagesTo != Address.Undefined)
            {
                MessageAuditer.Audit(new SendOptions(ForwardReceivedMessagesTo)
                {
                    TimeToBeReceived = TimeToBeReceivedOnForwardedMessages
                }, context.PhysicalMessage);
            }
        }
    }
}