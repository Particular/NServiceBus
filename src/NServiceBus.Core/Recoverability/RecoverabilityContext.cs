namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using Transport;

    class RecoverabilityContext : BehaviorContext, IRecoverabilityContext
    {
        public RecoverabilityContext(IncomingMessage failedMessage, IBehaviorContext parent)
           : base(parent)
        {
            Guard.AgainstNull(nameof(failedMessage), failedMessage);
            FailedMessage = failedMessage;

            // The safe default is to retry the message
            ActionToTake = ErrorHandleResult.RetryRequired;
        }

        public IncomingMessage FailedMessage { get; }

        public ErrorHandleResult ActionToTake { get; set; }
    }
}