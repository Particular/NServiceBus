namespace NServiceBus.Recoverability.SecondLevelRetries
{
    using System;
    using System.Collections.Concurrent;

    class SlrStatusStorage
    {
        public void AddException(string messageId, Exception exception)
        {
            failedMessages.AddOrUpdate(messageId, exception, (m, e) => e);
        }

        public bool TryGetException(string messageId, out Exception exception)
        {
            return failedMessages.TryGetValue(messageId, out exception);
        }

        public void ClearException(string messageId)
        {
            Exception exc;
            failedMessages.TryRemove(messageId, out exc);
        }

        public void ClearAllExceptions()
        {
            failedMessages.Clear();
        }

        ConcurrentDictionary<string, Exception> failedMessages = new ConcurrentDictionary<string, Exception>();
    }
}
