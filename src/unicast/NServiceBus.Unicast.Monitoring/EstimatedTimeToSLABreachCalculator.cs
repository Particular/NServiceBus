namespace NServiceBus.Unicast.Monitoring
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class EstimatedTimeToSLABreachCalculator
    {
        /// <summary>
        /// Verified that the counter exists
        /// </summary>
        /// <param name="sla"> </param>
        public void Initialize(TimeSpan sla)
        {
            endpointSLA = sla;
         
            //timer = new Timer(ClearPerfCounter, null, 0, 2000);
        }


        public void Update(DateTime timeSent, DateTime timeProcessed)
        {
            var dataPoint = new DataPoint
                                {
                                    CriticalTime = timeProcessed - timeSent,
                                    OccuredAt = timeProcessed
                                };
            IList<DataPoint> snapshot;
            lock (dataPoints)
            {
                dataPoints.Add(dataPoint);
                if(dataPoints.Count > MaxDatapoints)
                    dataPoints.RemoveRange(0, dataPoints.Count - MaxDatapoints);

                snapshot = new List<DataPoint>(dataPoints);
            }

            UpdateTimeToSLABreachGiven(snapshot);
        }

        void UpdateTimeToSLABreachGiven(IEnumerable<DataPoint> snapshots)
        {
            //need at least 2 datapoints to be able to calculate
            if(snapshots.Count()<2)
                return;

            DataPoint previous = null;

            TimeSpan criticalTimeDelta = TimeSpan.Zero;

            foreach (var current in snapshots)
            {
                if(previous != null)
                {
                    criticalTimeDelta += current.CriticalTime - previous.CriticalTime;
                }

                previous = current;
            }

            var elapsedTime = snapshots.Last().OccuredAt - snapshots.First().OccuredAt;
            var lastKnownCriticalTime = snapshots.Last().CriticalTime.TotalSeconds;

            var criticalTimeDeltaPerSecond = criticalTimeDelta.TotalSeconds / elapsedTime.TotalSeconds;

            var secondsToSLABreach = (endpointSLA.TotalSeconds - lastKnownCriticalTime) / criticalTimeDeltaPerSecond;


            SetCounterAction(secondsToSLABreach);
        }

        public Action<double> SetCounterAction = d => { throw new InvalidOperationException("The performance counter action must be set"); };


        //void ClearPerfCounter(object state)
        //{
        //    var timeSinceLastCounter = DateTime.UtcNow - timeOfLastCounter;

        //    if (timeSinceLastCounter > maxTimeSinceLastCounter)
        //    {
        //        counter.RawValue = long.MaxValue;
        //        lock(dataPoints)
        //            dataPoints.Clear();

        //    }
                
        //}

        List<DataPoint> dataPoints = new List<DataPoint>();

        const int MaxDatapoints = 10;


        //Timer timer;
        //DateTime timeOfLastCounter;
        //readonly TimeSpan maxTimeSinceLastCounter = TimeSpan.FromSeconds(2);

        TimeSpan endpointSLA;

        class DataPoint
        {
            public TimeSpan CriticalTime { get; set; }
            public DateTime OccuredAt { get; set; }
        }
    }
}