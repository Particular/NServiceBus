namespace NServiceBus
{
    using System;

    class RecoverabilityExecutorFactory
    {
        public RecoverabilityExecutorFactory(IRecoverabilityPolicy defaultRecoverabilityPolicy, Func<string, DelayedRetryExecutor> delayedRetryExecutorFactory,
            Func<string, MoveToErrorsExecutor> moveToErrorsExecutorFactory, TransportTransactionMode transportTransactionMode)
        {
            this.defaultRecoverabilityPolicy = defaultRecoverabilityPolicy;
            this.delayedRetryExecutorFactory = delayedRetryExecutorFactory;
            this.moveToErrorsExecutorFactory = moveToErrorsExecutorFactory;
            this.transportTransactionMode = transportTransactionMode;
        }

        public RecoverabilityExecutor CreateDefault(IEventAggregator eventAggregator, string localAddress)
        {
            return Create(defaultRecoverabilityPolicy, eventAggregator, localAddress);
        }

        public RecoverabilityExecutor Create(IRecoverabilityPolicy customRecoverabilityPolicy, IEventAggregator eventAggregator, string localAddress)
        {
            var delayedRetryExecutor = delayedRetryExecutorFactory(localAddress);
            var moveToErrorsExecutor = moveToErrorsExecutorFactory(localAddress);

            return new RecoverabilityExecutor(
                customRecoverabilityPolicy,
                eventAggregator,
                delayedRetryExecutor,
                moveToErrorsExecutor,
                transportTransactionMode != TransportTransactionMode.None);
        }

        IRecoverabilityPolicy defaultRecoverabilityPolicy;
        Func<string, DelayedRetryExecutor> delayedRetryExecutorFactory;
        Func<string, MoveToErrorsExecutor> moveToErrorsExecutorFactory;
        TransportTransactionMode transportTransactionMode;
    }
}