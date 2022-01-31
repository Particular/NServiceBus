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
            bool delayedRetriesAvailable,
            INotificationSubscriptions<MessageToBeRetried> messageRetryNotification,
            INotificationSubscriptions<MessageFaulted> messageFaultedNotification)
        {
            this.configuration = configuration;
            this.delayedRetryExecutorFactory = delayedRetryExecutorFactory;
            this.moveToErrorsExecutorFactory = moveToErrorsExecutorFactory;
            this.immediateRetriesAvailable = immediateRetriesAvailable;
            this.delayedRetriesAvailable = delayedRetriesAvailable;
            this.messageRetryNotification = messageRetryNotification;
            this.messageFaultedNotification = messageFaultedNotification;
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
                moveToErrorsExecutor,
                messageRetryNotification,
                messageFaultedNotification);
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
        readonly INotificationSubscriptions<MessageToBeRetried> messageRetryNotification;
        readonly INotificationSubscriptions<MessageFaulted> messageFaultedNotification;

        Func<DelayedRetryExecutor> delayedRetryExecutorFactory;
        Func<MoveToErrorsExecutor> moveToErrorsExecutorFactory;
        RecoverabilityConfig configuration;
    }
}