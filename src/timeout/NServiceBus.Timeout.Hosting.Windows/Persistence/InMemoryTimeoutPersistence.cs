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
            lock (lockObject)
            {
                var timeouts = new List<TimeoutData>(storage.Where(data => data.Time <= DateTime.UtcNow));
                var nextTimeout = storage.Where(data => data.Time > DateTime.UtcNow).OrderBy(data => data.Time).FirstOrDefault();

                nextTimeToRunQuery = nextTimeout != null ? nextTimeout.Time : DateTime.UtcNow.AddMinutes(1);

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

        public bool TryRemove(string timeoutId)
        {
            lock (lockObject)
            {
                var timeoutData = storage.SingleOrDefault(t => t.Id == timeoutId);
                
                if (timeoutData == null)
                {
                    return false;
                }

                return storage.Remove(timeoutData);
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