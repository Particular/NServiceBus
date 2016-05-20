namespace NServiceBus.Features
{
    using System;
    using System.Diagnostics;
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

            var counterInstanceName = context.Settings.EndpointName();
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
                    var i = index;
                    i = (i + 1) & MaxDataPointsMask;
                    dataPoints[i] = dataPoint;
                    index = i;
                }

                UpdateTimeToSLABreach();
            }

            protected override Task OnStart(IMessageSession session)
            {
                counter = PerformanceCounterHelper.InstantiatePerformanceCounter("SLA violation countdown", counterInstanceName);
                timer = new Timer(TriggerUpdateTimeToSLABreach, null, 0, 2000);

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
                var snapshots = new DataPoint[MaxDataPoints];

                lock (dataPoints)
                {
                    Array.Copy(dataPoints, snapshots, MaxDataPoints);
                }

                var secondsToSLABreach = CalculateTimeToSLABreach(snapshots);

                counter.RawValue = Convert.ToInt32(Math.Min(secondsToSLABreach, int.MaxValue));
            }

            double CalculateTimeToSLABreach(DataPoint[] snapshots)
            {
                if (snapshots.Length == 0)
                {
                    return double.MaxValue;
                }

                DataPoint? first = null, previous = null;

                var criticalTimeDelta = TimeSpan.Zero;

                var last = snapshots[snapshots.Length - 1];
                var oldestDataToKeep = DateTime.UtcNow - new TimeSpan(last.ProcessingTime.Ticks*3);

                for (var i = 0; i < snapshots.Length; i++)
                {
                    var current = snapshots[i];
                    if (current.OccurredAt < oldestDataToKeep)
                    {
                        continue;
                    }

                    if (!first.HasValue)
                    {
                        first = current;
                    }

                    if (previous.HasValue)
                    {
                        criticalTimeDelta += current.CriticalTime - previous.Value.CriticalTime;
                    }

                    previous = current;
                }

                if (criticalTimeDelta.TotalSeconds <= 0.0)
                {
                    return double.MaxValue;
                }

                var elapsedTime = previous.Value.OccurredAt - first.Value.OccurredAt;

                if (elapsedTime.TotalSeconds <= 0.0)
                {
                    return double.MaxValue;
                }

                var lastKnownCriticalTime = previous.Value.CriticalTime.TotalSeconds;

                var criticalTimeDeltaPerSecond = criticalTimeDelta.TotalSeconds/elapsedTime.TotalSeconds;

                var secondsToSLABreach = (endpointSla.TotalSeconds - lastKnownCriticalTime)/criticalTimeDeltaPerSecond;

                if (secondsToSLABreach < 0.0)
                {
                    return 0.0;
                }

                return secondsToSLABreach;
            }

            void TriggerUpdateTimeToSLABreach(object state)
            {
                UpdateTimeToSLABreach();
            }

            PerformanceCounter counter;
            DataPoint[] dataPoints = new DataPoint[MaxDataPoints];
            TimeSpan endpointSla;
            string counterInstanceName;
            // ReSharper disable once NotAccessedField.Local
            Timer timer;
            int index;

            const int MaxDataPoints = 16;
            const int MaxDataPointsMask = MaxDataPoints - 1;

            struct DataPoint
            {
                public TimeSpan CriticalTime;
                public DateTime OccurredAt;
                public TimeSpan ProcessingTime;
            }
        }
    }
}