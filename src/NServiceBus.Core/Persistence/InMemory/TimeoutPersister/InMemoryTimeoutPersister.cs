namespace NServiceBus.InMemory.TimeoutPersister
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Timeout.Core;

    class InMemoryTimeoutPersister : IPersistTimeouts, IQueryTimeouts, IDisposable
    {
        public void Dispose()
        {
        }

        public Task Add(TimeoutData timeout, ReadOnlyContextBag context)
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

            return Task.FromResult(0);
        }

        public Task<TimeoutData> Remove(string timeoutId, ReadOnlyContextBag context)
        {
            try
            {
                readerWriterLock.EnterWriteLock();

                for (var index = 0; index < storage.Count; index++)
                {
                    var data = storage[index];
                    if (data.Id == timeoutId)
                    {
                        storage.RemoveAt(index);
                        return Task.FromResult(data);
                    }
                }

                return Task.FromResult<TimeoutData>(null);
            }
            finally
            {
                readerWriterLock.ExitWriteLock();
            }
        }

        public Task RemoveTimeoutBy(Guid sagaId, ReadOnlyContextBag context)
        {
            try
            {
                readerWriterLock.EnterWriteLock();
                for (var index = 0; index < storage.Count;)
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

            return Task.FromResult(0);
        }

        public Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
        {
            var now = DateTime.UtcNow;
            var nextTimeToRunQuery = DateTime.MaxValue;
            var dueTimeouts = new List<TimeoutsChunk.Timeout>();

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
                        dueTimeouts.Add(new TimeoutsChunk.Timeout(data.Id, data.Time));
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

            return Task.FromResult(new TimeoutsChunk(dueTimeouts, nextTimeToRunQuery));
        }

        ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();
        List<TimeoutData> storage = new List<TimeoutData>();
    }
}