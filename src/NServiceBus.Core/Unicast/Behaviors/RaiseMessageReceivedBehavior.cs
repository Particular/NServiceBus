namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;
    using Unicast;

    class RaiseMessageReceivedBehavior : IBehavior<PhysicalMessageContext>
    {
        public UnicastBus UnicastBus { get; set; }
        
        public void Invoke(PhysicalMessageContext context, Action next)
        {
            UnicastBus.OnMessageReceived(context.PhysicalMessage);
            next();
        }
    }
}