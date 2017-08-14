namespace NServiceBus.InMemory.TimeoutPersister
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using Timeout.Core;

    class InMemoryTimeoutPersister : IPersistTimeouts, IPersistTimeoutsV2
    {
        List<TimeoutData> storage = new List<TimeoutData>();
        ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();
        Func<DateTime> currentTime = () => DateTime.UtcNow;

        public InMemoryTimeoutPersister()
        {            
        }

        public InMemoryTimeoutPersister(Func<DateTime> currentTimeProvider)
        {
            currentTime = currentTimeProvider;
        }

        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            var now = currentTime();
            nextTimeToRunQuery = DateTime.MaxValue;

            var tuples = new List<Tuple<string, DateTime>>();

            try
            {
                readerWriterLock.EnterReadLock();

                foreach (var data in storage)
                {
                    if (data.Time > now && data.Time < nextTimeToRunQuery)
                    {
                        nextTimeToRunQuery = data.Time;
                    }
                    if (data.Time > startSlice && data.Time <= now)
                    {
                        tuples.Add(new Tuple<string, DateTime>(data.Id, data.Time));
                    }
                }
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }
            if (nextTimeToRunQuery == DateTime.MaxValue)
            {
                nextTimeToRunQuery = now.AddMinutes(1);
            }
            return tuples;
        }

        public void Add(TimeoutData timeout)
        {
            timeout.Id = Guid.NewGuid().ToString();
            try
            {
                readerWriterLock.EnterWriteLock();
                storage.Add(timeout);
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public bool TryRemove(string timeoutId, out TimeoutData timeoutData)
        {
            try
            {
                readerWriterLock.EnterWriteLock();

                for (var index = 0; index < storage.Count; index++)
                {
                    var data = storage[index];
                    if (data.Id == timeoutId)
                    {
                        timeoutData = data;
                        storage.RemoveAt(index);
                        return true;
                    }
                }

                timeoutData = null;
                return false;
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public void RemoveTimeoutBy(Guid sagaId)
        {
            try
            {
                readerWriterLock.EnterWriteLock();
                for (var index = 0; index < storage.Count; )
                {
                    var timeoutData = storage[index];
                    if (timeoutData.SagaId == sagaId)
                    {
                        storage.RemoveAt(index);
                        continue;
                    }
                    index++;
                }
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public TimeoutData Peek(string timeoutId)
        {
            try
            {
                readerWriterLock.EnterReadLock();
                return storage.SingleOrDefault(t => t.Id == timeoutId);
            }
            finally
            {
                readerWriterLock.ExitReadLock();
            }

        }

        public bool TryRemove(string timeoutId)
        {
            TimeoutData timeoutData;
            return TryRemove(timeoutId, out timeoutData);
        }
    }
}
