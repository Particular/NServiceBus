namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Errors notifications
    /// </summary>
    public class ErrorsNotifications: IDisposable
    {
        /// <summary>
        /// Notification when a message is moved to the error queue.
        /// </summary>
        public IObservable<ErroneousMessage> MessageSentToErrorQueue
        {
            get { return erroneousMessageList; }
        }

        /// <summary>
        /// Notification when a message fails a first level retry.
        /// </summary>
        public IObservable<FirstLevelRetry> MessageHasFailedAFirstLevelRetryAttempt
        {
            get { return firstLevelRetryList; }
        }

        /// <summary>
        /// Notification when a message is sent to second level retires queue.
        /// </summary>
        public IObservable<SecondLevelRetry> MessageHasBeenSentToSecondLevelRetries
        {
            get { return secondLevelRetryList; }
        }

        void IDisposable.Dispose()
        {
            // Injected
        }

        internal void InvokeMessageHasBeenSentToErrorQueue(TransportMessage message, Exception exception)
        {
            erroneousMessageList.Add(new ErroneousMessage
            {
                Headers = new Dictionary<string, string>(message.Headers),
                Body = CopyOfBody(message.Body),
                Exception = exception,
            });
        }

        internal void InvokeMessageHasFailedAFirstLevelRetryAttempt(int firstLevelRetryAttempt, TransportMessage message, Exception exception)
        {
            firstLevelRetryList.Add(new FirstLevelRetry
            {
                Headers = new Dictionary<string, string>(message.Headers),
                Body = CopyOfBody(message.Body),
                Exception = exception,
                RetryAttempt = firstLevelRetryAttempt
            });
        }

        internal void InvokeMessageHasBeenSentToSecondLevelRetries(int secondLevelRetryAttempt, TransportMessage message, Exception exception)
        {
            secondLevelRetryList.Add(new SecondLevelRetry
            {
                Headers = new Dictionary<string, string>(message.Headers),
                Body = CopyOfBody(message.Body),
                Exception = exception,
                RetryAttempt = secondLevelRetryAttempt
            });
        }

        static byte[] CopyOfBody(byte[] body)
        {
            var copyBody = new byte[body.Length];

            Buffer.BlockCopy(body, 0, copyBody, 0, body.Length);

            return copyBody;
        }

        ObservableList<ErroneousMessage> erroneousMessageList = new ObservableList<ErroneousMessage>();
        ObservableList<FirstLevelRetry> firstLevelRetryList = new ObservableList<FirstLevelRetry>();
        ObservableList<SecondLevelRetry> secondLevelRetryList = new ObservableList<SecondLevelRetry>();
    }
}