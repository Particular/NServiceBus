namespace NServiceBus
{
    using System;
    using Pipeline;
    using Transports;

    class FaultContext : BehaviorContext, IFaultContext
    {
        public FaultContext(OutgoingMessage message, string localAddress, Exception exception, IBehaviorContext parent)
            : base(parent)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNullAndEmpty(nameof(localAddress), localAddress);
            Guard.AgainstNull(nameof(exception), exception);

            var errorQueueAddress = ErrorQueueSettings.GetConfiguredErrorQueue(Builder.Build<Settings.ReadOnlySettings>());
            Guard.AgainstNullAndEmpty(nameof(errorQueueAddress), errorQueueAddress);

            Message = message;
            ErrorQueueAddress = errorQueueAddress;
            Exception = exception;

            var headers = message.Headers;
            ExceptionHeaderHelper.SetExceptionHeaders(headers, exception, localAddress);

            headers.Remove(Headers.Retries);
            headers.Remove(Headers.FLRetries);
        }

        public OutgoingMessage Message { get; }

        public string ErrorQueueAddress { get; }

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