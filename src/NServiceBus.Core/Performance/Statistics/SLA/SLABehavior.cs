namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;

    class SLABehavior : PhysicalMessageProcessingStageBehavior
    {
        EstimatedTimeToSLABreachCalculator breachCalculator;

        public SLABehavior(EstimatedTimeToSLABreachCalculator breachCalculator)
        {
            this.breachCalculator = breachCalculator;
        }

        public override async Task Invoke(Context context, Func<Task> next)
        {
            await next();

            ProcessingStatisticsBehavior.State state;

            if (!context.TryGet(out state))
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