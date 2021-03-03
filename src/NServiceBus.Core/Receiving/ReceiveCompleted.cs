namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Transport;

    /// <summary>
    /// Event raised when the transport has completed receiving a message.
    /// </summary>
    public class ReceiveCompleted
    {
        /// <summary>
        /// Constructs the event.
        /// </summary>
        public ReceiveCompleted(string nativeMessageId, ReceiveResult result, Dictionary<string, string> headers, DateTimeOffset startedAt, DateTimeOffset completedAt)
        {
            NativeMessageId = nativeMessageId;
            Result = result;
            Headers = headers;
            StartedAt = startedAt;
            CompletedAt = completedAt;
        }

        /// <summary>
        /// The native message ID.
        /// </summary>
        public string NativeMessageId { get; }

        /// <summary>
        /// The <see cref="ReceiveResult"/>.
        /// </summary>
        public ReceiveResult Result { get; }

        /// <summary>
        /// The message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// The time receiving started.
        /// </summary>
        public DateTimeOffset StartedAt { get; }

        /// <summary>
        /// The time receiving completed.
        /// </summary>
        public DateTimeOffset CompletedAt { get; }
    }
}
