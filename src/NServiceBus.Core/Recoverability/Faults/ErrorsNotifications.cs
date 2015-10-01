namespace NServiceBus.Faults
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.Transports;

    /// <summary>
    /// Errors notifications.
    /// </summary>
    public class ErrorsNotifications : IDisposable
    {
        /// <summary>
        /// Notification when a message is moved to the error queue.
        /// </summary>
        public IObservable<FailedMessage> MessageSentToErrorQueue => erroneousMessageList;

        /// <summary>
        /// Notification when a message fails a first level retry.
        /// </summary>
        public IObservable<FirstLevelRetry> MessageHasFailedAFirstLevelRetryAttempt => firstLevelRetryList;

        /// <summary>
        /// Notification when a message is sent to second level retires queue.
        /// </summary>
        public IObservable<SecondLevelRetry> MessageHasBeenSentToSecondLevelRetries => secondLevelRetryList;

        void IDisposable.Dispose()
        {
            // Injected
        }

        internal void InvokeMessageHasBeenSentToErrorQueue(IncomingMessage message, Exception exception)
        {
            // TODO: Do we really need to copy?
            using (var stream = new MemoryStream())
            {
                CopyOfBody(message.BodyStream, stream);
                erroneousMessageList.OnNext(new FailedMessage(new Dictionary<string, string>(message.Headers), stream, exception));
            }
        }

        internal void InvokeMessageHasFailedAFirstLevelRetryAttempt(int firstLevelRetryAttempt, IncomingMessage message, Exception exception)
        {
            // TODO: Do we really need to copy?
            using (var stream = new MemoryStream())
            {
                CopyOfBody(message.BodyStream, stream);
                firstLevelRetryList.OnNext(new FirstLevelRetry(new Dictionary<string, string>(message.Headers), stream, exception, firstLevelRetryAttempt));
            }
        }

        internal void InvokeMessageHasBeenSentToSecondLevelRetries(int secondLevelRetryAttempt, IncomingMessage message, Exception exception)
        {
            // TODO: Do we really need to copy?
            using (var stream = new MemoryStream())
            {
                CopyOfBody(message.BodyStream, stream);
                secondLevelRetryList.OnNext(new SecondLevelRetry(new Dictionary<string, string>(message.Headers), stream, exception, secondLevelRetryAttempt));
            }
        }

        static void CopyOfBody(Stream origin, Stream destination)
        {
            origin?.CopyTo(destination);
        }

        Observable<FailedMessage> erroneousMessageList = new Observable<FailedMessage>();
        Observable<FirstLevelRetry> firstLevelRetryList = new Observable<FirstLevelRetry>();
        Observable<SecondLevelRetry> secondLevelRetryList = new Observable<SecondLevelRetry>();
    }
}