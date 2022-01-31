namespace NServiceBus
{
    using System;
    using NServiceBus.Pipeline;
    using Transport;

    class RecoverabilityContext : BehaviorContext, IRecoverabilityContext
    {
        public RecoverabilityContext(
            ErrorContext errorContext,
            RecoverabilityConfig recoverabilityConfig,
            RecoverabilityAction recoverabilityAction,
            IBehaviorContext parent) : base(parent)
        {
            ErrorContext = errorContext;
            RecoverabilityConfiguration = recoverabilityConfig;
            RecoverabilityAction = recoverabilityAction;
        }

        public ErrorContext ErrorContext { get; }

        public RecoverabilityConfig RecoverabilityConfiguration { get; }

        public RecoverabilityAction RecoverabilityAction
        {
            get => recoverabilityAction;
            set
            {
                if (locked)
                {
                    throw new InvalidOperationException("The RecoverabilityAction has already been executed and can't be changed");
                }
                recoverabilityAction = value;
            }
        }

        public void PreventChanges()
        {
            locked = true;
        }

        RecoverabilityAction recoverabilityAction;
        bool locked;

    }
}