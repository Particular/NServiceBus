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

            DateTime timeSent;

            if (!context.TryGet("IncomingMessage.TimeSent", out timeSent))
            {
                return;
            }

            breachCalculator.Update(timeSent, context.Get<DateTime>("IncomingMessage.ProcessingStarted"), context.Get<DateTime>("IncomingMessage.ProcessingEnded"));
        }

        public class Registration:RegisterStep
        {
            public Registration()
                : base("SLA", typeof(SLABehavior), "Updates the SLA performance counter")
            {
                InsertBefore(WellKnownStep.ProcessingStatistics);
            }
        }
    }
}