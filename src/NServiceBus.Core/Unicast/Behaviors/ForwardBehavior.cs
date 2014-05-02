namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;
    using Transports;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class ForwardBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public ISendMessages MessageSender { get; set; }

        public Address ForwardReceivedMessagesTo { get; set; }

        public TimeSpan TimeToBeReceivedOnForwardedMessages { get; set; }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            next();

            if (ForwardReceivedMessagesTo != null && ForwardReceivedMessagesTo != Address.Undefined)
            {
                MessageSender.ForwardMessage(context.PhysicalMessage, TimeToBeReceivedOnForwardedMessages, ForwardReceivedMessagesTo);
            }
        }
    }
}