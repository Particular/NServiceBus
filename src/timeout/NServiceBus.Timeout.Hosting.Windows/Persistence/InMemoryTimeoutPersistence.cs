namespace NServiceBus.Timeout.Hosting.Windows.Persistence
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Core;

    public class InMemoryTimeoutPersistence : IPersistTimeouts
    {
        readonly IList<TimeoutData> storage = new List<TimeoutData>(); 

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

        public void Remove(TimeoutData timeout)
        {
            storage.Remove(timeout);
        }

        public void ClearTimeoutsFor(Guid sagaId)
        {
            lock (storage)
                storage.Where(t => t.SagaId == sagaId).ToList().ForEach(item => storage.Remove(item));

        }
    }
}