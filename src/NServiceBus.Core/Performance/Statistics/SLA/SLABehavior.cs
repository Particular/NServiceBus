namespace NServiceBus
{
    using System;
    using Pipeline;

    class SLABehavior : PhysicalMessageProcessingStageBehavior
    {
        EstimatedTimeToSLABreachCalculator breachCalculator;

        public SLABehavior(EstimatedTimeToSLABreachCalculator breachCalculator)
        {
            this.breachCalculator = breachCalculator;
        }

        public override void Invoke(Context context, Action next)
        {
            next();

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