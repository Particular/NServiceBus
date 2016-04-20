namespace NServiceBus
{
    using System;
    using Pipeline;
    using Transports;

    class FaultContext : BehaviorContext, IFaultContext
    {
        public FaultContext(OutgoingMessage message, string sourceQueueAddress, Exception exception, IBehaviorContext parent)
            : base(parent)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNullAndEmpty(nameof(sourceQueueAddress), sourceQueueAddress);
            Guard.AgainstNull(nameof(exception), exception);
            Message = message;
            SourceQueueAddress = sourceQueueAddress;
            Exception = exception;
        }

        public OutgoingMessage Message { get; }

        public string SourceQueueAddress { get; }

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