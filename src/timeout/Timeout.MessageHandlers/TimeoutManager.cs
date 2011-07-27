using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Timeout.MessageHandlers
{
    /// <summary>
    /// Thread-safe timeout management class.
    /// </summary>
    public class TimeoutManager : IManageTimeouts
    {
        public IPersistTimeouts Persister { get; set; }

        public event EventHandler<TimeoutData> SagaTimedOut;

        public void Init(TimeSpan interval)
        {
            lock (data)
            {
                duration = interval;
                Persister.Init();

                Persister.GetAll().ToList().ForEach(td =>
                    PushTimeout(td));
            }
        }

        public void PushTimeout(TimeoutData timeout)
        {
            lock (data)
            {
                if (!data.ContainsKey(timeout.Time))
                    data[timeout.Time] = new List<TimeoutData>();

                data[timeout.Time].Add(timeout);

                if (!sagaLookup.ContainsKey(timeout.SagaId))
                    sagaLookup[timeout.SagaId] = new List<DateTime>();

                sagaLookup[timeout.SagaId].Add(timeout.Time);

                Persister.Add(timeout);
            }
        }

        public void PopTimeout()
        {
            var pair = new KeyValuePair<DateTime, List<TimeoutData>>(DateTime.MinValue, null);

            lock (data)
            {
                if (data.Count > 0)
                {
                    var next = data.ElementAt(0);
                    if (next.Key - DateTime.UtcNow < duration)
                    {
                        pair = next;
                        data.Remove(pair.Key);

                        pair.Value.ForEach(td =>
                            {
                                sagaLookup.Remove(td.SagaId);
                                Persister.Remove(td);
                            });
                    }
                }
            }

            if (pair.Key == DateTime.MinValue)
            {
                Thread.Sleep(duration);
                return;
            }

            if (pair.Key > DateTime.UtcNow)
                Thread.Sleep(pair.Key - DateTime.UtcNow);

            pair.Value.ForEach(OnSagaTimedOut);
        }

        public void ClearTimeout(Guid sagaId)
        {
            lock(data)
            {
                if (!sagaLookup.ContainsKey(sagaId))
                    return;

                var times = sagaLookup[sagaId];

                sagaLookup.Remove(sagaId);

                foreach (var time in times)
                {
                    foreach (var td in data[time].ToArray())
                        if (td.SagaId == sagaId)
                            data[time].Remove(td);

                    if (data[time].Count == 0)
                        data.Remove(time);
                }

                Persister.ClearAll(sagaId);
            }
        }

        private void OnSagaTimedOut(TimeoutData timeoutData)
        {
            if (SagaTimedOut != null)
                SagaTimedOut(null, timeoutData);
        }

        private readonly SortedDictionary<DateTime, List<TimeoutData>> data = new SortedDictionary<DateTime, List<TimeoutData>>();
        private readonly Dictionary<Guid, List<DateTime>> sagaLookup = new Dictionary<Guid, List<DateTime>>();

        private TimeSpan duration;
    }
}
