namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using Faults;

    class FirstLevelRetries
    {
        ConcurrentDictionary<string, Tuple<int, Exception>> failuresPerMessage = new ConcurrentDictionary<string, Tuple<int, Exception>>();
        IManageMessageFailures failureManager;
        CriticalError criticalError;
        int maxRetries;

        public FirstLevelRetries(int maxRetries, IManageMessageFailures failureManager, CriticalError criticalError)
        {
            this.maxRetries = maxRetries;
            this.failureManager = failureManager;
            this.criticalError = criticalError;
        }

        public bool HasMaxRetriesForMessageBeenReached(TransportMessage message)
        {
            var messageId = message.Id;
            Tuple<int, Exception> e;

            if (failuresPerMessage.TryGetValue(messageId, out e))
            {
                if (e.Item1 < maxRetries)
                {
                    return false;
                }

                TryInvokeFaultManager(message, e.Item2, e.Item1);
                ClearFailuresForMessage(message);

                return true;
            }

            return false;
        }

        public void ClearFailuresForMessage(TransportMessage message)
        {
            var messageId = message.Id;
            Tuple<int, Exception> e;
            failuresPerMessage.TryRemove(messageId, out e);
        }

        public void IncrementFailuresForMessage(TransportMessage message, Exception e)
        {
            failuresPerMessage.AddOrUpdate(message.Id, new Tuple<int, Exception>(1, e),
                                           (s, i) => new Tuple<int, Exception>(i.Item1 + 1, e));
        }

        void TryInvokeFaultManager(TransportMessage message, Exception exception, int numberOfAttempts)
        {
            try
            {
                message.RevertToOriginalBodyIfNeeded();
                var numberOfRetries = numberOfAttempts - 1;
                message.Headers[Headers.FLRetries] = numberOfRetries.ToString(CultureInfo.InvariantCulture);
                failureManager.ProcessingAlwaysFailsForMessage(message, exception);
            }
            catch (Exception ex)
            {
                criticalError.Raise(String.Format("Fault manager failed to process the failed message with id {0}", message.Id), ex);

                throw;
            }
        }
    }
}