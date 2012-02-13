namespace NServiceBus.Unicast.Monitoring
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public class EstimatedTimeToSLABreachCalculator
    {
        /// <summary>
        /// Verified that the counter exists
        /// </summary>
        /// <param name="sla"> </param>
        public void Initialize(TimeSpan sla)
        {
            endpointSLA = sla;

            timer = new Timer(RemoveOldDatapoints, null, 0, 2000);
        }


        /// <summary>
        /// Updates the counter based on the passed times
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
                    dataPoints.RemoveRange(0, dataPoints.Count - MaxDatapoints);
            }

            UpdateTimeToSLABreach();
        }

        void UpdateTimeToSLABreach()
        {
            IList<DataPoint> snapshots;

            lock(dataPoints)
                snapshots = new List<DataPoint>(dataPoints);

            var secondsToSLABreach = CalculateTimeToSLABreach(snapshots);


            SetCounterAction(secondsToSLABreach);
        }

        double CalculateTimeToSLABreach(IEnumerable<DataPoint> snapshots)
        {
            //need at least 2 datapoints to be able to calculate
            if (snapshots.Count() < 2)
                return double.MaxValue;

            DataPoint previous = null;

            TimeSpan criticalTimeDelta = TimeSpan.Zero;

            foreach (var current in snapshots)
            {
                if (previous != null)
                {
                    criticalTimeDelta += current.CriticalTime - previous.CriticalTime;
                }

                previous = current;
            }
            
            if (criticalTimeDelta.TotalSeconds <= 0.0)
                return double.MaxValue;

            var elapsedTime = snapshots.Last().OccuredAt - snapshots.First().OccuredAt;

            if (elapsedTime.TotalSeconds <= 0.0)
                return double.MaxValue;


            var lastKnownCriticalTime = snapshots.Last().CriticalTime.TotalSeconds;

            var criticalTimeDeltaPerSecond = criticalTimeDelta.TotalSeconds / elapsedTime.TotalSeconds;

            var secondsToSLABreach = (endpointSLA.TotalSeconds - lastKnownCriticalTime) / criticalTimeDeltaPerSecond;

            if (secondsToSLABreach < 0.0)
                return 0.0;

            return secondsToSLABreach;
        }

        public Action<double> SetCounterAction = d => { throw new InvalidOperationException("The performance counter action must be set"); };


        void RemoveOldDatapoints(object state)
        {
            lock (dataPoints)
            {
                var last = dataPoints.LastOrDefault();

                if(last != null)
                {
                    var oldestDataToKeep = DateTime.UtcNow - new TimeSpan(last.ProcessingTime.Ticks * 3);

                    dataPoints.RemoveAll(d => d.OccuredAt < oldestDataToKeep);
                }
            }

            UpdateTimeToSLABreach();
        }

        readonly List<DataPoint> dataPoints = new List<DataPoint>();

        const int MaxDatapoints = 10;


        Timer timer;
        TimeSpan endpointSLA;

        class DataPoint
        {
            public TimeSpan CriticalTime { get; set; }
            public DateTime OccuredAt { get; set; }
            public TimeSpan ProcessingTime { get; set; }
        }
    }
}