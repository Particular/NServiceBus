namespace NServiceBus
{
    using System;

    class RecoverabilityExecutorFactory
    {
        public RecoverabilityExecutorFactory(
            RecoverabilityConfig configuration,
            Func<DelayedRetryExecutor> delayedRetryExecutorFactory,
            Func<MoveToErrorsExecutor> moveToErrorsExecutorFactory,
            bool immediateRetriesAvailable,
            bool delayedRetriesAvailable)
        {
            this.configuration = configuration;
            this.delayedRetryExecutorFactory = delayedRetryExecutorFactory;
            this.moveToErrorsExecutorFactory = moveToErrorsExecutorFactory;
            this.immediateRetriesAvailable = immediateRetriesAvailable;
            this.delayedRetriesAvailable = delayedRetriesAvailable;
        }

        public RecoverabilityExecutor CreateRecoverabilityExecutor()
        {
            var delayedRetryExecutor = delayedRetryExecutorFactory();
            var moveToErrorsExecutor = moveToErrorsExecutorFactory();

            return new RecoverabilityExecutor(
                immediateRetriesAvailable,
                delayedRetriesAvailable,
                configuration,
                delayedRetryExecutor,
                moveToErrorsExecutor);
        }

        public SatelliteRecoverabilityExecutor CreateSatelliteRecoverabilityExecutor()
        {
            var delayedRetryExecutor = delayedRetryExecutorFactory();
            var moveToErrorsExecutor = moveToErrorsExecutorFactory();

            return new SatelliteRecoverabilityExecutor(
                immediateRetriesAvailable,
                delayedRetriesAvailable,
                configuration,
                delayedRetryExecutor,
                moveToErrorsExecutor);
        }

        readonly bool immediateRetriesAvailable;
        readonly bool delayedRetriesAvailable;

        Func<DelayedRetryExecutor> delayedRetryExecutorFactory;
        Func<MoveToErrorsExecutor> moveToErrorsExecutorFactory;
        RecoverabilityConfig configuration;
    }
}