namespace NServiceBus.Timeout.Hosting.Windows.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;

    public class InMemoryTimeoutPersistence : IPersistTimeouts
    {
        readonly IList<TimeoutData> storage = new List<TimeoutData>();
        readonly object lockObject = new object();

        public List<TimeoutData> GetNextChunk(out DateTime nextTimeToRunQuery)
        {
            nextTimeToRunQuery = DateTime.UtcNow;

            lock (lockObject)
            {
                var timeouts = new List<TimeoutData>(storage.Where(data => data.Time <= DateTime.UtcNow));

                var nextTimeout = storage.Where(data => data.Time > DateTime.UtcNow).OrderBy(data => data.Time).FirstOrDefault();
                if (nextTimeout != null)
                {
                    nextTimeToRunQuery = nextTimeout.Time;
                }

                return timeouts;
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

        public void Remove(string timeoutId)
        {
            lock (lockObject)
            {
                storage.Remove(storage.Single(t => t.Id == timeoutId));
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