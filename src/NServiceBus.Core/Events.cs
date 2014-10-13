namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    ///     NServiceBus domain events.
    /// </summary>
    public class Events
    {
        /// <summary>
        /// 
        /// </summary>
        public IObservable<ErroneousMessage> MessageSentToErrorQueue
        {
            get { return erroneousMessageList; }
        }

        /// <summary>
        /// </summary>
        public IObservable<FirstLevelRetry> MessageHasFailedAFirstLevelRetryAttempt
        {
            get { return firstLevelRetryList; }
        }

        /// <summary>
        /// </summary>
        public IObservable<SecondLevelRetry> MessageHasBeenSentToSecondLevelRetries
        {
            get { return secondLevelRetryList; }
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

    /// <summary>
    /// </summary>
    public class SecondLevelRetry
    {
        /// <summary>
        ///     Gets the message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; internal set; }

        /// <summary>
        ///     Gets a byte array to the body content of the message
        /// </summary>
        public byte[] Body { get; internal set; }

        /// <summary>
        ///     The exception that caused this message to fail.
        /// </summary>
        public Exception Exception { get; internal set; }

        /// <summary>
        ///     Number of retry attempt.
        /// </summary>
        public int RetryAttempt { get; set; }
    }

    /// <summary>
    /// </summary>
    public class FirstLevelRetry
    {
        /// <summary>
        ///     Gets the message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; internal set; }

        /// <summary>
        ///     Gets a byte array to the body content of the message
        /// </summary>
        public byte[] Body { get; internal set; }

        /// <summary>
        ///     The exception that caused this message to fail.
        /// </summary>
        public Exception Exception { get; internal set; }

        /// <summary>
        ///     Number of retry attempt.
        /// </summary>
        public int RetryAttempt { get; set; }
    }

    /// <summary>
    /// </summary>
    public class ErroneousMessage
    {
        /// <summary>
        ///     Gets the message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; internal set; }

        /// <summary>
        ///     Gets a byte array to the body content of the message
        /// </summary>
        public byte[] Body { get; internal set; }

        /// <summary>
        ///     The exception that caused this message to fail.
        /// </summary>
        public Exception Exception { get; internal set; }
    }
}