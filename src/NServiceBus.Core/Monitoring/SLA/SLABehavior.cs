namespace NServiceBus
{
    using System;
    using Pipeline;
    using Pipeline.Contexts;

    class SLABehavior : IBehavior<IncomingContext>
    {
        EstimatedTimeToSLABreachCalculator breachCalculator;

        public SLABehavior(EstimatedTimeToSLABreachCalculator breachCalculator)
        {
            this.breachCalculator = breachCalculator;
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

                breachCalculator.Update(timeSent, processingStarted, processingEnded);
            }
        }

        public class Registration:RegisterStep
        {
            public Registration()
                : base("SLA", typeof(SLABehavior), "Updates the SLA performance counter")
            {
                InsertBefore("ProcessingStatistics");
            }
        }
    }
}