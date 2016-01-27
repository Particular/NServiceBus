namespace NServiceBus.AcceptanceTests.Timeouts
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using Timeout.Core;

    /// <summary>
    /// This class mocks outages for timeout storage. 
    /// If SecondsToWait is set to 10, it will throw exceptions for 10 seconds, then be available for 10 seconds, and repeat.
    /// </summary>
    class CyclingOutageTimeoutPersister : IPersistTimeouts, IQueryTimeouts
    {
        int secondsToWait;

        public int SecondsToWait
        {
            get { return secondsToWait; }
            set
            {
                secondsToWait = value;
                NextChangeTime = DateTime.Now.AddSeconds(SecondsToWait);
            }
        }

        static bool isAvailable = false;
        Task completedTask = Task.FromResult(0);
        DateTime NextChangeTime;
        ConcurrentDictionary<string, TimeoutData> storage = new ConcurrentDictionary<string, TimeoutData>(); 

        void ThrowExceptionUntilWaitTimeReached()
        {
            if (NextChangeTime <= DateTime.Now)
            {
                NextChangeTime = DateTime.Now.AddSeconds(SecondsToWait);
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
            nextTimeToRunQuery = DateTime.Now.AddSeconds(2);
            return Enumerable.Empty<Tuple<string, DateTime>>().ToList();
        }

        public Task Add(TimeoutData timeout)
        {
            ThrowExceptionUntilWaitTimeReached();
            return completedTask;
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

            var chunk = new TimeoutsChunk(timeoutsDue, DateTime.Now.AddSeconds(5));

            return Task.FromResult(chunk);
        }
    }
}
