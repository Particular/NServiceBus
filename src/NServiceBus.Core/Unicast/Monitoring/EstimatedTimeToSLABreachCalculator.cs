namespace NServiceBus.Unicast.Monitoring
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    public class EstimatedTimeToSLABreachCalculator : IDisposable
    {

        public void Dispose()
        {
            //Injected at compile time
        }

        public void DisposeManaged()
        {
            if (counter != null)
            {
                counter.Dispose();
            }
        }

        /// <summary>
        ///     Verified that the counter exists
        /// </summary>
        public void Initialize(TimeSpan sla, PerformanceCounter slaBreachCounter)
        {
            endpointSLA = sla;
            counter = slaBreachCounter;

            timer = new Timer(RemoveOldDataPoints, null, 0, 2000);
        }


        /// <summary>
        ///     Updates the counter based on the passed times
        /// </summary>
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

        void UpdateTimeToSLABreach()
        {
            IList<DataPoint> snapshots;

            lock (dataPoints)
            {
                snapshots = new List<DataPoint>(dataPoints);
            }

            var secondsToSLABreach = CalculateTimeToSLABreach(snapshots);

            counter.RawValue = Convert.ToInt32(Math.Min(secondsToSLABreach, Int32.MaxValue));
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

            var criticalTimeDeltaPerSecond = criticalTimeDelta.TotalSeconds/elapsedTime.TotalSeconds;

            var secondsToSLABreach = (endpointSLA.TotalSeconds - lastKnownCriticalTime)/criticalTimeDeltaPerSecond;

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
                    var oldestDataToKeep = DateTime.UtcNow - new TimeSpan(last.ProcessingTime.Ticks*3);

                    dataPoints.RemoveAll(d => d.OccurredAt < oldestDataToKeep);
                }
            }

            UpdateTimeToSLABreach();
        }

        const int MaxDataPoints = 10;
        readonly List<DataPoint> dataPoints = new List<DataPoint>();
        PerformanceCounter counter;
        TimeSpan endpointSLA;
        Timer timer;

        class DataPoint
        {
            public TimeSpan CriticalTime { get; set; }
            public DateTime OccurredAt { get; set; }
            public TimeSpan ProcessingTime { get; set; }
        }
    }
}