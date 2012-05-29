namespace NServiceBus.Timeout.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using log4net;

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
            
            DispatchTimeouts(pair.Value);
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

        void DispatchTimeouts(List<TimeoutData> value)
        {
            var exceptions = new List<Exception>();

            value.ForEach(timeout =>
                              {
                                  try
                                  {
                                      OnSagaTimedOut(timeout);
                                  }
                                  catch (Exception ex)
                                  {
                                      Logger.Error("Failed to dispatch timeout " + timeout,ex);
                                      exceptions.Add(ex);
                                  }
                              });

            if(exceptions.Any())
                throw new InvalidOperationException("Failed to dispatch the following timeouts:" + string.Join(";",value));
        }

        void OnSagaTimedOut(TimeoutData timeoutData)
        {
            if (SagaTimedOut != null)
                SagaTimedOut(null, timeoutData);
        }

        readonly SortedDictionary<DateTime, List<TimeoutData>> data = new SortedDictionary<DateTime, List<TimeoutData>>();

        TimeSpan duration = TimeSpan.FromSeconds(1);
        static ILog Logger = LogManager.GetLogger("Timeouts");
    }
}
