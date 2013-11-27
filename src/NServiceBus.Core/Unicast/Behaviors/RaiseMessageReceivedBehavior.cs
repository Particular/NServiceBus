namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class RaiseMessageReceivedBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        UnicastBus unicastBus;

        internal RaiseMessageReceivedBehavior(UnicastBus unicastBus)
        {
            this.unicastBus = unicastBus;
        }

        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            unicastBus.OnMessageReceived(context.PhysicalMessage);
            next();
        }
    }
}