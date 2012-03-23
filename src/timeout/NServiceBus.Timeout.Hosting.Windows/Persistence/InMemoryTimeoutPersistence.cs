namespace NServiceBus.Timeout.Hosting.Windows.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;

    public class InMemoryTimeoutPersistence : IPersistTimeouts
    {
        readonly List<TimeoutData> storage = new List<TimeoutData>(); 

        public IEnumerable<TimeoutData> GetAll()
        {
            lock (storage)
                return new List<TimeoutData>(storage);
        }

        public void Add(TimeoutData timeout)
        {
            lock (storage)
                storage.Add(timeout);
        }

        public void RemoveTimeout(Guid timeoutId)
        {
            lock (storage)
                storage.RemoveAll(t => t.Id == timeoutId);
        }
    }
}