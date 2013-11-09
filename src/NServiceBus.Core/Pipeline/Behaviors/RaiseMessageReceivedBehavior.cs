namespace NServiceBus.Pipeline.Behaviors
{
    using System;
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