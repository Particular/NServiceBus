namespace NServiceBus.Pipeline.Contexts
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;
    using NServiceBus.Unicast;

    /// <summary>
    /// Outgoing pipeline context.
    /// </summary>
    public class OutgoingContext : BehaviorContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="OutgoingContext"/>.
        /// </summary>
        /// <param name="parentContext">The parent context.</param>
        /// <param name="deliveryMessageOptions">The delivery options.</param>
        /// <param name="headers">The headers fór the message</param>
        /// <param name="messageId">The id of the message</param>
        /// <param name="intent">The intent of the message</param>
        /// <param name="messageType">The message type</param>
        /// <param name="messageInstance">The message instance</param>
        /// <param name="extensionContext">Extension data provided via options</param>
        public OutgoingContext(BehaviorContext parentContext, DeliveryMessageOptions deliveryMessageOptions, Dictionary<string, string> headers, string messageId, MessageIntentEnum intent, Type messageType, object messageInstance, ExtensionContext extensionContext)
            : base(parentContext)
        {
            DeliveryMessageOptions = deliveryMessageOptions;
            Headers = headers;
            MessageId = messageId;
            Intent = intent;
            MessageType = messageType;
            MessageInstance = messageInstance;
            Extensions = extensionContext;
        }

        /// <summary>
        /// Sending options.
        /// </summary>
        public DeliveryMessageOptions DeliveryMessageOptions { get; private set; }

        /// <summary>
        ///     Gets other applicative out-of-band information.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// This id of this message
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        /// The intent of the message
        /// </summary>
        public MessageIntentEnum Intent { get; private set; }

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
        public ExtensionContext Extensions { get; private set; }
    }
}