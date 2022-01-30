namespace NServiceBus
{
    using NServiceBus.Pipeline;
    using Transport;

    class RecoverabilityContext : BehaviorContext, IRecoverabilityContext
    {
        public RecoverabilityContext(ErrorContext errorContext, RecoverabilityAction recoverabilityAction, IBehaviorContext parent)
           : base(parent)
        {
            Guard.AgainstNull(nameof(errorContext), errorContext);
            ErrorContext = errorContext;

            // The safe default is to retry the message
            ActionToTake = ErrorHandleResult.RetryRequired;
            RecoverabilityAction = recoverabilityAction;
        }

        public ErrorContext ErrorContext { get; }

        public ErrorHandleResult ActionToTake { get; set; }

        public RecoverabilityAction RecoverabilityAction { get; set; }
    }
}