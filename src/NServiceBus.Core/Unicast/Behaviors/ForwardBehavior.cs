namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;
    using Unicast;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ForwardBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public ISendMessages MessageSender { get; set; }

        public UnicastBus UnicastBus { get; set; }

        internal Address ForwardReceivedMessagesTo { get; set; }

        internal TimeSpan TimeToBeReceivedOnForwardedMessages { get; set; }

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