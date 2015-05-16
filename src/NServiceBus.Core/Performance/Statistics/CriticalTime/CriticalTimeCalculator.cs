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
        TimeSpan estimatedMaximumProcessingDuration = TimeSpan.FromSeconds(2);
        DateTime lastMessageProcessedTime;
// ReSharper disable once NotAccessedField.Local
        Timer timer;

        public CriticalTimeCalculator(PerformanceCounter cnt)
        {
            counter = cnt;
            timer = new Timer(ResetCounterValueIfNoMessageHasBeenProcessedRecently, null, 0, 2000);
        }

        public void Dispose()
        {
            //Injected at compile time
        }

        public void Update(DateTime sentInstant, DateTime processingStartedInstant, DateTime processingEndedInstant)
        {
            var endToEndTime = processingEndedInstant - sentInstant;
            counter.RawValue = Convert.ToInt32(endToEndTime.TotalSeconds);

            lastMessageProcessedTime = processingEndedInstant;

            var processingDuration = processingEndedInstant - processingStartedInstant;
            estimatedMaximumProcessingDuration = processingDuration.Add(TimeSpan.FromSeconds(1));
        }

        void ResetCounterValueIfNoMessageHasBeenProcessedRecently(object state)
        {
            if (NoMessageHasBeenProcessedRecently())
            {
                counter.RawValue = 0;
            }
        }

        bool NoMessageHasBeenProcessedRecently()
        {
            var timeFromLastMessageProcessed = DateTime.UtcNow - lastMessageProcessedTime;
            return timeFromLastMessageProcessed > estimatedMaximumProcessingDuration;
        }
    }
}