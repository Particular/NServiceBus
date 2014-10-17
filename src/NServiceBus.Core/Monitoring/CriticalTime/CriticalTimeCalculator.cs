namespace NServiceBus
{
    using System;
    using System.Diagnostics;
    using System.Threading;

    /// <summary>
    ///     Performance counter for the critical time
    /// </summary>
    class CriticalTimeCalculator : IDisposable
    {
        PerformanceCounter counter;
        TimeSpan maxDelta = TimeSpan.FromSeconds(2);
        DateTime timeOfLastCounter;
// ReSharper disable once NotAccessedField.Local
        Timer timer;


        public CriticalTimeCalculator(PerformanceCounter cnt)
        {
            counter = cnt;
            timer = new Timer(ClearPerfCounter, null, 0, 2000);
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void Update(DateTime sent, DateTime processingStarted, DateTime processingEnded)
        {
            counter.RawValue = Convert.ToInt32((processingEnded - sent).TotalSeconds);

            timeOfLastCounter = processingEnded;

            var timeTaken = processingEnded - processingStarted;
            maxDelta = timeTaken.Add(TimeSpan.FromSeconds(1));
        }

        void ClearPerfCounter(object state)
        {
            var delta = DateTime.UtcNow - timeOfLastCounter;

            if (delta > maxDelta)
            {
                counter.RawValue = 0;
            }
        }
    }
}