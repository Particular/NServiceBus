namespace NServiceBus.Unicast.Behaviors
{
    using System;

    /// <summary>
    /// Represents a message handler and its invocation.
    /// </summary>
    public class MessageHandler
    {
        /// <summary>
        /// The actual instance, can be a saga or just a plain handler.
        /// </summary>
        public object Instance { get; set; }
        
        /// <summary>
        /// The actual invocation.
        /// </summary>
        public Action<object, object> Invocation { get; set; }
    }
}