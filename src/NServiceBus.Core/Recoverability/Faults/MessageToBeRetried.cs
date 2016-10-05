namespace NServiceBus
{
    using System;
    using Transport;

    class MessageToBeRetried : MessageProcessingFailed
    {
        public int Attempt { get; }
        public TimeSpan Delay { get; }
        public bool IsImmediateRetry => Delay == TimeSpan.Zero;

        public MessageToBeRetried(int attempt, TimeSpan delay, IncomingMessage message, Exception exception) : base(message, exception)
        {
            Attempt = attempt;
            Delay = delay;
        }
    }
}