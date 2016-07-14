namespace NServiceBus
{
    using System;
    using Transport;

    class RecoverabilityExecutorFactory
    {
        public RecoverabilityExecutorFactory(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> defaultRecoverabilityPolicy, RecoverabilityConfig configuration, Func<string, DelayedRetryExecutor> delayedRetryExecutorFactory,
            Func<string, MoveToErrorsExecutor> moveToErrorsExecutorFactory, TransportTransactionMode transportTransactionMode)
        {
            this.configuration = configuration;
            this.defaultRecoverabilityPolicy = defaultRecoverabilityPolicy;
            this.delayedRetryExecutorFactory = delayedRetryExecutorFactory;
            this.moveToErrorsExecutorFactory = moveToErrorsExecutorFactory;
            this.transportTransactionMode = transportTransactionMode;
        }

        public RecoverabilityExecutor CreateDefault(IEventAggregator eventAggregator, string localAddress)
        {
            return Create(defaultRecoverabilityPolicy, eventAggregator, localAddress, raiseNotifications: true);
        }

        public RecoverabilityExecutor Create(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> customRecoverabilityPolicy, IEventAggregator eventAggregator, string localAddress)
        {
            return Create(customRecoverabilityPolicy, eventAggregator, localAddress, raiseNotifications: false);
        }

        RecoverabilityExecutor Create(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> customRecoverabilityPolicy, IEventAggregator eventAggregator, string localAddress, bool raiseNotifications)
        {
            var delayedRetryExecutor = delayedRetryExecutorFactory(localAddress);
            var moveToErrorsExecutor = moveToErrorsExecutorFactory(localAddress);

            return new RecoverabilityExecutor(
                raiseNotifications,
                customRecoverabilityPolicy,
                configuration,
                eventAggregator,
                delayedRetryExecutor,
                moveToErrorsExecutor,
                transportTransactionMode != TransportTransactionMode.None);
        }

        Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> defaultRecoverabilityPolicy;
        Func<string, DelayedRetryExecutor> delayedRetryExecutorFactory;
        Func<string, MoveToErrorsExecutor> moveToErrorsExecutorFactory;
        TransportTransactionMode transportTransactionMode;
        RecoverabilityConfig configuration;
    }
}