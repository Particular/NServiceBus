namespace NServiceBus.Features
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Used to configure SLAMonitoring.
    /// </summary>
    public class SLAMonitoring : Feature
    {
        internal SLAMonitoring()
        {
        }

        /// <summary>
        /// <see cref="Feature.Setup" />.
        /// </summary>
        protected internal override void Setup(FeatureConfigurationContext context)
        {
            if (context.Settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                throw new Exception("SLA Monitoring is not supported for send only endpoints, please remove .EnableSLAPerformanceCounter(mySLA) from your config.");
            }

            TimeSpan endpointSla;

            if (!context.Settings.TryGet(EndpointSLAKey, out endpointSla))
            {
                throw new Exception("Endpoint SLA is required for the `SLA violation countdown` counter. Pass the SLA for this endpoint to .EnableSLAPerformanceCounter(mySLA).");
            }

            var counterInstanceName = context.Settings.EndpointName().ToString();
            var slaBreachCounter = new EstimatedTimeToSLABreachCounter(endpointSla, counterInstanceName);

            var notifications = context.Settings.Get<NotificationSubscriptions>();

            notifications.Subscribe<ReceivePipelineCompleted>(e =>
            {
                string timeSentString;

                if (!e.ProcessedMessage.Headers.TryGetValue(Headers.TimeSent, out timeSentString))
                {
                    return TaskEx.CompletedTask;
                }

                slaBreachCounter.Update(DateTimeExtensions.ToUtcDateTime(timeSentString), e.StartedAt, e.CompletedAt);

                return TaskEx.CompletedTask;
            });

            context.RegisterStartupTask(() => slaBreachCounter);
        }

        internal const string EndpointSLAKey = "EndpointSLA";

        class EstimatedTimeToSLABreachCounter : FeatureStartupTask
        {
            public EstimatedTimeToSLABreachCounter(TimeSpan endpointSla, string counterInstanceName)
            {
                this.endpointSla = endpointSla;
                this.counterInstanceName = counterInstanceName;
            }


            public void Update(DateTime sent, DateTime processingStarted, DateTime processingEnded)
            {
                var dataPoint = new DataPoint
                {
                    CriticalTime = processingEnded - sent,
                    ProcessingTime = processingEnded - processingStarted,
                    OccurredAt = processingEnded
                };

                lock (dataPoints)
                {
                    dataPoints.Add(dataPoint);
                    if (dataPoints.Count > MaxDataPoints)
                    {
                        dataPoints.RemoveRange(0, dataPoints.Count - MaxDataPoints);
                    }
                }

                UpdateTimeToSLABreach();
            }

            protected override Task OnStart(IMessageSession session)
            {
                counter = PerformanceCounterHelper.InstantiatePerformanceCounter("SLA violation countdown", counterInstanceName);
                timer = new Timer(RemoveOldDataPoints, null, 0, 2000);

                return TaskEx.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session)
            {
                timer.Dispose();
                counter.Dispose();

                return TaskEx.CompletedTask;
            }

            void UpdateTimeToSLABreach()
            {
                IList<DataPoint> snapshots;

                lock (dataPoints)
                {
                    snapshots = new List<DataPoint>(dataPoints);
                }

                var secondsToSLABreach = CalculateTimeToSLABreach(snapshots);

                counter.RawValue = Convert.ToInt32(Math.Min(secondsToSLABreach, int.MaxValue));
            }

            double CalculateTimeToSLABreach(IList<DataPoint> snapshots)
            {
                //need at least 2 data points to be able to calculate
                if (snapshots.Count < 2)
                {
                    return double.MaxValue;
                }

                DataPoint previous = null;

                var criticalTimeDelta = TimeSpan.Zero;

                foreach (var current in snapshots)
                {
                    if (previous != null)
                    {
                        criticalTimeDelta += current.CriticalTime - previous.CriticalTime;
                    }

                    previous = current;
                }

                if (criticalTimeDelta.TotalSeconds <= 0.0)
                {
                    return double.MaxValue;
                }

                var elapsedTime = snapshots.Last().OccurredAt - snapshots.First().OccurredAt;

                if (elapsedTime.TotalSeconds <= 0.0)
                {
                    return double.MaxValue;
                }

                var lastKnownCriticalTime = snapshots.Last().CriticalTime.TotalSeconds;

                var criticalTimeDeltaPerSecond = criticalTimeDelta.TotalSeconds / elapsedTime.TotalSeconds;

                var secondsToSLABreach = (endpointSla.TotalSeconds - lastKnownCriticalTime) / criticalTimeDeltaPerSecond;

                if (secondsToSLABreach < 0.0)
                {
                    return 0.0;
                }

                return secondsToSLABreach;
            }

            void RemoveOldDataPoints(object state)
            {
                lock (dataPoints)
                {
                    var last = dataPoints.LastOrDefault();

                    if (last != null)
                    {
                        var oldestDataToKeep = DateTime.UtcNow - new TimeSpan(last.ProcessingTime.Ticks * 3);

                        dataPoints.RemoveAll(d => d.OccurredAt < oldestDataToKeep);
                    }
                }

                UpdateTimeToSLABreach();
            }

            PerformanceCounter counter;
            List<DataPoint> dataPoints = new List<DataPoint>();
            TimeSpan endpointSla;
            string counterInstanceName;
            // ReSharper disable once NotAccessedField.Local
            Timer timer;

            const int MaxDataPoints = 10;

            class DataPoint
            {
                public TimeSpan CriticalTime { get; set; }
                public DateTime OccurredAt { get; set; }
                public TimeSpan ProcessingTime { get; set; }
            }
        }
    }
}