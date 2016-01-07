namespace NServiceBus
{
    using System;
    using NServiceBus.Faults;
    using NServiceBus.Pipeline;
    using NServiceBus.Transports;

    class FaultContext : BehaviorContext, IFaultContext
    {
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

        public OutgoingMessage Message { get; }

        public string ErrorQueueAddress { get; }

        public Exception Exception { get; }

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