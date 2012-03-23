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
        public event EventHandler<TimeoutData> TimedOut;
        public event EventHandler<TimeoutData> TimeOutCleared;

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

                List<DateTime> timeouts;
                if (!sagaTimeouts.TryGetValue(timeout.SagaId, out timeouts))
                    sagaTimeouts.Add(timeout.SagaId, timeouts = new List<DateTime>());
                timeouts.Add(timeout.Time);
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

                        pair.Value.ForEach(
                            delegate(TimeoutData td)
                                {
                                    // remove this timeout entry from the sagaTimeouts list
                                    var timeouts = sagaTimeouts[td.SagaId];
                                    if (timeouts.Count > 1)
                                    {
                                        var index = timeouts.FindIndex(d => d == pair.Key);
                                        timeouts.RemoveAt(index);
                                    }
                                    else
                                        sagaTimeouts.Remove(td.SagaId);
                                });
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

            pair.Value.ForEach(OnTimedOut);
        }

        void IManageTimeouts.ClearTimeouts(Guid sagaId)
        {
            var clearedTimeouts = new List<TimeoutData>();
            lock (data)
            {
                List<DateTime> timeouts;
                if (!sagaTimeouts.TryGetValue(sagaId, out timeouts))
                    return;
                sagaTimeouts.Remove(sagaId);

                foreach (var time in timeouts.Distinct())
                {
                    var list = data[time];
                    clearedTimeouts.AddRange(list.Where(t => t.SagaId == sagaId));
                    clearedTimeouts.ForEach(td => list.Remove(td));

                    if (list.Count == 0)
                        data.Remove(time);
                }
            }
            clearedTimeouts.ForEach(OnTimeOutCleared);
        }

        private void OnTimedOut(TimeoutData timeoutData)
        {
            if (TimedOut != null)
                TimedOut(null, timeoutData);
        }

        private void OnTimeOutCleared(TimeoutData timeoutData)
        {
            if (TimeOutCleared != null)
                TimeOutCleared(null, timeoutData);
        }

        private readonly SortedDictionary<DateTime, List<TimeoutData>> data =
            new SortedDictionary<DateTime, List<TimeoutData>>();

        /// <summary>
        /// For each saga id, this dictionary contains a list of all DateTimes for which the given saga has requested a timeout
        /// </summary>
        private readonly Dictionary<Guid, List<DateTime>> sagaTimeouts = new Dictionary<Guid, List<DateTime>>();

        private TimeSpan duration = TimeSpan.FromSeconds(1);
    }
}