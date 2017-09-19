namespace NServiceBus.AcceptanceTests.Core.Timeout
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Timeout.Core;

    /// <summary>
    /// This class mocks outages for timeout storage.
    /// If SecondsToWait is set to 10, it will throw exceptions for 10 seconds, then be available for 10 seconds, and repeat.
    /// </summary>
    class CyclingOutageTimeoutPersister : IPersistTimeouts, IQueryTimeouts
    {
        public CyclingOutageTimeoutPersister(int secondsToWait)
        {
            this.secondsToWait = secondsToWait;
        }

        public Task<bool> TryRemove(string timeoutId, ContextBag context)
        {
            ThrowExceptionUntilWaitTimeReached();

            TimeoutData timeoutData = null;

            if (storage.ContainsKey(timeoutId))
            {
                storage.TryRemove(timeoutId, out timeoutData);
            }

            return Task.FromResult(timeoutData != null);
        }

        public Task RemoveTimeoutBy(Guid sagaId, ContextBag context)
        {
            ThrowExceptionUntilWaitTimeReached();
            return completedTask;
        }

        public Task Add(TimeoutData timeout, ContextBag context)
        {
            ThrowExceptionUntilWaitTimeReached();
            storage.TryAdd(timeout.Id, timeout);
            return completedTask;
        }

        public Task<TimeoutData> Peek(string timeoutId, ContextBag context)
        {
            ThrowExceptionUntilWaitTimeReached();
            if (storage.ContainsKey(timeoutId))
            {
                return Task.FromResult(storage[timeoutId]);
            }
            return Task.FromResult<TimeoutData>(null);
        }

        public Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
        {
            ThrowExceptionUntilWaitTimeReached();

            var timeoutsDue = new List<TimeoutsChunk.Timeout>();
            foreach (var key in storage.Keys)
            {
                var value = storage[key];
                if (value.Time <= startSlice)
                {
                    var timeout = new TimeoutsChunk.Timeout(key, value.Time);
                    timeoutsDue.Add(timeout);
                }
            }

            var chunk = new TimeoutsChunk(timeoutsDue.ToArray(), DateTime.UtcNow.AddSeconds(1));

            return Task.FromResult(chunk);
        }

        void ThrowExceptionUntilWaitTimeReached()
        {
            if (NextChangeTime <= DateTime.UtcNow)
            {
                NextChangeTime = DateTime.UtcNow.AddSeconds(secondsToWait);
                isAvailable = !isAvailable;
            }

            if (!isAvailable)
            {
                throw new Exception("Persister is temporarily unavailable");
            }
        }

        public IEnumerable<Tuple<string, DateTime>> GetNextChunk(DateTime startSlice, out DateTime nextTimeToRunQuery)
        {
            ThrowExceptionUntilWaitTimeReached();
            nextTimeToRunQuery = DateTime.UtcNow.AddSeconds(1);
            return Enumerable.Empty<Tuple<string, DateTime>>().ToList();
        }

        public Task Add(TimeoutData timeout)
        {
            ThrowExceptionUntilWaitTimeReached();
            return completedTask;
        }

        Task completedTask = Task.FromResult(0);
        DateTime NextChangeTime;
        int secondsToWait;
        ConcurrentDictionary<string, TimeoutData> storage = new ConcurrentDictionary<string, TimeoutData>();

        static bool isAvailable;
    }
}