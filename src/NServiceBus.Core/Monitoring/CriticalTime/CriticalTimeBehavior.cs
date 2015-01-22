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

            DateTime timeSent;

            if (!context.TryGet("IncomingMessage.TimeSent", out timeSent))
            {
                return;
            }

            criticalTimeCounter.Update(timeSent, context.Get<DateTime>("IncomingMessage.ProcessingStarted"), context.Get<DateTime>("IncomingMessage.ProcessingEnded"));
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