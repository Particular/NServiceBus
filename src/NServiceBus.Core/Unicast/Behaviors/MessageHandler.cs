namespace NServiceBus.Unicast.Behaviors
{
    using System;

    /// <summary>
    /// Represents a message handler and its invocation
    /// </summary>
    public class MessageHandler
    {
        Action<object, object, object> invocationWithContext;

        /// <summary>
        /// Creates a new instance of the message handler
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "MessageHandler(Action<object, object, object> invocationWithContext, Type handlerType)")]
        public MessageHandler()
        {
            throw new NotImplementedException("Creator of the message handler must assign the handler type and the invocation delegate");
        }

        /// <summary>
        /// Creates a new instance of the message handler with predefined invocation delegate and handler type
        /// </summary>
        /// <param name="invocationWithContext">The invocation with context delegate</param>
        /// <param name="handlerType">The handler type</param>
        /// <param name="handlerKind">The handler kind</param>
        // Daniel: Should we expose that to public?
        internal MessageHandler(Action<object, object, object> invocationWithContext, Type handlerType, HandlerKind handlerKind)
        {
            HandlerKind = handlerKind;
            HandlerType = handlerType;
            this.invocationWithContext = invocationWithContext;
        }

        /// <summary>
        /// The actual instance, can be a saga, a timeout or just a plain handler
        /// </summary>
        public object Instance { get; set; }

        /// <summary>
        /// The handler type, can be a saga, a timeout or just a plain handler
        /// </summary>
        public Type HandlerType { get; private set; }

        /// <summary>
        /// Daniel: Should we expose that to public?
        /// </summary>
        internal HandlerKind HandlerKind { get; private set; }

        /// <summary>
        /// The actual invocation
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "MessageHandler.Invoke")]
        public Action<object, object> Invocation
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="context"></param>
        public void Invoke(object message, object context)
        {
            invocationWithContext(Instance, message, context);
        }
    }

    /// <summary>
    /// Daniel: Should we expose that to public?
    /// </summary>
    [Flags]
    internal enum HandlerKind
    {
        None = 0,
        Message = 1,
        Event = 2,
        Timeout = 3
    }
}