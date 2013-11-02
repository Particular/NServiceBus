namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;

    class TimeoutManager
    {
        public void Push(object spanOrTime, object value)
        {
            var at = new DateTime();
            if (spanOrTime is DateTime)
                at = (DateTime) spanOrTime;
            if (spanOrTime is TimeSpan)
                at = DateTime.UtcNow + (TimeSpan) spanOrTime;

            if (!storage.ContainsKey(at))
                storage.Add(at, new List<object>());

            storage[at].Add(value);
        }

        public object Pop()
        {
            var min = DateTime.MaxValue;
            foreach (var d in storage.Keys)
                if (d < min)
                    min = d;

            var result = storage[min][0];

            storage[min].RemoveAt(0);
            if (storage[min].Count == 0)
                storage.Remove(min);

            return result;
        }

        private Dictionary<DateTime, List<object>> storage = new Dictionary<DateTime, List<object>>();
    }
}
