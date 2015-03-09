namespace NServiceBus.Unicast.Behaviors
{
    using System;

    /// <summary>
    /// Represents a message handler and its invocation
    /// </summary>
    public class MessageHandler
    {
        /// <summary>
        /// Creates a new instance of the message handler
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "MessageHandler(Action<object, object, object> invocationWithContext, Type handlerType)")]
        // Daniel: Ask Simon Cropp why this doesn't work
        public MessageHandler()
        {
            throw new NotImplementedException("Creator of the message handler must assign the handler type and the invocation delegate");
        }

        /// <summary>
        /// Creates a new instance of the message handler with predefined invocation delegate and handler type
        /// </summary>
        /// <param name="invocationWithContext">The invocation with context delegate</param>
        /// <param name="handlerType">The handler type</param>
        // Daniel: Should we expose that to public?
        public MessageHandler(Action<object, object, object> invocationWithContext, Type handlerType)
        {
            HandlerType = handlerType;
            InvocationWithContext = invocationWithContext;
        }

        /// <summary>
        /// The actual instance, can be a saga, a timeout or just a plain handler
        /// </summary>
        public object Instance { get; set; }

        /// <summary>
        /// The handler type, can be a saga, a timeout or just a plain handler
        /// </summary>
        public Type HandlerType { get; set; }

        /// <summary>
        /// The actual invocation
        /// </summary>
        [ObsoleteEx(TreatAsErrorFromVersion = "6", RemoveInVersion = "7", ReplacementTypeOrMember = "MessageHandler.InvocationWithContext")]
        public Action<object, object> Invocation
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        /// <summary>
        /// The invocation delegate including the context
        /// </summary>
        public Action<object, object, object> InvocationWithContext { get; set; }
    }
}