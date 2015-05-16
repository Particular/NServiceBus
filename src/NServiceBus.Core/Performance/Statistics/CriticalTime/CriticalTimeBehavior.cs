namespace NServiceBus
{
    using System;
    using Pipeline;


    class CriticalTimeBehavior : PhysicalMessageProcessingStageBehavior
    {
        CriticalTimeCalculator criticalTimeCounter;

        public CriticalTimeBehavior(CriticalTimeCalculator criticalTimeCounter)
        {
            this.criticalTimeCounter = criticalTimeCounter;
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

            criticalTimeCounter.Update(state.TimeSent.Value, state.ProcessingStarted,state.ProcessingEnded);
        }

        public class Registration : RegisterStep
        {
            public Registration()
                : base("CriticalTime", typeof(CriticalTimeBehavior), "Updates the critical time performance counter")
            {
                InsertBefore(WellKnownStep.ProcessingStatistics);
            }
        }
    }
}