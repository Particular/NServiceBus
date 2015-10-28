namespace NServiceBus.Recoverability.SecondLevelRetries
{
    using System;
    using System.Collections.Concurrent;

    class SlrStatusStorage
    {
        public void MarkForRetry(string messageId, Exception exception)
        {
            failedMessages.AddOrUpdate(messageId, exception, (m, e) => e);
        }

        public Exception GetExceptionForRetry(string messageId)
        {
            Exception exc;
            return !failedMessages.TryGetValue(messageId, out exc) ? null : exc;
        }

        public void MarkAsPickedUpForRetry(string messageId)
        {
            Exception exc;
            failedMessages.TryRemove(messageId, out exc);
        }

        ConcurrentDictionary<string, Exception> failedMessages = new ConcurrentDictionary<string, Exception>();
    }
}
