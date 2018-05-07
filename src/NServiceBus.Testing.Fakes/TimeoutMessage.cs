namespace NServiceBus.Testing
{
    using System;

    /// <summary>
    /// Represents a message that is timed out, if not processed within a given timespan. Contains the message itself and its
    /// associated options.
    /// </summary>
    /// <typeparam name="TMessage">The message type</typeparam>
    public class TimeoutMessage<TMessage> : OutgoingMessage<TMessage, SendOptions>
    {
        /// <summary>
        /// Creates a new instance for the given message and options.
        /// </summary>
        public TimeoutMessage(TMessage message, SendOptions options, TimeSpan within) : base(message, options)
        {
            Within = within;
        }

        /// <summary>
        /// Creates a new instance for the given message and options.
        /// </summary>
        public TimeoutMessage(TMessage message, SendOptions options, DateTimeOffset at) : base(message, options)
        {
            At = at;
        }

        /// <summary>
        /// Specifies a <see cref="TimeSpan" /> for the message to be processed, before timeout.
        /// </summary>
        public TimeSpan? Within { get; }
        
        /// <summary>
        /// Specifies a <see cref="DateTime" /> for the message to be processed at.
        /// </summary>
        public DateTimeOffset? At { get; }
    }
}