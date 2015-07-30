namespace NServiceBus.OutgoingPipeline
{
    using System;
    using NServiceBus.Pipeline.Contexts;

    /// <summary>
    /// Provides access to information about the outgoing logical message.
    /// </summary>
    public static class OutgoingLogicalMessageContextExtensions
    {
        /// <summary>
        /// The message type.
        /// </summary>
        public static Type GetMessageType(this OutgoingPublishContext context)
        {
            Guard.AgainstNull("context", context);

            return context.Get<OutgoingLogicalMessage>().MessageType;
        }

        /// <summary>
        /// The message instance.
        /// </summary>
        public static object GetMessageInstance(this OutgoingPublishContext context)
        {
            Guard.AgainstNull("context", context);
            
            return context.Get<OutgoingLogicalMessage>().Instance;
        }

        /// <summary>
        /// The message type.
        /// </summary>
        public static Type GetMessageType(this OutgoingSendContext context)
        {
            Guard.AgainstNull("context", context);
            
            return context.Get<OutgoingLogicalMessage>().MessageType;
        }

        /// <summary>
        /// The message instance.
        /// </summary>
        public static object GetMessageInstance(this OutgoingSendContext context)
        {
            Guard.AgainstNull("context", context);
            
            return context.Get<OutgoingLogicalMessage>().Instance;
        }

        /// <summary>
        /// The message type.
        /// </summary>
        public static Type GetMessageType(this OutgoingReplyContext context)
        {
            Guard.AgainstNull("context", context);

            return context.Get<OutgoingLogicalMessage>().MessageType;
        }

        /// <summary>
        /// The message instance.
        /// </summary>
        public static object GetMessageInstance(this OutgoingReplyContext context)
        {
            Guard.AgainstNull("context", context);

            return context.Get<OutgoingLogicalMessage>().Instance;
        }

        /// <summary>
        /// The message type.
        /// </summary>
        public static Type GetMessageType(this OutgoingContext context)
        {
            Guard.AgainstNull("context", context);

            return context.Get<OutgoingLogicalMessage>().MessageType;
        }

        /// <summary>
        /// The message instance.
        /// </summary>
        public static object GetMessageInstance(this OutgoingContext context)
        {
            Guard.AgainstNull("context", context);

            return context.Get<OutgoingLogicalMessage>().Instance;
        }
        /// <summary>
        /// Updates the message instance.
        /// </summary>
        public static void UpdateMessageInstance(this OutgoingContext context,object newInstance)
        {
            Guard.AgainstNull("context", context);
            Guard.AgainstNull("newInstance", newInstance);

            context.Set(new OutgoingLogicalMessage(newInstance));
        }
        
    }
}