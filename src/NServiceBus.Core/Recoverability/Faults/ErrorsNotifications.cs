namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;
    using Transports;

    /// <summary>
    ///     Errors notifications.
    /// </summary>
    public class ErrorsNotifications : IDisposable
    {
        /// <summary>
        ///     Notification when a message is moved to the error queue.
        /// </summary>
        public IObservable<FailedMessage> MessageSentToErrorQueue => erroneousMessageList;

        /// <summary>
        ///     Notification when a message fails a first level retry.
        /// </summary>
        public IObservable<FirstLevelRetry> MessageHasFailedAFirstLevelRetryAttempt => firstLevelRetryList;

        /// <summary>
        ///     Notification when a message is sent to second level retires queue.
        /// </summary>
        public IObservable<SecondLevelRetry> MessageHasBeenSentToSecondLevelRetries => secondLevelRetryList;

        void IDisposable.Dispose()
        {
            // Injected
        }

        internal void InvokeMessageHasBeenSentToErrorQueue(IncomingMessage message, Exception exception)
        {
            erroneousMessageList.OnNext(new FailedMessage(message.MessageId, new Dictionary<string, string>(message.Headers), CopyOfBody(message.Body), exception));
        }

        internal void InvokeMessageHasFailedAFirstLevelRetryAttempt(int firstLevelRetryAttempt, IncomingMessage message, Exception exception)
        {
            firstLevelRetryList.OnNext(new FirstLevelRetry(message.MessageId, new Dictionary<string, string>(message.Headers), CopyOfBody(message.Body), exception, firstLevelRetryAttempt));
        }

        internal void InvokeMessageHasBeenSentToSecondLevelRetries(int secondLevelRetryAttempt, IncomingMessage message, Exception exception)
        {
            secondLevelRetryList.OnNext(new SecondLevelRetry(new Dictionary<string, string>(message.Headers), CopyOfBody(message.Body), exception, secondLevelRetryAttempt));
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