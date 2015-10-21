namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Timeout.Core;

    class TemporarilyUnavailableTimeoutPersister : IPersistTimeouts
    {
        public int SecondsToWait { get; set; }
        static bool isAvailable = false;
        DateTime NextChangeTime;

        public TemporarilyUnavailableTimeoutPersister()
        {
            NextChangeTime = DateTime.Now.AddSeconds(SecondsToWait);
        }

        private void ThrowExceptionUntilWait()
        {
            if (NextChangeTime <= DateTime.Now)
            {
                NextChangeTime = DateTime.Now.AddSeconds(SecondsToWait);
                isAvailable = !isAvailable;
            }

            if (!isAvailable)
            {
                throw new Exception("Persister is temporarily unavailable");
            }
        }

        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            ThrowExceptionUntilWait();
            nextTimeToRunQuery = DateTime.Now.AddSeconds(2);
            return Enumerable.Empty<Tuple<string, DateTime>>().ToList();
        }

        public void Add(TimeoutData timeout)
        {
            ThrowExceptionUntilWait();
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            ThrowExceptionUntilWait();
            timeoutData = null;
            return true;
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            ThrowExceptionUntilWait();
        }
    }
}
