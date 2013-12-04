namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;
    using Unicast;

    class ForwardBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public ISendMessages MessageSender { get; set; }

        public UnicastBus UnicastBus { get; set; }

        public Address ForwardReceivedMessagesTo { get; set; }

        public TimeSpan TimeToBeReceivedOnForwardedMessages { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            next();

            if (ForwardReceivedMessagesTo != null && ForwardReceivedMessagesTo != Address.Undefined)
            {
                MessageSender.ForwardMessage(context.PhysicalMessage, TimeToBeReceivedOnForwardedMessages, ForwardReceivedMessagesTo);
            }
            //To cope with people hacking UnicastBus.ForwardReceivedMessagesTo at runtime. will be removed when we remove UnicastBus.ForwardReceivedMessagesTo
            if (UnicastBus.ForwardReceivedMessagesTo != ForwardReceivedMessagesTo)
            {
                if (UnicastBus.ForwardReceivedMessagesTo != null && UnicastBus.ForwardReceivedMessagesTo != Address.Undefined)
                {
                    MessageSender.ForwardMessage(context.PhysicalMessage, UnicastBus.TimeToBeReceivedOnForwardedMessages, UnicastBus.ForwardReceivedMessagesTo);
                }
            }
        }
    }
}