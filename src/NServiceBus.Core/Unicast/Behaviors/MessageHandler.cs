namespace NServiceBus.Unicast.Behaviors
{
    using System;

    public class MessageHandler
    {
        public object Instance { get; set; }
        public Action<object, object> Invocation { get; set; }
    }
}