namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Pipeline;


    class CriticalTimeBehavior : Behavior<IncomingPhysicalMessageContext>
    {
        CriticalTimeCalculator criticalTimeCounter;

        public CriticalTimeBehavior(CriticalTimeCalculator criticalTimeCounter)
        {
            this.criticalTimeCounter = criticalTimeCounter;
        }

        public override async Task Invoke(IncomingPhysicalMessageContext context, Func<Task> next)
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

            criticalTimeCounter.Update(state.TimeSent.Value, state.ProcessingStarted, state.ProcessingEnded);
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