namespace NServiceBus.Routing
{
    using System;


    abstract class MessageRouter
    {
        public abstract bool TryGetRoute(Type messageType, out string destination);
    }
}