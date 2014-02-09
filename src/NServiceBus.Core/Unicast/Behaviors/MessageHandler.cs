namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;


    [Obsolete("This is a prototype API. May change in minor version releases.")]
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MessageHandler
    {
        public object Instance { get; set; }
        public Action<object, object> Invocation { get; set; }
    }
}