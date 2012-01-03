namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    /// <summary>
    /// Thread-safe timeout management class.
    /// </summary>
    public class DefaultTimeoutManager : IManageTimeouts
    {
        public event EventHandler<TimeoutData> SagaTimedOut;

        void IManageTimeouts.Init(TimeSpan interval)
        {
            duration = interval;
        }

        void IManageTimeouts.PushTimeout(TimeoutData timeout)
        {
            lock (data)
            {
                if (!data.ContainsKey(timeout.Time))
                    data[timeout.Time] = new List<TimeoutData>();

                data[timeout.Time].Add(timeout);
                sagaLookup[timeout.SagaId] = timeout.Time;
            }
        }

        void IManageTimeouts.PopTimeout()
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

                        pair.Value.ForEach(td => sagaLookup.Remove(td.SagaId));
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

        void IManageTimeouts.ClearTimeout(Guid sagaId)
        {
            lock(data)
            {
                if (!sagaLookup.ContainsKey(sagaId))
                    return;

                var time = sagaLookup[sagaId];

                sagaLookup.Remove(sagaId);
                
                if (!data.ContainsKey(time))
                    return;

                foreach (var td in data[time].ToArray())
                    if (td.SagaId == sagaId)
                        data[time].Remove(td);

                if (data[time].Count == 0)
                    data.Remove(time);
            }
        }

        private void OnSagaTimedOut(TimeoutData timeoutData)
        {
            if (SagaTimedOut != null)
                SagaTimedOut(null, timeoutData);
        }

        private readonly SortedDictionary<DateTime, List<TimeoutData>> data = new SortedDictionary<DateTime, List<TimeoutData>>();
        private readonly Dictionary<Guid, DateTime> sagaLookup = new Dictionary<Guid, DateTime>();

        private TimeSpan duration = TimeSpan.FromSeconds(1); 
    }
}
