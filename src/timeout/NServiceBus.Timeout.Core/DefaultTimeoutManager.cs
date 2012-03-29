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
            }
        }

        void IManageTimeouts.PopTimeout()
        {
            var pair = new KeyValuePair<DateTime, List<TimeoutData>>(DateTime.MinValue, null);
            var now = DateTime.UtcNow;

            lock (data)
            {
                if (data.Count > 0)
                {
                    var next = data.ElementAt(0);
                    if (next.Key - now < duration)
                    {
                        pair = next;
                        data.Remove(pair.Key);
                    }
                }
            }

            if (pair.Key == DateTime.MinValue)
            {
                Thread.Sleep(duration);
                return;
            }

            if (pair.Key > now)
                Thread.Sleep(pair.Key - now);

            pair.Value.ForEach(OnSagaTimedOut);
        }

        void IManageTimeouts.ClearTimeout(Guid sagaId)
        {
            lock(data)
            {
                data.Where(time => time.Value.Any(t => t.SagaId == sagaId)).ToList()
                    .ForEach(time =>
                                 {
                                     time.Value.RemoveAll(t => t.SagaId == sagaId);

                                     if (!time.Value.Any())
                                         data.Remove(time.Key);
                                 });
            }
        }

        void OnSagaTimedOut(TimeoutData timeoutData)
        {
            if (SagaTimedOut != null)
                SagaTimedOut(null, timeoutData);
        }

        readonly SortedDictionary<DateTime, List<TimeoutData>> data = new SortedDictionary<DateTime, List<TimeoutData>>();

        TimeSpan duration = TimeSpan.FromSeconds(1); 
    }
}
