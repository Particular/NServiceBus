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
        ISendMessages messageSender;

        UnicastBus unicastBus;

        public ForwardBehavior(UnicastBus unicastBus, ISendMessages messageSender)
        {
            this.unicastBus = unicastBus;
            this.messageSender = messageSender;
        }

        internal Address ForwardReceivedMessagesTo { get; set; }

        internal TimeSpan TimeToBeReceivedOnForwardedMessages { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            next();

            if (ForwardReceivedMessagesTo != null && ForwardReceivedMessagesTo != Address.Undefined)
            {
                messageSender.ForwardMessage(context.PhysicalMessage, TimeToBeReceivedOnForwardedMessages, ForwardReceivedMessagesTo);
            }
            //To cope with people hacking UnicastBus.ForwardReceivedMessagesTo at runtime. will be removed when we remove UnicastBus.ForwardReceivedMessagesTo
            if (unicastBus.ForwardReceivedMessagesTo != ForwardReceivedMessagesTo)
            {
                if (unicastBus.ForwardReceivedMessagesTo != null && unicastBus.ForwardReceivedMessagesTo != Address.Undefined)
                {
                    messageSender.ForwardMessage(context.PhysicalMessage, unicastBus.TimeToBeReceivedOnForwardedMessages, unicastBus.ForwardReceivedMessagesTo);
                }
            }
        }
    }
}