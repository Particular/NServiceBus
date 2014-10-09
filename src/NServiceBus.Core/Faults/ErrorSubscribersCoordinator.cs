namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Logging;
    using NServiceBus.ObjectBuilder;

    class ErrorSubscribersCoordinator
    {
        readonly List<IErrorSubscriber> subscribers;
        ILog logger = LogManager.GetLogger<ErrorSubscribersCoordinator>();

        public ErrorSubscribersCoordinator(IBuilder builder)
        {
            subscribers = builder.BuildAll<IErrorSubscriber>().ToList();
        }

        public void InvokeMessageHasBeenSentToErrorQueue(TransportMessage message, Exception exception)
        {
            Invoke(subscriber => subscriber.MessageHasBeenSentToErrorQueue(message, exception), "MessageHasBeenSentToErrorQueue");
        }

        public void InvokeMessageHasFailedAFirstLevelRetryAttempt(int firstLevelRetryAttempt, TransportMessage message, Exception exception)
        {
            Invoke(subscriber => subscriber.MessageHasFailedAFirstLevelRetryAttempt(firstLevelRetryAttempt, message, exception), "MessageHasFailedAFirstLevelRetryAttempt");
        }

        public void InvokeMessageHasBeenSentToSecondLevelRetries(int secondLevelRetryAttempt, TransportMessage message, Exception exception)
        {
            Invoke(subscriber => subscriber.MessageHasBeenSentToSecondLevelRetries(secondLevelRetryAttempt, message, exception), "MessageHasBeenSentToSecondLevelRetries");
        }

        void Invoke(Action<IErrorSubscriber> action, string methodName)
        {
            var exceptions = new List<Exception>(subscribers.Count);

            subscribers.ForEach(subscriber =>
            {
                try
                {
                    action(subscriber);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            });

            if (exceptions.Count == 0)
            {
                return;
            }

            var aggregateException = new AggregateException(string.Format("Failed to invoke all implementations of IErrorSubscriber.{0}.", methodName), exceptions);
            logger.Error(aggregateException.Message, aggregateException);
        }
    }
}