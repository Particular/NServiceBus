namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    class RaiseMessageReceivedBehavior : IBehavior<ReceivePhysicalMessageContext>
    {
        public UnicastBus UnicastBus { get; set; }
        
        public void Invoke(ReceivePhysicalMessageContext context, Action next)
        {
            UnicastBus.OnMessageReceived(context.PhysicalMessage);
            next();
        }
    }
}