namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Extensibility;

    /// <summary>
    /// Allows the transport to pass signal that a message has been completed.
    /// </summary>
    public class CompleteContext : IExtendable
    {
        /// <summary>
        /// Initializes the context.
        /// </summary>
        /// <param name="messageId">Native message id.</param>
        /// <param name="wasAcknowledged">True if the message was acknowledged and removed from the queue.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="startedAt">The time that processing started.</param>
        /// <param name="completedAt">The time that processing started.</param>
        /// <param name="context">A <see cref="ContextBag" /> which can be used to extend the current object.</param>
        public CompleteContext(string messageId, bool wasAcknowledged, Dictionary<string, string> headers, DateTimeOffset startedAt, DateTimeOffset completedAt, ContextBag context)
        {
            Guard.AgainstNullAndEmpty(nameof(messageId), messageId);
            Guard.AgainstNull(nameof(startedAt), startedAt);
            Guard.AgainstNull(nameof(completedAt), completedAt);
            Guard.AgainstNull(nameof(context), context);

            MessageId = messageId;
            WasAcknowledged = wasAcknowledged;
            Headers = headers;
            StartedAt = startedAt;
            CompletedAt = completedAt;
            Extensions = context;
        }

        /// <summary>
        /// The native id of the message.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// True if the message was acknowledged and removed from the queue.
        /// </summary>
        public bool WasAcknowledged { get; }

        /// <summary>
        /// The message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; }

        /// <summary>
        /// The time that processing started.
        /// </summary>
        public DateTimeOffset StartedAt { get; }

        /// <summary>
        /// The time processing completed.
        /// </summary>
        public DateTimeOffset CompletedAt { get; }

        /// <summary>
        /// A <see cref="ContextBag" /> which can be used to extend the current object.
        /// </summary>
        public ContextBag Extensions { get; }
    }
}