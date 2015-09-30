namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Timeout.Core;

    class UpdatedTimeoutPersister : IPersistTimeouts, IPersistTimeoutsV2
    {
        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            nextTimeToRunQuery = DateTime.Now.AddYears(42);
            return Enumerable.Empty<Tuple<string, DateTime>>().ToList();
        }

        public void Add(TimeoutData timeout)
        {
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            timeoutData = null;
            return false;
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
        }

        public TimeoutData Peek(string timeoutId)
        {
            return null;
        }

        public bool TryRemove(string timeoutId)
        {
            return true;
        }
    }
}
