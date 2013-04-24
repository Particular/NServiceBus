namespace NServiceBus.Unicast.Monitoring
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    public class EstimatedTimeToSLABreachCalculator : IDisposable
    {
        const int MaxDatapoints = 10;
        readonly List<DataPoint> dataPoints = new List<DataPoint>();
        PerformanceCounter counter;
        bool disposed;
        TimeSpan endpointSLA;
        Timer timer;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                if (counter != null)
                {
                    counter.Dispose();
                }
            }
            disposed = true;
        }

        ~EstimatedTimeToSLABreachCalculator()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Verified that the counter exists
        /// </summary>
        /// <param name="sla"> </param>
        /// <param name="slaBreachCounter"></param>
        public void Initialize(TimeSpan sla, PerformanceCounter slaBreachCounter)
        {
            endpointSLA = sla;
            counter = slaBreachCounter;

            timer = new Timer(RemoveOldDatapoints, null, 0, 2000);
        }


        /// <summary>
        ///     Updates the counter based on the passed times
        /// </summary>
        /// <param name="sent"> </param>
        /// <param name="processingStarted"></param>
        /// <param name="processingEnded"></param>
        public void Update(DateTime sent, DateTime processingStarted, DateTime processingEnded)
        {
            var dataPoint = new DataPoint
                {
                    CriticalTime = processingEnded - sent,
                    ProcessingTime = processingEnded - processingStarted,
                    OccuredAt = processingEnded
                };

            lock (dataPoints)
            {
                dataPoints.Add(dataPoint);
                if (dataPoints.Count > MaxDatapoints)
                {
                    dataPoints.RemoveRange(0, dataPoints.Count - MaxDatapoints);
                }
            }

            UpdateTimeToSLABreach();
        }

        void UpdateTimeToSLABreach()
        {
            IList<DataPoint> snapshots;

            lock (dataPoints)
                snapshots = new List<DataPoint>(dataPoints);

            double secondsToSLABreach = CalculateTimeToSLABreach(snapshots);

            counter.RawValue = Convert.ToInt32(Math.Min(secondsToSLABreach, Int32.MaxValue));
        }

        double CalculateTimeToSLABreach(IEnumerable<DataPoint> snapshots)
        {
            //need at least 2 datapoints to be able to calculate
            if (snapshots.Count() < 2)
            {
                return double.MaxValue;
            }

            DataPoint previous = null;

            TimeSpan criticalTimeDelta = TimeSpan.Zero;

            foreach (DataPoint current in snapshots)
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

            TimeSpan elapsedTime = snapshots.Last().OccuredAt - snapshots.First().OccuredAt;

            if (elapsedTime.TotalSeconds <= 0.0)
            {
                return double.MaxValue;
            }


            double lastKnownCriticalTime = snapshots.Last().CriticalTime.TotalSeconds;

            double criticalTimeDeltaPerSecond = criticalTimeDelta.TotalSeconds/elapsedTime.TotalSeconds;

            double secondsToSLABreach = (endpointSLA.TotalSeconds - lastKnownCriticalTime)/criticalTimeDeltaPerSecond;

            if (secondsToSLABreach < 0.0)
            {
                return 0.0;
            }

            return secondsToSLABreach;
        }

        void RemoveOldDatapoints(object state)
        {
            lock (dataPoints)
            {
                DataPoint last = dataPoints.LastOrDefault();

                if (last != null)
                {
                    DateTime oldestDataToKeep = DateTime.UtcNow - new TimeSpan(last.ProcessingTime.Ticks*3);

                    dataPoints.RemoveAll(d => d.OccuredAt < oldestDataToKeep);
                }
            }

            UpdateTimeToSLABreach();
        }

        class DataPoint
        {
            public TimeSpan CriticalTime { get; set; }
            public DateTime OccuredAt { get; set; }
            public TimeSpan ProcessingTime { get; set; }
        }
    }
}