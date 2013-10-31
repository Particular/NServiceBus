namespace NServiceBus.Pipeline.Behaviors
{
    using System;
    using Unicast;

    class RaiseMessageReceivedBehavior : IBehavior
    {
        public UnicastBus UnicastBus { get; set; }
        public void Invoke(BehaviorContext context, Action next)
        {
            UnicastBus.OnMessageReceived(context.TransportMessage);
            next();
        }
    }
}