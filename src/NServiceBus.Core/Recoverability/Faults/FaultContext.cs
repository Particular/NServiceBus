namespace NServiceBus
{
    using System;
    using Pipeline;
    using Transports;

    /// <summary>
    ///
    /// </summary>
    class FaultContext : BehaviorContext, IFaultContext
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        /// <param name="parent"></param>
        public FaultContext(OutgoingMessage message, Exception exception, IBehaviorContext parent)
            : base(parent)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(exception), exception);

            Message = message;
            Exception = exception;
        }

        public OutgoingMessage Message { get; }

        public string ErrorQueueAddress { get; set; }

        public Exception Exception { get; }

        public void AddFaultData(string key, string value)
        {
            Guard.AgainstNullAndEmpty(nameof(key), key);
            Guard.AgainstNullAndEmpty(nameof(value), value);

            FaultToDispatchConnector.State state;

            if (!Extensions.TryGet(out state))
            {
                state = new FaultToDispatchConnector.State();
                Extensions.Set(state);
            }
            state.FaultyValues[key] = value;
        }
    }
}