namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    class RaiseMessageReceivedBehavior : IBehavior<IncomingPhysicalMessageContext>
    {
        public UnicastBus UnicastBus { get; set; }
        
        public void Invoke(IncomingPhysicalMessageContext context, Action next)
        {
            UnicastBus.OnMessageReceived(context.PhysicalMessage);
            next();
        }
    }
}