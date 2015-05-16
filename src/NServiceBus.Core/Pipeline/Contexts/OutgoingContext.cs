namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using NServiceBus.Extensibility;

    /// <summary>
    /// Outgoing pipeline context.
    /// </summary>
    public class OutgoingContext : BehaviorContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="OutgoingContext"/>.
        /// </summary>
        /// <param name="parentContext">The parent context.</param>
        /// <param name="messageType">The message type</param>
        /// <param name="messageInstance">The message instance</param>
        /// <param name="options">The options for the operation</param>
        public OutgoingContext(BehaviorContext parentContext,Type messageType, object messageInstance, ExtendableOptions options)
            : base(parentContext)
        {
            Guard.AgainstNull(messageType, "messageType");
            Guard.AgainstNull(messageInstance, "messageInstance");
            Guard.AgainstNull(options, "options");
         
            Set(options);

            MessageType = messageType;
            MessageInstance = messageInstance;
        }

        /// <summary>
        /// The message type for this message
        /// </summary>
        public Type MessageType { get; set; }

        /// <summary>
        /// The actual message instance
        /// </summary>
        public object MessageInstance { get; set; }
        /// <summary>
        /// Place for extensions to store their data
        /// </summary>
        public OptionExtensionContext Extensions { get { return Get<ExtendableOptions>().Extensions; } }
    }
}