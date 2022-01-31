namespace NServiceBus
{
    using System;

    class RecoverabilityExecutorFactory
    {
        public RecoverabilityExecutorFactory(
            Func<DelayedRetryExecutor> delayedRetryExecutorFactory,
            Func<MoveToErrorsExecutor> moveToErrorsExecutorFactory)
        {
            this.delayedRetryExecutorFactory = delayedRetryExecutorFactory;
            this.moveToErrorsExecutorFactory = moveToErrorsExecutorFactory;
        }

        public RecoverabilityExecutor CreateRecoverabilityExecutor()
        {
            var delayedRetryExecutor = delayedRetryExecutorFactory();
            var moveToErrorsExecutor = moveToErrorsExecutorFactory();

            return new RecoverabilityExecutor(delayedRetryExecutor, moveToErrorsExecutor);
        }

        public SatelliteRecoverabilityExecutor CreateSatelliteRecoverabilityExecutor()
        {
            var delayedRetryExecutor = delayedRetryExecutorFactory();
            var moveToErrorsExecutor = moveToErrorsExecutorFactory();

            return new SatelliteRecoverabilityExecutor(
                delayedRetryExecutor,
                moveToErrorsExecutor);
        }

        Func<DelayedRetryExecutor> delayedRetryExecutorFactory;
        Func<MoveToErrorsExecutor> moveToErrorsExecutorFactory;
    }
}