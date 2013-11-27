namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Not for public consumption. May change in minor version releases.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class MessageHandler
    {
        public object Instance { get; set; }
        public Action<object, object> Invocation { get; set; }
    }
}