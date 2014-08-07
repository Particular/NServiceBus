namespace NServiceBus
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;


    class CriticalTimeBehavior : IBehavior<IncomingContext>
    {
        CriticalTimeCalculator criticalTimeCounter;

        public CriticalTimeBehavior(CriticalTimeCalculator criticalTimeCounter)
        {
            this.criticalTimeCounter = criticalTimeCounter;
        }

        public void Invoke(IncomingContext context, Action next)
        {
            next();
            string timeSentString;
            var headers = context.PhysicalMessage.Headers;
            if (headers.TryGetValue(Headers.TimeSent, out timeSentString))
            {
                var timeSent = DateTimeExtensions.ToUtcDateTime(timeSentString);
                var processingStarted = DateTimeExtensions.ToUtcDateTime(headers[Headers.ProcessingStarted]);
                var processingEnded = DateTimeExtensions.ToUtcDateTime(headers[Headers.ProcessingEnded]);

                criticalTimeCounter.Update(timeSent, processingStarted, processingEnded);
            }
        }

        public class Registration:RegisterStep
        {
            public Registration()
                : base("CriticalTime", typeof(CriticalTimeBehavior), "Updates the critical time performance counter")
            {
                InsertBefore("ProcessingStatistics");
            }
        }
    }
}