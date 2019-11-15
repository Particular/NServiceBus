namespace NServiceBus
{
    using System;
    using Transport;

    class RecoverabilityExecutorFactory
    {
        public RecoverabilityExecutorFactory(
            Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> defaultRecoverabilityPolicy,
            RecoverabilityConfig configuration,
            Func<string, DelayedRetryExecutor> delayedRetryExecutorFactory,
            Func<string, MoveToErrorsExecutor> moveToErrorsExecutorFactory,
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

        public RecoverabilityExecutor CreateDefault(string localAddress)
        {
            return Create(defaultRecoverabilityPolicy, localAddress, raiseNotifications: true);
        }

        public RecoverabilityExecutor Create(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> customRecoverabilityPolicy, string localAddress)
        {
            return Create(customRecoverabilityPolicy, localAddress, raiseNotifications: false);
        }

        RecoverabilityExecutor Create(Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> customRecoverabilityPolicy, string localAddress, bool raiseNotifications)
        {
            var delayedRetryExecutor = delayedRetryExecutorFactory(localAddress);
            var moveToErrorsExecutor = moveToErrorsExecutorFactory(localAddress);

            return new RecoverabilityExecutor(
                raiseNotifications,
                immediateRetriesAvailable,
                delayedRetriesAvailable,
                customRecoverabilityPolicy,
                configuration,
                delayedRetryExecutor,
                moveToErrorsExecutor,
                messageRetryNotification,
                messageFaultedNotification);
        }

        readonly bool immediateRetriesAvailable;
        readonly bool delayedRetriesAvailable;
        readonly INotificationSubscriptions<MessageToBeRetried> messageRetryNotification;
        readonly INotificationSubscriptions<MessageFaulted> messageFaultedNotification;

        Func<RecoverabilityConfig, ErrorContext, RecoverabilityAction> defaultRecoverabilityPolicy;
        Func<string, DelayedRetryExecutor> delayedRetryExecutorFactory;
        Func<string, MoveToErrorsExecutor> moveToErrorsExecutorFactory;
        RecoverabilityConfig configuration;
    }
}