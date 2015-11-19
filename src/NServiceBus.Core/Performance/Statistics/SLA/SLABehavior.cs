namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class SLABehavior : Behavior<PhysicalMessageProcessingContext>
    {
        EstimatedTimeToSLABreachCalculator breachCalculator;

        public SLABehavior(EstimatedTimeToSLABreachCalculator breachCalculator)
        {
            this.breachCalculator = breachCalculator;
        }

        public override async Task Invoke(PhysicalMessageProcessingContext context, Func<Task> next)
        {
            await next().ConfigureAwait(false);

            ProcessingStatisticsBehavior.State state;

            if (!context.Extensions.TryGet(out state))
            {
                return;
            }

            if (!state.TimeSent.HasValue)
            {
                return;
            }

            breachCalculator.Update(state.TimeSent.Value, state.ProcessingStarted, state.ProcessingEnded);
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