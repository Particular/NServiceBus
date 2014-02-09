namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class RaiseMessageReceivedBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public UnicastBus UnicastBus { get; set; }
        
        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            UnicastBus.OnMessageReceived(context.PhysicalMessage);
            next();
        }
    }
}