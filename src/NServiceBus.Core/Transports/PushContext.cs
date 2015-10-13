namespace NServiceBus.Transports
{
    using System.Collections.Generic;
    using System.IO;
    using Extensibility;

    /// <summary>
    /// Allows the transport to pass relevant info to the pipeline.
    /// </summary>
    public class PushContext
    {
        /// <summary>
        /// Initializes the context.
        /// </summary>
        /// <param name="messageId">Native message id.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="bodyStream">The message body stream.</param>
        /// <param name="context">Any context that the transport wants to be available on the pipeline.</param>
        public PushContext(string messageId, Dictionary<string, string> headers, Stream bodyStream, ContextBag context)
        {
            Guard.AgainstNullAndEmpty("messageId", messageId);
            Guard.AgainstNull("bodyStream", bodyStream);
            Guard.AgainstNull("headers", headers);
            Guard.AgainstNull("context", context);

            Headers = headers;
            BodyStream = bodyStream;
            MessageId = messageId;
            Context = context;
        }

        /// <summary>
        /// The native id of the message.
        /// </summary>
        public string MessageId { get; private set; }

        /// <summary>
        /// The message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; private set; }

        /// <summary>
        /// The message body.
        /// </summary>
        public Stream BodyStream { get; private set; }

        /// <summary>
        /// Context provided by the transport.
        /// </summary>
        public ContextBag Context { get; private set; }
    }
}