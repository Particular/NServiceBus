namespace NServiceBus.Persistence.InMemory.TimeoutPersister
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Support;
    using Timeout.Core;

    public class InMemoryTimeoutPersistence : IPersistTimeouts
    {
        readonly IList<TimeoutData> storage = new List<TimeoutData>();
        readonly object lockObject = new object();

        public List<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            lock (lockObject)
            {
                var results = storage
                    .Where(data => data.Time > startSlice && data.Time <= SystemClock.TechnicalTime)
                    .OrderBy(data => data.Time)
                    .Select(t => new Tuple<string, DateTime>(t.Id, t.Time))
                    .ToList();

                var nextTimeout = storage
                    .Where(data => data.Time > SystemClock.TechnicalTime)
                    .OrderBy(data => data.Time)
                    .FirstOrDefault();

                nextTimeToRunQuery = nextTimeout != null ? nextTimeout.Time : SystemClock.TechnicalTime.AddMinutes(1);

                return results;
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
                storage.Where(t => t.SagaId == sagaId).ToList().ForEach(item => storage.Remove(item));
            }
        }
    }
}
