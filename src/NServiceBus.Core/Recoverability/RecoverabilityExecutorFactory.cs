namespace NServiceBus
{
    using System;
    using Transport;

    class RecoverabilityExecutorFactory
    {
        public RecoverabilityExecutorFactory(
            Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> defaultRecoverabilityPolicy,
            RecoverabilityConfig configuration,
            Func<DelayedRetryExecutor> delayedRetryExecutorFactory,
            Func<MoveToErrorsExecutor> moveToErrorsExecutorFactory,
            bool immediateRetriesAvailable,
            bool delayedRetriesAvailable,
            INotificationSubscriptions<MessageToBeRetried> messageRetryNotification,
            INotificationSubscriptions<MessageFaulted> messageFaultedNotification)
        {
            this.configuration = configuration;
            this.defaultRecoverabilityPolicy = defaultRecoverabilityPolicy;
            this.delayedRetryExecutorFactory = delayedRetryExecutorFactory;
            this.moveToErrorsExecutorFactory = moveToErrorsExecutorFactory;
            this.immediateRetriesAvailable = immediateRetriesAvailable;
            this.delayedRetriesAvailable = delayedRetriesAvailable;
            this.messageRetryNotification = messageRetryNotification;
            this.messageFaultedNotification = messageFaultedNotification;
        }

        public RecoverabilityExecutor CreateDefault()
        {
            var delayedRetryExecutor = delayedRetryExecutorFactory();
            var moveToErrorsExecutor = moveToErrorsExecutorFactory();

            return new RecoverabilityExecutor(
                immediateRetriesAvailable,
                delayedRetriesAvailable,
                defaultRecoverabilityPolicy,
                configuration,
                delayedRetryExecutor,
                moveToErrorsExecutor,
                messageRetryNotification,
                messageFaultedNotification);
        }

        public SatelliteRecoverabilityExecutor CreateSatelliteRecoverabilityExecutor(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> customRecoverabilityPolicy)
        {
            var delayedRetryExecutor = delayedRetryExecutorFactory();
            var moveToErrorsExecutor = moveToErrorsExecutorFactory();

            return new SatelliteRecoverabilityExecutor(
                immediateRetriesAvailable,
                delayedRetriesAvailable,
                customRecoverabilityPolicy,
                configuration,
                delayedRetryExecutor,
                moveToErrorsExecutor);
        }

        readonly bool immediateRetriesAvailable;
        readonly bool delayedRetriesAvailable;
        readonly INotificationSubscriptions<MessageToBeRetried> messageRetryNotification;
        readonly INotificationSubscriptions<MessageFaulted> messageFaultedNotification;

        Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> defaultRecoverabilityPolicy;
        Func<DelayedRetryExecutor> delayedRetryExecutorFactory;
        Func<MoveToErrorsExecutor> moveToErrorsExecutorFactory;
        RecoverabilityConfig configuration;
    }
}