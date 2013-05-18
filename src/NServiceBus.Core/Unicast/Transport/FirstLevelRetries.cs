namespace NServiceBus.Unicast.Transport
{
    using System;
    using System.Collections.Concurrent;
    using NServiceBus.Faults;

    internal class FirstLevelRetries
    {
        private readonly ConcurrentDictionary<string, Tuple<int, Exception>> failuresPerMessage = new ConcurrentDictionary<string, Tuple<int, Exception>>();
        private readonly IManageMessageFailures failureManager;
        private readonly int maxRetries;

        public FirstLevelRetries(int maxRetries, IManageMessageFailures failureManager)
        {
            this.maxRetries = maxRetries;
            this.failureManager = failureManager;
        }

        public bool HasMaxRetriesForMessageBeenReached(TransportMessage message)
        {
            string messageId = message.Id;
            Tuple<int, Exception> e;

            if (failuresPerMessage.TryGetValue(messageId, out e))
            {
                if (e.Item1 < maxRetries)
                {
                    return false;
                }

                if (TryInvokeFaultManager(message, e.Item2))
                {
                    ClearFailuresForMessage(message);
                }

                return true;
            }

            return false;
        }

        public void ClearFailuresForMessage(TransportMessage message)
        {
            string messageId = message.Id;
            Tuple<int, Exception> e;
            failuresPerMessage.TryRemove(messageId, out e);
        }

        public void IncrementFailuresForMessage(TransportMessage message, Exception e)
        {
            failuresPerMessage.AddOrUpdate(message.Id, new Tuple<int, Exception>(1, e),
                                           (s, i) => new Tuple<int, Exception>(i.Item1 + 1, e));
        }

        private bool TryInvokeFaultManager(TransportMessage message, Exception exception)
        {
            try
            {
                Exception e = exception;

                if (e is AggregateException)
                {
                    e = e.GetBaseException();
                }

                if (e is TransportMessageHandlingFailedException)
                {
                    e = e.InnerException;
                }

                failureManager.ProcessingAlwaysFailsForMessage(message, e);

                return true;
            }
            catch (Exception ex)
            {
                Configure.Instance.RaiseCriticalError(
                    String.Format("Fault manager failed to process the failed message with id {0}", message.Id), ex);
            }

            return false;
        }
    }
}