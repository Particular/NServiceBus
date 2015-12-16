namespace NServiceBus.Faults
{
    using System;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    /// <summary>
    /// Provides context to behaviors on the fault pipeline.
    /// </summary>
    public class FaultContext : BehaviorContext, IFaultContext
    {
        /// <summary>
        /// Creates a new instance of the fault context.
        /// </summary>
        /// <param name="message">The message to which fault relates to.</param>
        /// <param name="errorQueueAddress">The fault queue address.</param>
        /// <param name="exception">Exception that occurred while processing the message.</param>
        /// <param name="parent">The parent context.</param>
        public FaultContext(OutgoingMessage message, string errorQueueAddress, Exception exception, IBehaviorContext parent)
            : base(parent)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNullAndEmpty(nameof(errorQueueAddress), errorQueueAddress);
            Guard.AgainstNull(nameof(exception), exception);
            Message = message;
            ErrorQueueAddress = errorQueueAddress;
            Exception = exception;
        }

        /// <summary>
        /// The message to which fault relates to.
        /// </summary>
        public OutgoingMessage Message { get; }

        /// <summary>
        /// Address of the error queue.
        /// </summary>
        public string ErrorQueueAddress { get; }

        /// <summary>
        /// Exception that occurred while processing the message.
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Adds information about faults related to current message.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void AddFaultData(string key, string value)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            Guard.AgainstNullAndEmpty(nameof(value), value);

            FaultToDispatchConnector.State state;

            if (!this.Extensions.TryGet(out state))
            {
                state = new FaultToDispatchConnector.State();
                this.Extensions.Set(state);
            }
            state.FaultyValues[key] = value;
        }
    }
}