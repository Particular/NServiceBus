namespace NServiceBus
{
    using System;
    using Transport;

    class MessageToBeRetried : MessageProcessingFailed
    {
        public int Attempt { get; }
        public TimeSpan Delay { get; }
        public bool IsImmediateRetry => Delay == TimeSpan.Zero;

        public MessageToBeRetried(int attempt, TimeSpan delay, ErrorContext errorContext) : base(errorContext)
        {
            Attempt = attempt;
            Delay = delay;
        }
    }
}