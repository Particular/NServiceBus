namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Errors notifications
    /// </summary>
    public class ErrorsNotifications : IDisposable
    {
        /// <summary>
        /// Notification when a message is moved to the error queue.
        /// </summary>
        public IObservable<FailedMessage> MessageSentToErrorQueue
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
            erroneousMessageList.Publish(new FailedMessage(new Dictionary<string, string>(message.Headers), CopyOfBody(message.Body), exception));
        }

        internal void InvokeMessageHasFailedAFirstLevelRetryAttempt(int firstLevelRetryAttempt, TransportMessage message, Exception exception)
        {
            firstLevelRetryList.Publish(new FirstLevelRetry(new Dictionary<string, string>(message.Headers), CopyOfBody(message.Body), exception, firstLevelRetryAttempt));
        }

        internal void InvokeMessageHasBeenSentToSecondLevelRetries(int secondLevelRetryAttempt, TransportMessage message, Exception exception)
        {
            secondLevelRetryList.Publish(new SecondLevelRetry(new Dictionary<string, string>(message.Headers), CopyOfBody(message.Body), exception, secondLevelRetryAttempt));
        }

        static byte[] CopyOfBody(byte[] body)
        {
            if (body == null)
            {
                return null;
            }

            var copyBody = new byte[body.Length];

            Buffer.BlockCopy(body, 0, copyBody, 0, body.Length);

            return copyBody;
        }

        Observable<FailedMessage> erroneousMessageList = new Observable<FailedMessage>();
        Observable<FirstLevelRetry> firstLevelRetryList = new Observable<FirstLevelRetry>();
        Observable<SecondLevelRetry> secondLevelRetryList = new Observable<SecondLevelRetry>();
    }
}