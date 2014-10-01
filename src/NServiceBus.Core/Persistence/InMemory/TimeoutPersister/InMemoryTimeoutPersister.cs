namespace NServiceBus.InMemory.TimeoutPersister
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Timeout.Core;

    class InMemoryTimeoutPersister : IPersistTimeouts
    {
        readonly IList<TimeoutData> storage = new List<TimeoutData>();
        readonly object lockObject = new object();

        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            lock (lockObject)
            {
                var now = DateTime.UtcNow;

                var nextTimeout = storage
                    .Where(data => data.Time > now)
                    .OrderBy(data => data.Time)
                    .FirstOrDefault();

                nextTimeToRunQuery = nextTimeout != null ? nextTimeout.Time : now.AddMinutes(1);

                return storage
                    .Where(data => data.Time > startSlice && data.Time <= now)
                    .OrderBy(data => data.Time)
                    .Select(t => new Tuple<string, DateTime>(t.Id, t.Time));
            }
        }

        public void Add(TimeoutData timeout)
        {
            lock (lockObject)
            {
                timeout.Id = Guid.NewGuid().ToString();
                storage.Add(timeout);
            }
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            lock (lockObject)
            {
                timeoutData = storage.SingleOrDefault(t => t.Id == timeoutId);

                return timeoutData != null && storage.Remove(timeoutData);
            }
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            lock (lockObject)
            {
                foreach (var item in storage.Where(t => t.SagaId == sagaId).ToList())
                {
                    storage.Remove(item);
                }
            }
        }
    }
}