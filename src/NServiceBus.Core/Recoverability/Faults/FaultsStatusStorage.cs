using System;

namespace NServiceBus.Recoverability.Faults
{
    using System.Collections.Concurrent;

    class FaultsStatusStorage
    {
        public void MarkForMovingToErrorQueue(string messageId, Exception exception)
        {
            failedMessages.AddOrUpdate(messageId, exception, (m, e) => e);
        }

        public Exception GetExceptionForMovingToErrorQueue(string messageId)
        {
            Exception exc;
            return !failedMessages.TryGetValue(messageId, out exc) ? null : exc;
        }

        public void MarkAsPickedUpForMovingToErrorQueue(string messageId)
        {
            Exception exc;
            failedMessages.TryRemove(messageId, out exc);
        }

        ConcurrentDictionary<string, Exception> failedMessages = new ConcurrentDictionary<string, Exception>();
    }
}
