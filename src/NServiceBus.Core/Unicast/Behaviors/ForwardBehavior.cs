namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;

    class ForwardBehavior : IBehavior<IncomingContext>
    {
        public ISendMessages MessageSender { get; set; }

        public Address ForwardReceivedMessagesTo { get; set; }

        public TimeSpan TimeToBeReceivedOnForwardedMessages { get; set; }

        public void Invoke(IncomingContext context, Action next)
        {
            next();

            if (ForwardReceivedMessagesTo != null && ForwardReceivedMessagesTo != Address.Undefined)
            {
                MessageSender.ForwardMessage(context.PhysicalMessage, TimeToBeReceivedOnForwardedMessages, ForwardReceivedMessagesTo);
            }
        }
    }
}