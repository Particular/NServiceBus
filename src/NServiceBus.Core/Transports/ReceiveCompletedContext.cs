namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;

    /// <summary>
    /// Allows the transport to pass signal that it has completed receiving a message.
    /// </summary>
    public class ReceiveCompletedContext
    {
        /// <summary>
        /// Initializes the context.
        /// </summary>
        /// <param name="nativeMessageId">The native message ID.</param>
        /// <param name="result">The <see cref="ReceiveResult"/>.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="startedAt">The time that processing started.</param>
        /// <param name="completedAt">The time that processing started.</param>
        /// <param name="context">A <see cref="ReadOnlyContextBag" /> containing the context for the message receive operation.</param>
        public ReceiveCompletedContext(string nativeMessageId, ReceiveResult result, Dictionary<string, string> headers, DateTimeOffset startedAt, DateTimeOffset completedAt, ReadOnlyContextBag context)
        {
            Guard.AgainstNullAndEmpty(nameof(nativeMessageId), nativeMessageId);
            Guard.AgainstNull(nameof(headers), headers);
            Guard.AgainstNull(nameof(startedAt), startedAt);
            Guard.AgainstNull(nameof(completedAt), completedAt);
            Guard.AgainstNull(nameof(context), context);

            NativeMessageId = nativeMessageId;
            Result = result;
            Headers = headers;
            StartedAt = startedAt;
            CompletedAt = completedAt;
            Extensions = context;
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

        /// <summary>
        /// A collection of additional information for this receive operation.
        /// </summary>
        public ReadOnlyContextBag Extensions { get; }
    }
}