namespace NServiceBus.InMemory.TimeoutPersister
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using Timeout.Core;

    class InMemoryTimeoutPersister : IPersistTimeouts
    {

        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            var results = storage
                .Where(kvp => kvp.Value.Time > startSlice && kvp.Value.Time <= DateTime.UtcNow)
                .OrderBy(kvp => kvp.Value.Time)
                .Select(kvp => new Tuple<string, DateTime>(kvp.Key, kvp.Value.Time))
                .ToList();

            var nextTimeout = storage.Values
                .Where(data => data.Time > DateTime.UtcNow)
                .OrderBy(data => data.Time)
                .FirstOrDefault();

            nextTimeToRunQuery = nextTimeout != null ? nextTimeout.Time : DateTime.UtcNow.AddMinutes(1);

            return results;
        }

        public void Add(string timeoutId, TimeoutData timeout)
        {
            storage.TryAdd(timeoutId,timeout);
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            return storage.TryRemove(timeoutId, out timeoutData);
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            var toDelete = storage.Where(kvp => kvp.Value.SagaId == sagaId)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in toDelete)
            {
                TimeoutData data;

                storage.TryRemove(key, out data);
            }
        }

        readonly ConcurrentDictionary<string, TimeoutData> storage = new ConcurrentDictionary<string, TimeoutData>();

    }
}
