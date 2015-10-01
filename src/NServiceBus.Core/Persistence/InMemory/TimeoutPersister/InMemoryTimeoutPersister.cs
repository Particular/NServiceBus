namespace NServiceBus.InMemory.TimeoutPersister
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using Timeout.Core;

    class InMemoryTimeoutPersister : IPersistTimeouts, IQueryTimeouts, IDisposable
    {
        List<TimeoutData> storage = new List<TimeoutData>();
        ReaderWriterLockSlim readerWriterLock = new ReaderWriterLockSlim();

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

        public async Task Add(TimeoutData timeout, TimeoutPersistenceOptions options)
        {
            timeout.Id = Guid.NewGuid().ToString();
            var body = timeout.Body;
            var ms = new MemoryStream();
            await body.CopyToAsync(ms);
            timeout.Body = ms;
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

        public Task<TimeoutData> Remove(string timeoutId, TimeoutPersistenceOptions options)
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

        public Task RemoveTimeoutBy(Guid sagaId, TimeoutPersistenceOptions options)
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

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            
        }
    }
}